using System.Net;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Net.Sockets;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using Ini;
using WirelessTransfer.Tools.InternetSocket.MyTcp;
using WirelessTransfer.Windows;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// HomePage.xaml 的互動邏輯
    /// </summary>
    public partial class HomePage : Page
    {
        public event EventHandler BackSignal;
        public event EventHandler<Page> NavigateSignal;

        int port;

        BasePage basePage;
        UdpClient udpListen; // Listening for request and send back info

        public HomePage()
        {
            InitializeComponent();

            port = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, IniFile.DEFAULT_PATH));

            deviceNameTB.Text = Environment.MachineName;
            deviceIpTB.Text = GetLocalIPAddress();
            wifiNameTB.Text = GetSSID();

            ListenForConnections();
        }

        private string GetSSID()
        {
            string notFoundMessage = "No wifi connected!";
            try
            {
                // Run the netsh command to get the Wi-Fi info
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Extract the SSID from the output
                int startIndex = output.IndexOf("SSID");
                if (startIndex == -1) return notFoundMessage;
                startIndex = output.IndexOf(":", startIndex + 4);
                if (startIndex == -1) return notFoundMessage;
                int endIndex = output.IndexOf("\n", startIndex + 1);
                if (endIndex == -1) return notFoundMessage;
                return output.Substring(startIndex + 1, endIndex - startIndex);
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Error: " + ex.Message);
                return notFoundMessage;
            }
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "Unknown";
        }

        private void ListenForConnections()
        {
            if (udpListen == null)
            {
                udpListen = new UdpClient(new IPEndPoint(IPAddress.Any, port));
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
                    switch (((RequestCmd)cmd).RequestType)
                    {
                        case RequestType.ClientInfo:
                            ClientInfoCmd clientInfoCmd = new ClientInfoCmd(Environment.MachineName, IPAddress.Parse(GetLocalIPAddress()));
                            byte[] bytes = clientInfoCmd.Encode();
                            udpListen.Send(bytes, bytes.Length, remoteEP);
                            break;
                        case RequestType.Mirror:
                            Dispatcher.Invoke(() =>
                            {
                                MyTcpClient myTcpClient = new MyTcpClient(remoteEP.Address, int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.TcpPort, IniFile.DEFAULT_PATH)), Environment.MachineName);
                                StopListening();
                                MirrorWindow mirrorWindow = new MirrorWindow(myTcpClient);
                                mirrorWindow.ShowDialog();
                                myTcpClient.Disconnect();
                                ListenForConnections();
                            });
                            break;
                        case RequestType.Extend:
                            break;
                        case RequestType.FileShare:
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

            basePage?.StopAllProcess();
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
    }
}
