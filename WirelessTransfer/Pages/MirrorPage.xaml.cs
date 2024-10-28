using Ini;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyTcp;
using WirelessTransfer.Tools.Screen;
using WirelessTransfer.Windows;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// MirrorPage.xaml 的互動邏輯
    /// </summary>
    public partial class MirrorPage : Page
    {
        const int MAX_CLIENT = 1;

        int udpPort, tcpPort;
        MyTcpServer myTcpServer;
        ScreenCaptureDX screenCaptureDX;

        public MirrorPage()
        {
            InitializeComponent();

            udpPort = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, IniFile.DEFAULT_PATH));
            tcpPort = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.TcpPort, IniFile.DEFAULT_PATH));

            maskGrid.Visibility = Visibility.Collapsed;
            waitRespondSp.Visibility = Visibility.Collapsed;
            disconnectSp.Visibility = Visibility.Collapsed;

            Tag = PageFunction.Mirror;
            deviceFinder.DeviceChoosed += deviceFinder_DeviceChoosed;
            deviceFinder.StartSearching();
        }

        private void deviceFinder_DeviceChoosed(object? sender, CustomControls.DeviceTag e)
        {
            deviceFinder.StopSearching();
            maskGrid.Visibility = Visibility.Visible;
            waitRespondSp.Visibility = Visibility.Visible;

            myTcpServer = new MyTcpServer(tcpPort);
            myTcpServer.ClientConnected += myTcpServer_ClientConnected;
            myTcpServer.ClientDisconnected += myTcpServer_ClientDisconnected;
            myTcpServer.ReceivedCmd += myTcpServer_ReceivedCmd;
            myTcpServer.Start(MAX_CLIENT);

            // send request
            UdpClient udpClient = new UdpClient();
            byte[] bytes = new RequestCmd(RequestType.Mirror, Environment.MachineName).Encode();
            udpClient.Send(bytes, bytes.Length, new System.Net.IPEndPoint(e.Address, udpPort));

            // waiting for accept
            Task<UdpReceiveResult> receiveTask = udpClient.ReceiveAsync();
            Task.Run(() =>
            {
                if (receiveTask.Wait(10000))
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
                        }
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
                }
                udpClient.Close();
            });
        }

        private void myTcpServer_ReceivedCmd(object? sender, Cmd e)
        {
            
        }

        private void myTcpServer_ClientDisconnected(object? sender, MyTcpClientInfo e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                disconnectBtn_Click(sender, null);
            });
        }

        private void myTcpServer_ClientConnected(object? sender, MyTcpClientInfo e)
        {
            waitRespondSp.Visibility = Visibility.Collapsed;
            disconnectSp.Visibility = Visibility.Visible;

            screenCaptureDX = new ScreenCaptureDX(0, 0);
            screenCaptureDX.ScreenRefreshed += screenCaptureDX_ScreenRefreshed;
            screenCaptureDX.Start();
        }

        private void screenCaptureDX_ScreenRefreshed(object? sender, Bitmap[] e)
        {
            lock (myTcpServer.ConnectedClients)
            {
                if (myTcpServer.ConnectedClients.Count > 0)
                    myTcpServer.SendCmd(new ScreenCmd(e.First()), myTcpServer.ConnectedClients.First());
            }
            Task.Delay(20).Wait();
        }

        public void StopSearching()
        {
            deviceFinder.StopSearching();
        }

        private void disconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            screenCaptureDX?.Stop();
            myTcpServer?.Stop();
            maskGrid.Visibility = Visibility.Collapsed;
            waitRespondSp.Visibility = Visibility.Collapsed;
            disconnectSp.Visibility = Visibility.Collapsed;
            deviceFinder.StartSearching();
        }
    }
}
