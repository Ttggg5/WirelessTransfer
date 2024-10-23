using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public class RequestClientInfoCmd : Cmd
    {
        // No data needed
        public RequestClientInfoCmd()
        {
            Data = new byte[0];
            CmdType = CmdType.RequestClientInfo;
        }

        public override byte[] Encode()
        {
            return AddHeadTail(Data);
        }

        public override void Decode()
        {

        }
    }
}
