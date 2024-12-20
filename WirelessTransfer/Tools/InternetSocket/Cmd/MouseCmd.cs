﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Point = System.Windows.Point;
using Cursor = System.Windows.Forms.Cursor;
using Cursors = System.Windows.Forms.Cursors;
using System.Windows.Input;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static System.Windows.Forms.AxHost;
using WindowsInput.Native;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public enum MouseAct
    {
        RightButtonDown = 0x0008,
        RightButtonUp = 0x0010,
        LeftButtonDown = 0x0002,
        LeftButtonUp = 0x0004,
        MiddleButtonDown = 0x0020,
        MiddleButtonUp = 0x0040,
        MiddleButtonRolled = 0x0800,
        None,
    }

    public class MouseCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = mousePos.X + "," + mousePos.Y + "," + mouseAct + "," + middleButtonMomentum + "," + moveMouse
        //---------------------------------------------------------------------------------

        public Point MousePos { get; private set; }
        public MouseAct MouseAct { get; private set; }
        public int MiddleButtonMomentum { get; private set; }
        public bool MoveMouse { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public MouseCmd(Point mousePos, MouseAct mouseAct, int middleButtonMomentum, bool moveMouse)
        {
            MousePos = mousePos;
            MouseAct = mouseAct;
            MiddleButtonMomentum = middleButtonMomentum;
            MoveMouse = moveMouse;
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
                MouseAct.ToString() + "," +
                MiddleButtonMomentum.ToString() + "," +
                MoveMouse.ToString());
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            string[] tmp = Encoding.ASCII.GetString(Data).Split(",");
            MousePos = new Point(double.Parse(tmp[0]), double.Parse(tmp[1]));
            MouseAct = Enum.Parse<MouseAct>(tmp[2]);
            MiddleButtonMomentum = int.Parse(tmp[3]);
            MoveMouse = bool.Parse(tmp[4]);
        }
    }
}
