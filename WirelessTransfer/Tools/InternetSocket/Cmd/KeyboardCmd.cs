using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WindowsInput.Native;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public enum KeyState
    {
        Down,
        Up,
        Click,
    }

    public class KeyboardCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = keyCode + "," + state
        //---------------------------------------------------------------------------------

        public VirtualKeyCode KeyCode { get; private set; }
        public KeyState State { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public KeyboardCmd(VirtualKeyCode keyCode, KeyState state)
        {
            KeyCode = keyCode;
            State = state;
            CmdType = CmdType.Keyboard;
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public KeyboardCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.Keyboard;
        }

        public override byte[] Encode()
        {
            Data = Encoding.ASCII.GetBytes(KeyCode.ToString() + "," + State.ToString());
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            string[] tmp = Encoding.ASCII.GetString(Data).Split(",");
            KeyCode = Enum.Parse<VirtualKeyCode>(tmp[0]);
            State = Enum.Parse<KeyState>(tmp[1]);
        }
    }
}
