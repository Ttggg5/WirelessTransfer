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
        Disconnect,
        ClientInfo,
        Mirror,
        Extend,
        FileShare,
    }

    public class RequestCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = requestType + "," + deviceName
        //---------------------------------------------------------------------------------
        // requestType: RequestType's string value.

        public RequestType RequestType { get; private set; }
        public string DeviceName { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public RequestCmd(RequestType requestType, string deviceName)
        {
            RequestType = requestType;
            DeviceName = deviceName;
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
            Data = Encoding.UTF8.GetBytes(RequestType.ToString() + "," + DeviceName);
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            string[] tmp = Encoding.UTF8.GetString(Data).Split(",");
            if (Enum.TryParse<RequestType>(tmp[0], out RequestType result)) RequestType = result;
            DeviceName = tmp[1];
        }
    }
}
