using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public enum RequestType
    {
        ClientInfo,
        Mirror,
        Extend,
        FileShare,
    }

    public class RequestCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = requestType
        //---------------------------------------------------------------------------------
        // requestType: RequestType's string value.

        public RequestType RequestType { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public RequestCmd(RequestType requestType)
        {
            RequestType = requestType;
            CmdType = CmdType.Request;
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public RequestCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.Request;
        }

        public override byte[] Encode()
        {
            Data = Encoding.UTF8.GetBytes(RequestType.ToString());
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            string tmp = Encoding.UTF8.GetString(Data);
            if (Enum.TryParse<RequestType>(tmp, out RequestType result)) RequestType = result;
        }
    }
}
