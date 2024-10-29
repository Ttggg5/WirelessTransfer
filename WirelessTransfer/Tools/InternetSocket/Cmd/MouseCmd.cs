using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Point = System.Windows.Point;
using System.Windows.Input;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static System.Windows.Forms.AxHost;
using WindowsInput.Native;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public class MouseCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = mousePos.X + "," + mousePos.Y + "," + mouseAction
        //---------------------------------------------------------------------------------

        public Point MousePos { get; private set; }
        public MouseAction MouseAction { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public MouseCmd(Point mousePos, MouseAction mouseAction)
        {
            MousePos = mousePos;
            MouseAction = mouseAction;
            CmdType = CmdType.Mouse;
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public MouseCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.Mouse;
        }

        public override byte[] Encode()
        {
            Data = Encoding.ASCII.GetBytes(
                MousePos.X.ToString() + "," + 
                MousePos.Y.ToString() + "," + 
                MouseAction.ToString());
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            string[] tmp = Encoding.ASCII.GetString(Data).Split(",");
            MousePos = new Point(double.Parse(tmp[0]), double.Parse(tmp[1]));
            MouseAction = Enum.Parse<MouseAction>(tmp[2]);
        }
    }
}
