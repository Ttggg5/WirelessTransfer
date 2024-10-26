using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace WirelessTransfer.CustomControls
{
    /// <summary>
    /// DeviceTag.xaml 的互動邏輯
    /// </summary>
    public partial class DeviceTag : System.Windows.Controls.UserControl
    {
        public string DeviceName { get; }
        public IPAddress Address { get; }
        public DateTime FoundTime { get; set; }

        public DeviceTag(string deviceName, IPAddress address, DateTime foundTime)
        {
            InitializeComponent();

            DeviceName = deviceName;
            Address = address;

            nameTB.Text = deviceName;
            addressTB.Text = address.ToString();
            FoundTime = foundTime;
        }
    }
}
