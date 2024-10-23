using System;
using System.Collections.Generic;
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
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyUdp;

namespace WirelessTransfer.CustomControls
{
    /// <summary>
    /// DeviceFinder.xaml 的互動邏輯
    /// </summary>
    public partial class DeviceFinder : System.Windows.Controls.UserControl
    {
        const int PORT = 8888;

        List<MyUdpClientInfo> myUdpClientInfos;
        UdpClient searchClient;

        public DeviceFinder()
        {
            InitializeComponent();
            myUdpClientInfos = new List<MyUdpClientInfo>();

            /*
            DeviceTag deviceTag = new DeviceTag("test", IPAddress.Any);
            deviceTag.Width = 230;
            deviceTag.Height = 60;
            foundDevicesListBox.Items.Add(deviceTag);
            */
            StartSearching();
        }

        public void StartSearching()
        {
            searchClient = new UdpClient();
            searchClient.EnableBroadcast = true;
            RequestClientInfoCmd requestClientInfoCmd = new RequestClientInfoCmd();
            byte[] sendBytes = requestClientInfoCmd.Encode();
            searchClient.BeginSend(sendBytes, sendBytes.Length, new IPEndPoint(IPAddress.Broadcast, PORT), new AsyncCallback(SendCallBack), null);
        }

        private void SendCallBack(IAsyncResult ar)
        {
            searchClient.EndSend(ar);
            searchClient.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                IPEndPoint? remoteEP = null;
                byte[] receiveBytes = searchClient.EndReceive(ar, ref remoteEP);
                Cmd cmd = CmdDecoder.DecodeCmd(receiveBytes, 0, receiveBytes.Length);
                if (cmd?.CmdType == CmdType.ClientInfo)
                {
                    ((ClientInfoCmd)cmd).Decode();
                    MyUdpClientInfo clientInfo = new MyUdpClientInfo(new UdpClient(), ((ClientInfoCmd)cmd).ClientName, ((ClientInfoCmd)cmd).IP);
                    bool found = false;
                    foreach (MyUdpClientInfo muci in myUdpClientInfos)
                    {
                        if (muci.Address.ToString().Equals(clientInfo.Address.ToString()))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            myUdpClientInfos.Add(clientInfo);
                            DeviceTag deviceTag = new DeviceTag(clientInfo.Name, clientInfo.Address);
                            deviceTag.Width = 230;
                            deviceTag.Height = 60;
                            foundDevicesListBox.Items.Add(deviceTag);
                        });
                    }
                }

                searchClient.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
            }
            catch (ObjectDisposedException) { }
        }

        public void StopSearching()
        {
            try
            {
                searchClient?.Close();
            }
            catch { }
        }
    }
}
