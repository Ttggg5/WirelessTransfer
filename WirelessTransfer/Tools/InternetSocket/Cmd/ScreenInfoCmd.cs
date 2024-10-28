using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public class ScreenInfoCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = width + "," + height
        //---------------------------------------------------------------------------------

        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public ScreenInfoCmd(int width, int height)
        {
            Width = width;
            Height = height;
            CmdType = CmdType.ScreenInfo;
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public ScreenInfoCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.ScreenInfo;
        }

        public override byte[] Encode()
        {
            Data = Encoding.ASCII.GetBytes(Width.ToString() + "," + Height.ToString());
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            string[] tmp = Encoding.ASCII.GetString(Data).Split(",");
            Width = int.Parse(tmp[0]);
            Height = int.Parse(tmp[1]);
        }
    }
}
