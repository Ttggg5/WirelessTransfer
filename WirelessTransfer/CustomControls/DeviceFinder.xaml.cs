using Ini;
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
        public event EventHandler<DeviceTag> DeviceChoosed;

        const int SEARCH_CYCLE = 1; // unit is "second"

        int port;

        List<DeviceTag> deviceTags;
        UdpClient searchClient;

        public DeviceFinder()
        {
            InitializeComponent();

            port = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, IniFile.DEFAULT_PATH));
            deviceTags = new List<DeviceTag>();

            /*
            DeviceTag deviceTag = new DeviceTag("test", IPAddress.Any);
            deviceTag.Width = 230;
            deviceTag.Height = 60;
            foundDevicesListBox.Items.Add(deviceTag);
            */
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
                        // Send broadcast message
                        while (true)
                        {
                            // delete found device when no respond
                            for (int i = 0; i < deviceTags.Count; i++)
                            {
                                if (Math.Abs(deviceTags[i].FoundTime.Second - DateTime.Now.Second) > SEARCH_CYCLE * 2)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        foundDevicesListBox.Items.Remove(deviceTags[i]);
                                        deviceTags.RemoveAt(i--);
                                    });
                                }
                            }

                            RequestCmd requestClientInfoCmd = new RequestCmd(RequestType.ClientInfo);
                            byte[] sendBytes = requestClientInfoCmd.Encode();
                            searchClient.Send(sendBytes, sendBytes.Length, new IPEndPoint(IPAddress.Broadcast, port));

                            // Waiting
                            for (int i = 0; i < SEARCH_CYCLE * 10; i++)
                            {
                                Task.Delay(100).Wait();
                                if (searchClient == null)
                                    throw new ObjectDisposedException("");
                            }
                        }
                    }
                    catch (ObjectDisposedException) { }
                });
                searchClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
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
                if (cmd != null && cmd.CmdType == CmdType.ClientInfo)
                {
                    ClientInfoCmd cif = ((ClientInfoCmd)cmd);
                    cif.Decode();
                    bool found = false;
                    foreach (DeviceTag dt in deviceTags)
                    {
                        if (dt.Address.ToString().Equals(cif.IP.ToString()))
                        {
                            found = true;
                            dt.FoundTime = DateTime.Now;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            DeviceTag deviceTag = new DeviceTag(cif.ClientName, cif.IP, DateTime.Now);
                            deviceTag.Width = 230;
                            deviceTag.Height = 60;
                            foundDevicesListBox.Items.Add(deviceTag);
                            deviceTags.Add(deviceTag);
                        });
                    }
                }

                searchClient.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
            }
            catch
            {
                searchClient?.Close();
                searchClient = null;
            }
        }

        public void StopSearching()
        {
            searchClient?.Close();
        }

        private void foundDevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (foundDevicesListBox.SelectedIndex > -1)
                DeviceChoosed?.Invoke(this, (DeviceTag)foundDevicesListBox.SelectedItem);
        }
    }
}
