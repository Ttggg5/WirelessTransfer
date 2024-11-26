using System.Net;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Net.Sockets;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using Ini;
using WirelessTransfer.Tools.InternetSocket.MyTcp;
using WirelessTransfer.Windows;
using WirelessTransfer.Tools.InternetSocket;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// HomePage.xaml 的互動邏輯
    /// </summary>
    public partial class HomePage : Page
    {
        public event EventHandler BackSignal;
        public event EventHandler<Page> NavigateSignal;

        int udpPort, tcpPort;

        BasePage basePage;
        UdpClient udpListen; // Listening for request and send back info

        public HomePage()
        {
            InitializeComponent();

            udpPort = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, IniFile.DEFAULT_PATH));
            tcpPort = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.TcpPort, IniFile.DEFAULT_PATH));

            deviceNameTB.Text = Environment.MachineName;
            deviceIpTB.Text = InternetInfo.GetLocalIPAddress();
            wifiNameTB.Text = InternetInfo.GetSSID();

            ListenForConnections();
        }

        private void ListenForConnections()
        {
            if (udpListen == null)
            {
                udpListen = new UdpClient(new IPEndPoint(IPAddress.Any, udpPort));
                udpListen.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
            }
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                IPEndPoint? remoteEP = null;
                if (udpListen == null) throw new ObjectDisposedException(nameof(udpListen));
                byte[] receiveBytes = udpListen.EndReceive(ar, ref remoteEP);
                Cmd cmd = CmdDecoder.DecodeCmd(receiveBytes, 0, receiveBytes.Length);
                if (cmd != null && cmd.CmdType == CmdType.Request)
                {
                    byte[] tmpBytes;
                    RequestCmd requestCmd = (RequestCmd)cmd;
                    switch (requestCmd.RequestType)
                    {
                        case RequestType.PcClientInfo:
                            tmpBytes = new ClientInfoCmd(Environment.MachineName, IPAddress.Parse(InternetInfo.GetLocalIPAddress())).Encode();
                            udpListen.Send(tmpBytes, tmpBytes.Length, remoteEP);
                            break;
                        case RequestType.Mirror:
                        case RequestType.Extend:
                            Dispatcher.Invoke(() =>
                            {
                                MessageWindow messageWindow = new MessageWindow(
                                "\"" + requestCmd.DeviceName + "\" 正在嘗試與你分享螢幕，是否要接受連接?", true);
                                messageWindow.Topmost = true;
                                if (!(bool)messageWindow.ShowDialog())
                                {
                                    // refuse
                                    tmpBytes = new ReplyCmd(ReplyType.Refuse).Encode();
                                    udpListen.Send(tmpBytes, tmpBytes.Length, remoteEP);
                                }
                                else
                                {
                                    // accept
                                    tmpBytes = new ReplyCmd(ReplyType.Accept).Encode();
                                    udpListen.Send(tmpBytes, tmpBytes.Length, remoteEP);

                                    MyTcpClient myTcpClient = new MyTcpClient(remoteEP.Address, tcpPort, Environment.MachineName);
                                    StopListening();

                                    DisplayScreenWindow displayScreenWindow = new DisplayScreenWindow(myTcpClient);
                                    displayScreenWindow.ShowDialog();

                                    myTcpClient.Disconnect();
                                    ListenForConnections();
                                }
                            });
                            break;
                        case RequestType.FileShare:
                            Dispatcher.Invoke(() =>
                            {
                                MessageWindow messageWindow = new MessageWindow(
                                "\"" + requestCmd.DeviceName + "\" 正在嘗試與你分享檔案，是否要接受連接?", true);
                                messageWindow.Topmost = true;
                                if (!(bool)messageWindow.ShowDialog())
                                {
                                    // refuse
                                    tmpBytes = new ReplyCmd(ReplyType.Refuse).Encode();
                                    udpListen.Send(tmpBytes, tmpBytes.Length, remoteEP);
                                }
                                else
                                {
                                    // accept
                                    tmpBytes = new ReplyCmd(ReplyType.Accept).Encode();
                                    udpListen.Send(tmpBytes, tmpBytes.Length, remoteEP);

                                    MyTcpClient myTcpClient = new MyTcpClient(remoteEP.Address, tcpPort, Environment.MachineName);
                                    StopListening();

                                    FileReceiveWindow fileShareWindow = new FileReceiveWindow(myTcpClient);
                                    fileShareWindow.ShowDialog();

                                    myTcpClient.Disconnect();
                                    ListenForConnections();
                                }
                            });
                            break;
                    }
                }

                udpListen.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
            }
            catch (ObjectDisposedException)
            {
                udpListen = null;
            }
            catch (NullReferenceException) { }
        }

        private void StopListening()
        {
            udpListen?.Close();
            udpListen = null;
        }

        private void NavigateToNewPage(PageFunction function)
        {
            StopListening();

            //basePage?.StopAllProcess();
            basePage = new BasePage(function);
            basePage.Back += basePage_Back;
            NavigateSignal?.Invoke(this, basePage);
        }

        private void mirrorBtn_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToNewPage(PageFunction.Mirror);
        }

        private void extendBtn_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToNewPage(PageFunction.Extend);
        }

        private void fileShareBtn_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToNewPage(PageFunction.FileShare);
        }

        private void settingBtn_Click(object sender, MouseButtonEventArgs e)
        {
            NavigateToNewPage(PageFunction.Setting);
        }

        private void basePage_Back(object? sender, EventArgs e)
        {
            BackSignal?.Invoke(sender, e);
            basePage?.StopAllProcess();
            ListenForConnections();
        }

        public void StopAll()
        {
            basePage?.StopAllProcess();
            StopListening();
        }
    }
}
