using Ini;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyTcp;
using WirelessTransfer.Tools.Screen;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// MirrorPage.xaml 的互動邏輯
    /// </summary>
    public partial class MirrorPage : Page
    {
        const int MAX_CLIENT = 1;

        int port;
        MyTcpServer myTcpServer;
        ScreenCaptureDX screenCaptureDX;

        public MirrorPage()
        {
            InitializeComponent();

            port = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, IniFile.DEFAULT_PATH));

            maskGrid.Visibility = System.Windows.Visibility.Collapsed;

            Tag = PageFunction.Mirror;
            deviceFinder.DeviceChoosed += deviceFinder_DeviceChoosed;
            deviceFinder.StartSearching();
        }

        private void deviceFinder_DeviceChoosed(object? sender, CustomControls.DeviceTag e)
        {
            deviceFinder.StopSearching();
            maskGrid.Visibility = System.Windows.Visibility.Visible;

            myTcpServer = new MyTcpServer(int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.TcpPort, IniFile.DEFAULT_PATH)));
            myTcpServer.ClientConnected += myTcpServer_ClientConnected;
            myTcpServer.ClientDisconnected += myTcpServer_ClientDisconnected;
            myTcpServer.ReceivedCmd += myTcpServer_ReceivedCmd;
            myTcpServer.Start(MAX_CLIENT);

            // send request
            UdpClient udpClient = new UdpClient();
            byte[] bytes = new RequestCmd(RequestType.Mirror).Encode();
            udpClient.Send(bytes, bytes.Length, new System.Net.IPEndPoint(e.Address, port));

            // waiting for accept
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
        }

        public void StopSearching()
        {
            deviceFinder.StopSearching();
        }

        private void disconnectBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            screenCaptureDX.Stop();
            myTcpServer.Stop();
            maskGrid.Visibility = System.Windows.Visibility.Collapsed;
            deviceFinder.StartSearching();
        }
    }
}
