using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public class MouseMoveCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = MouseDisplacementX + "," + MouseDisplacementY
        //---------------------------------------------------------------------------------

        public float MouseDisplacementX { get; private set; }
        public float MouseDisplacementY { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public MouseMoveCmd(float mouseDisplacementX, float mouseDisplacementY)
        {
            MouseDisplacementX = mouseDisplacementX;
            MouseDisplacementY = mouseDisplacementY;
            CmdType = CmdType.MouseMove;
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public MouseMoveCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.MouseMove;
        }

        public override byte[] Encode()
        {
            Data = Encoding.ASCII.GetBytes(
                MouseDisplacementX.ToString() + "," +
                MouseDisplacementY.ToString());
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            string[] tmp = Encoding.ASCII.GetString(Data).Split(",");
            MouseDisplacementX = float.Parse(tmp[0]);
            MouseDisplacementY = float.Parse(tmp[1]);
        }
    }
}
