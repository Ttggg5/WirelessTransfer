using Ini;
using QRCoder;
using ScreenCapturerNS;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsInput;
using WirelessTransfer.Tools.InternetSocket;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyTcp;
using WirelessTransfer.Tools.Screen;
using WirelessTransfer.Windows;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// ExtendPage.xaml 的互動邏輯
    /// </summary>
    public partial class ExtendPage : Page
    {
        public event EventHandler<CustomControls.DeviceTag> DeviceChoosed;
        public event EventHandler DeviceConnected;
        public event EventHandler DeviceDisconnected;

        const int MAX_CLIENT = 1;

        int udpPort, tcpPort;
        Int64 quality;

        MyTcpServer myTcpServer;
        ScreenCaptureDX screenCaptureDX;
        InputSimulator inputSimulator;
        Process? virtualDisplayProcess;

        public ExtendPage()
        {
            InitializeComponent();

            udpPort = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, IniFile.DEFAULT_PATH));
            tcpPort = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.TcpPort, IniFile.DEFAULT_PATH));
            quality = Int64.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.ScreenQuality, IniFile.DEFAULT_PATH));

            inputSimulator = new InputSimulator();

            maskBorder.Visibility = Visibility.Collapsed;
            waitRespondSp.Visibility = Visibility.Collapsed;
            disconnectSp.Visibility = Visibility.Collapsed;

            Tag = PageFunction.Extend;
            deviceFinder.DeviceChoosed += deviceFinder_DeviceChoosed;
            deviceFinder.StartSearching();

            // create QR code
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode("Extend " + InternetInfo.GetLocalIPAddress(), QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                qrCodeImg.Source = BitmapConverter.ByteArrayToBitmapImage(qrCodeImage);
            }
        }

        private void deviceFinder_DeviceChoosed(object? sender, CustomControls.DeviceTag e)
        {
            deviceFinder.StopSearching();

            DeviceChoosed?.Invoke(this, e);

            maskBorder.Visibility = Visibility.Visible;
            waitRespondSp.Visibility = Visibility.Visible;

            myTcpServer = new MyTcpServer(tcpPort);
            myTcpServer.ClientConnected += myTcpServer_ClientConnected;
            myTcpServer.ClientDisconnected += myTcpServer_ClientDisconnected;
            myTcpServer.ReceivedCmd += myTcpServer_ReceivedCmd;
            myTcpServer.Start(MAX_CLIENT);

            // send request
            UdpClient udpClient = new UdpClient();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));
            byte[] bytes = new RequestCmd(RequestType.Extend, Environment.MachineName).Encode();
            udpClient.Send(bytes, bytes.Length, new System.Net.IPEndPoint(e.Address, udpPort));

            // waiting for accept
            Task<UdpReceiveResult> receiveTask = udpClient.ReceiveAsync();
            Task.Run(() =>
            {
                while (true)
                {
                    if (receiveTask.Wait(30000))
                    {
                        byte[] bytes = receiveTask.Result.Buffer;
                        Cmd cmd = CmdDecoder.DecodeCmd(bytes, 0, bytes.Length);
                        if (cmd != null && cmd.CmdType == CmdType.Reply)
                        {
                            ReplyCmd replyCmd = (ReplyCmd)cmd;
                            if (replyCmd.ReplyType == ReplyType.Refuse)
                            {
                                Dispatcher.BeginInvoke(() =>
                                {
                                    disconnectBtn_Click(this, null);
                                    MessageWindow messageWindow = new MessageWindow("對方已拒絕連接!", false);
                                    messageWindow.ShowDialog();
                                });
                                udpClient.Close();
                                return;
                            }
                            else break;
                        }
                        else
                        {
                            if (myTcpServer.CurState == MyTcpServerState.Listening)
                                receiveTask = udpClient.ReceiveAsync();
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            disconnectBtn_Click(this, null);
                            MessageWindow messageWindow = new MessageWindow("請求超時!", false);
                            messageWindow.ShowDialog();
                        });
                        udpClient.Close();
                        return;
                    }
                }
                udpClient.Close();

                Task.Delay(500).Wait();
                if (myTcpServer.ConnectedClients.Count == 0)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        disconnectBtn_Click(this, null);
                        MessageWindow messageWindow = new MessageWindow("連線逾時!", false);
                        messageWindow.ShowDialog();
                    });
                }
            });
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private void myTcpServer_ReceivedCmd(object? sender, Cmd e)
        {
            switch (e.CmdType)
            {
                case CmdType.Request:
                    RequestCmd rc = (RequestCmd)e;
                    if (rc.RequestType == RequestType.Disconnect)
                        Disconnect();
                    break;
                case CmdType.Mouse:
                    MouseCmd mouseCmd = (MouseCmd)e;

                    // move mouse
                    if (mouseCmd.MoveMouse)
                        System.Windows.Forms.Cursor.Position =
                            new System.Drawing.Point(
                                (int)mouseCmd.MousePos.X + Screen.AllScreens.Last().Bounds.X, 
                                (int)mouseCmd.MousePos.Y + Screen.AllScreens.Last().Bounds.Y);

                    // do mouse action
                    if (mouseCmd.MouseAct != MouseAct.None)
                        mouse_event(
                            (int)mouseCmd.MouseAct,
                            (int)mouseCmd.MousePos.X + Screen.AllScreens.Last().Bounds.X,
                            (int)mouseCmd.MousePos.Y + Screen.AllScreens.Last().Bounds.Y,
                            mouseCmd.MiddleButtonMomentum, 0);
                    break;
                case CmdType.MouseMove:
                    MouseMoveCmd mmc = (MouseMoveCmd)e;
                    System.Drawing.Point point = System.Windows.Forms.Cursor.Position;
                    point.X += (int)mmc.MouseDisplacementX;
                    point.Y += (int)mmc.MouseDisplacementY;

                    System.Windows.Forms.Cursor.Position = point;
                    break;
                case CmdType.Keyboard:
                    KeyboardCmd keyboardCmd = (KeyboardCmd)e;
                    switch (keyboardCmd.State)
                    {
                        case KeyState.Down:
                            inputSimulator.Keyboard.KeyDown(keyboardCmd.KeyCode);
                            break;
                        case KeyState.Up:
                            inputSimulator.Keyboard.KeyUp(keyboardCmd.KeyCode);
                            break;
                        case KeyState.Click:
                            inputSimulator.Keyboard.KeyDown(keyboardCmd.KeyCode);
                            inputSimulator.Keyboard.KeyUp(keyboardCmd.KeyCode);
                            break;
                    }
                    break;
            }
        }

        private void myTcpServer_ClientDisconnected(object? sender, MyTcpClientInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                disconnectBtn_Click(sender, null);
            });
        }

        private void myTcpServer_ClientConnected(object? sender, MyTcpClientInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                waitRespondSp.Visibility = Visibility.Collapsed;
                disconnectSp.Visibility = Visibility.Visible;
            });

            DeviceConnected?.Invoke(this, EventArgs.Empty);

            if (myTcpServer != null)
            {
                CreateVirtualScreen(1920, 1080);
                
                lock (myTcpServer.ConnectedClients)
                {
                    if (myTcpServer.ConnectedClients.Count > 0)
                        myTcpServer.SendCmd(
                            new ScreenInfoCmd(Screen.AllScreens.Last().Bounds.Width, Screen.AllScreens.Last().Bounds.Height),
                            myTcpServer.ConnectedClients.First());
                }

                int screenIndex = Screen.AllScreens.Length - 1;
                ScreenCaptureDX.FindScreen(Screen.AllScreens[screenIndex].DeviceName, out int adapterIndex, out int outputIndex);
                screenCaptureDX = new ScreenCaptureDX(adapterIndex, outputIndex);
                screenCaptureDX.Start((Bitmap bitmap) =>
                {
                    if (myTcpServer.CurState == MyTcpServerState.Listening)
                    {
                        try
                        {
                            myTcpServer.SendCmd(new ScreenCmd(bitmap, quality), myTcpServer.ConnectedClients.First());
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                });
            }
        }

        private void CreateVirtualScreen(int width, int height)
        {
            virtualDisplayProcess = new Process();
            virtualDisplayProcess.StartInfo.UseShellExecute = true;
            virtualDisplayProcess.StartInfo.Arguments = "enableidd 1";
            virtualDisplayProcess.StartInfo.FileName = ".\\usbmmidd_v2\\deviceinstaller64.exe";
            virtualDisplayProcess.StartInfo.Verb = "runas";
            virtualDisplayProcess.Start();
            virtualDisplayProcess.WaitForExit();
            Task.Delay(5000).Wait();

            // change resolution
            string virtualScreenName = Screen.AllScreens[Screen.AllScreens.Length - 1].DeviceName;
            DisplaySettingsChanger.ChangeScreenResolution(virtualScreenName, width, height, 60);
        }

        private void CloseVirtualScreen()
        {
            if (virtualDisplayProcess == null) return;
            virtualDisplayProcess.StartInfo.Arguments = "enableidd 0";
            virtualDisplayProcess.Start();
            virtualDisplayProcess.WaitForExit();
            virtualDisplayProcess = null;
        }

        public void StopAll()
        {
            deviceFinder.StopSearching();
            Disconnect();
        }

        private void disconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();

            maskBorder.Visibility = Visibility.Collapsed;
            waitRespondSp.Visibility = Visibility.Collapsed;
            disconnectSp.Visibility = Visibility.Collapsed;

            deviceFinder.StartSearching();
        }

        private void Disconnect()
        {
            screenCaptureDX?.Stop();
            if (myTcpServer?.CurState == MyTcpServerState.Listening)
            {
                try
                {
                    myTcpServer.SendCmd(new RequestCmd(RequestType.Disconnect, Environment.MachineName), myTcpServer.ConnectedClients.First());
                }
                catch { }
                myTcpServer?.Stop();
            }
            CloseVirtualScreen();

            DeviceDisconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
