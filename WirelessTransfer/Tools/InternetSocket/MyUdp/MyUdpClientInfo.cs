using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.MyUdp
{
    internal class MyUdpClientInfo
    {
        public UdpClient Client { get; }
        public string Name { get; }
        public IPAddress Address { get; }

        public MyUdpClientInfo(UdpClient client, string name, IPAddress address)
        {
            Client = client;
            Name = name;
            Address = address;
        }
    }
}
