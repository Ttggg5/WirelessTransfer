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
            if (searchClient == null)
            {
                searchClient = new UdpClient();
                searchClient.EnableBroadcast = true;
                Task.Run(() =>
                {
                    try
                    {
                        // Send broadcast message ever 3 sec
                        while (true)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                myUdpClientInfos.Clear();
                                foundDevicesListBox.Items.Clear();
                            });
                            RequestClientInfoCmd requestClientInfoCmd = new RequestClientInfoCmd();
                            byte[] sendBytes = requestClientInfoCmd.Encode();
                            searchClient.Send(sendBytes, sendBytes.Length, new IPEndPoint(IPAddress.Broadcast, PORT));

                            // Waiting 3 sec
                            for (int i = 0; i < 30; i++)
                            {
                                Task.Delay(100).Wait();
                                if (searchClient == null)
                                    throw new ObjectDisposedException("");
                            }
                        }
                    }
                    catch (ObjectDisposedException) { }
                });
                searchClient.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));
                searchClient.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
            }
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
                    ClientInfoCmd cif = ((ClientInfoCmd)cmd);
                    cif.Decode();
                    MyUdpClientInfo clientInfo = new MyUdpClientInfo(new UdpClient(new IPEndPoint(cif.IP, PORT)), cif.ClientName, cif.IP);
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
            catch (ObjectDisposedException)
            {
                searchClient = null;
            }
        }

        public void StopSearching()
        {
            searchClient?.Close();
        }
    }
}
