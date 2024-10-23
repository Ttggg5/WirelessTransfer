using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.MyTcp
{
    public class MyTcpClientInfo
    {
        public TcpClient Client { get; }
        public string Name { get; }
        public IPAddress Address { get; }

        public MyTcpClientInfo(TcpClient client, string name, IPAddress address)
        {
            Client = client;
            Name = name;
            Address = address;
        }
    }
}
