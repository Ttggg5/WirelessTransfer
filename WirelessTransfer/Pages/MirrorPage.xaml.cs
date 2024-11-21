using Ini;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyTcp;
using WirelessTransfer.Tools.Screen;
using WirelessTransfer.Windows;
using WindowsInput;
using System.Windows.Forms;
using System.Net;
using ScreenCapturerNS;
using SharpDX.DXGI;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// MirrorPage.xaml 的互動邏輯
    /// </summary>
    public partial class MirrorPage : Page
    {
        public event EventHandler<CustomControls.DeviceTag> DeviceChoosed;
        public event EventHandler DeviceConnected;
        public event EventHandler DeviceDisconnected;

        const int MAX_CLIENT = 1;

        int udpPort, tcpPort;
        MyTcpServer myTcpServer;
        ScreenCaptureDX screenCaptureDX;
        InputSimulator inputSimulator;

        public MirrorPage()
        {
            InitializeComponent();

            udpPort = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, IniFile.DEFAULT_PATH));
            tcpPort = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.TcpPort, IniFile.DEFAULT_PATH));

            inputSimulator = new InputSimulator();

            maskBorder.Visibility = Visibility.Collapsed;
            waitRespondSp.Visibility = Visibility.Collapsed;
            disconnectSp.Visibility = Visibility.Collapsed;

            Tag = PageFunction.Mirror;
            deviceFinder.DeviceChoosed += deviceFinder_DeviceChoosed;
            deviceFinder.StartSearching();
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
            byte[] bytes = new RequestCmd(RequestType.Mirror, Environment.MachineName).Encode();
            udpClient.Send(bytes, bytes.Length, new IPEndPoint(e.Address, udpPort));

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
                                break;
                            }
                            else break;
                        }
                        else
                        {
                            /*
                            Dispatcher.BeginInvoke(() =>
                            {
                                disconnectBtn_Click(this, null);
                                MessageWindow messageWindow = new MessageWindow("Error reply!", false);
                                messageWindow.ShowDialog();
                            });
                            */
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
                        break;
                    }
                }
                udpClient.Close();

                Task.Delay(3000).Wait();
                if (myTcpServer.ConnectedClients.Count == 0)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        disconnectBtn_Click(this, null);
                        MessageWindow messageWindow = new MessageWindow("連線超時!", false);
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
                    // move mouse
                    MouseCmd mouseCmd = (MouseCmd)e;
                    System.Windows.Forms.Cursor.Position = 
                        new System.Drawing.Point((int)mouseCmd.MousePos.X, (int)mouseCmd.MousePos.Y);

                    // do mouse action
                    if (mouseCmd.MouseAct != MouseAct.None)
                        mouse_event(
                            (int)mouseCmd.MouseAct, 
                            (int)mouseCmd.MousePos.X, (int)mouseCmd.MousePos.Y, 
                            mouseCmd.MiddleButtonMomentum, 0);
                    break;
                case CmdType.Keyboard:
                    KeyboardCmd keyboardCmd = (KeyboardCmd)e;
                    if (keyboardCmd.State == KeyState.Down)
                        inputSimulator.Keyboard.KeyDown(keyboardCmd.KeyCode);
                    else
                        inputSimulator.Keyboard.KeyUp(keyboardCmd.KeyCode);
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
                lock (myTcpServer.ConnectedClients)
                {
                    if (myTcpServer.ConnectedClients.Count > 0)
                        myTcpServer.SendCmd(
                            new ScreenInfoCmd(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height),
                            myTcpServer.ConnectedClients.First());
                }
            }

            int screenIndex = 0;
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                if (Screen.AllScreens[i].Primary)
                {
                    screenIndex = i;
                    break;
                }
            }

            int adapterIndex = 0;
            int outputIndex = 0;
            var factory = new Factory1();
            for (int i = 0; i < factory.Adapters1.Length; i++)
            {
                for (int j = 0; j < factory.Adapters1[i].Outputs.Length; j++)
                {
                    if (factory.Adapters1[i].Outputs[j].Description.DeviceName.Equals(Screen.AllScreens[screenIndex].DeviceName))
                    {
                        adapterIndex = i;
                        outputIndex = j;
                        break;
                    }
                }
            }

            ScreenCapturer.StartCapture((Bitmap bitmap) =>
            {
                if (myTcpServer.CurState == MyTcpServerState.Listening)
                {
                    try
                    {
                        //ScreenCaptureCpu.DrawCursor(bitmap, screenIndex, Screen.AllScreens[screenIndex].Bounds.Left, Screen.AllScreens[screenIndex].Bounds.Top);
                        myTcpServer.SendCmd(new ScreenCmd(bitmap), myTcpServer.ConnectedClients.First());
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }, outputIndex, adapterIndex);

            /*
            screenCaptureDX = new ScreenCaptureDX(0);
            screenCaptureDX.ScreenRefreshed += screenCaptureDX_ScreenRefreshed;
            screenCaptureDX.Start();
            */
        }

        private void screenCaptureDX_ScreenRefreshed(object? sender, Bitmap e)
        {
            if (myTcpServer.CurState == MyTcpServerState.Listening)
            {
                try
                {
                    myTcpServer.SendCmd(new ScreenCmd(e), myTcpServer.ConnectedClients.First());
                }
                catch { }
            }
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
            ScreenCapturer.StopCapture();
            if (myTcpServer?.CurState == MyTcpServerState.Listening)
            {
                try
                {
                    myTcpServer.SendCmd(new RequestCmd(RequestType.Disconnect, Environment.MachineName), myTcpServer.ConnectedClients.First());
                }
                catch { }
                myTcpServer?.Stop();
            }

            DeviceDisconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
