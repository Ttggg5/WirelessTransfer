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
using WirelessTransfer.CustomButtons;
using WirelessTransfer.Pages;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyUdp;

namespace WirelessTransfer.CustomControls
{
    public enum DeviceFinderState
    {
        Searching,
        Stopped,
    }

    /// <summary>
    /// DeviceFinder.xaml 的互動邏輯
    /// </summary>
    public partial class DeviceFinder : System.Windows.Controls.UserControl
    {
        public event EventHandler<DeviceTag> DeviceChoosed;

        public PageFunction Function
        {
            get { return (PageFunction)GetValue(FunctionProperty); }
            set { SetValue(FunctionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PageFunction.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FunctionProperty =
            DependencyProperty.Register("PageFunction", typeof(PageFunction), typeof(DeviceFinder), new PropertyMetadata(PageFunction.Mirror));

        const int SEARCH_CYCLE = 1; // unit is "second"

        public DeviceFinderState State { get; private set; }

        int port;

        List<DeviceTag> deviceTags;
        UdpClient searchClient;

        public DeviceFinder()
        {
            InitializeComponent();

            port = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, IniFile.DEFAULT_PATH));
            deviceTags = new List<DeviceTag>();

            searchClient = new UdpClient();
            searchClient.EnableBroadcast = true;
            searchClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

            State = DeviceFinderState.Stopped;

            /*
            DeviceTag deviceTag = new DeviceTag("test", IPAddress.Any);
            deviceTag.Width = 230;
            deviceTag.Height = 60;
            foundDevicesListBox.Items.Add(deviceTag);
            */
        }

        public void StartSearching()
        {
            if (State == DeviceFinderState.Searching) return;

            try
            {
                searchClient.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
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

                            // Pc request
                            RequestCmd requestClientInfoCmd = new RequestCmd(RequestType.PcClientInfo, Environment.MachineName);
                            byte[] sendBytes = requestClientInfoCmd.Encode();
                            searchClient?.Send(sendBytes, sendBytes.Length, new IPEndPoint(IPAddress.Broadcast, port));

                            // Phone request
                            PageFunction tmp = PageFunction.Setting;
                            Dispatcher.Invoke(() =>
                            {
                                tmp = Function;
                            });
                            if (tmp == PageFunction.Mirror || tmp == PageFunction.Extend)
                                requestClientInfoCmd = new RequestCmd(RequestType.PhoneClientInfoShareScreen, Environment.MachineName);
                            else
                                requestClientInfoCmd = new RequestCmd(RequestType.PhoneClientInfoFileShare, Environment.MachineName);
                            sendBytes = requestClientInfoCmd.Encode();
                            searchClient?.Send(sendBytes, sendBytes.Length, new IPEndPoint(IPAddress.Broadcast, port));

                            // Waiting
                            for (int i = 0; i < SEARCH_CYCLE * 10; i++)
                            {
                                Task.Delay(100).Wait();
                                if (searchClient == null)
                                    throw new ObjectDisposedException("");
                            }
                        }
                    }
                    catch (Exception)
                    {
                        searchClient?.Close();
                        State = DeviceFinderState.Stopped;
                    }
                });
                State = DeviceFinderState.Searching;
            }
            catch { }
        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                IPEndPoint? remoteEP = null;
                byte[] receiveBytes = searchClient.EndReceive(ar, ref remoteEP);
                if (State == DeviceFinderState.Stopped) 
                    return;

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
                State = DeviceFinderState.Stopped;
            }
        }

        public void StopSearching()
        {
            searchClient?.Close();
            State = DeviceFinderState.Stopped;
        }

        private void foundDevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (foundDevicesListBox.SelectedIndex > -1)
            {
                DeviceChoosed?.Invoke(this, (DeviceTag)foundDevicesListBox.SelectedItem);
                foundDevicesListBox.SelectedIndex = -1;
            } 
        }
    }
}
