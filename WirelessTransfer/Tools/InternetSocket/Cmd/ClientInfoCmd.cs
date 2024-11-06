using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public class ClientInfoCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = clientNameLength + clientName + IP
        //---------------------------------------------------------------------------------
        // clientNameLength: 3 bytes to present (fill 0 in front if it not full).

        public string ClientName { get; private set; }
        public IPAddress IP { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public ClientInfoCmd(string clientName, IPAddress ip)
        {
            ClientName = clientName;
            IP = ip;
            CmdType = CmdType.ClientInfo;
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public ClientInfoCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.ClientInfo;
        }

        public override byte[] Encode()
        {
            byte[] tmp = Encoding.UTF8.GetBytes(ClientName);
            Data = Encoding.UTF8.GetBytes(tmp.Length.ToString("000") + ClientName + IP.ToString());
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            int nameLength = int.Parse(Encoding.UTF8.GetString(Data.Take(3).ToArray()));
            ClientName = Encoding.UTF8.GetString(Data.Skip(3).Take(nameLength).ToArray());
            IP = IPAddress.Parse(Encoding.UTF8.GetString(Data.Skip(nameLength + 3).ToArray()));
        }
    }
}
