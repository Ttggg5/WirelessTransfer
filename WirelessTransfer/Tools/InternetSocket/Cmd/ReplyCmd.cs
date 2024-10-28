using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public enum ReplyType
    {
        Accept,
        Refuse,
    }

    public class ReplyCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = replyType
        //---------------------------------------------------------------------------------

        public ReplyType ReplyType { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public ReplyCmd(ReplyType replyType)
        {
            ReplyType = replyType;
            CmdType = CmdType.Reply;
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public ReplyCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.Reply;
        }

        public override byte[] Encode()
        {
            Data = Encoding.ASCII.GetBytes(ReplyType.ToString());
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            string tmp = Encoding.ASCII.GetString(Data);
            ReplyType = Enum.Parse<ReplyType>(tmp);
        }
    }
}
