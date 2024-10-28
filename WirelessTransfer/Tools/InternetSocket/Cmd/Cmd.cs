using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public enum CmdType
    {
        Alive,
        ClientInfo,
        FileData,
        FileInfo,
        Reply,
        Screen,
        Keyboard,
        Mouse,
        Webcam,
        Request,
        ScreenInfo,
    }

    public abstract class Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // <CmdType> + DataLength + Data + <!CmdType>
        //---------------------------------------------------------------------------------
        // CmdType: no limit.
        // DataLength: 7 byte(max: 5242880 bytes = 5MB), fill 0 in front if it not full.
        // Data: max length is 5242880 bytes.

        public CmdType CmdType { get; protected set; }
        protected byte[] Data { get; set; } // not included head and tail

        public abstract byte[] Encode();

        public abstract void Decode();

        protected byte[] AddHeadTail(byte[] data)
        {
            string cmd = Enum.GetName(typeof(CmdType), CmdType);

            byte[] headBytes = Encoding.ASCII.GetBytes("<" + cmd + ">");
            byte[] tailBytes = Encoding.ASCII.GetBytes("<!" + cmd + ">");
            byte[] dataLengthBytes = Encoding.ASCII.GetBytes(data.Length.ToString("0000000"));
            byte[] fullBytes = new byte[headBytes.Length + dataLengthBytes.Length + data.Length + tailBytes.Length];

            int index = 0;
            Array.Copy(headBytes, 0, fullBytes, index, headBytes.Length);
            index += headBytes.Length;
            Array.Copy(dataLengthBytes, 0, fullBytes, index, dataLengthBytes.Length);
            index += dataLengthBytes.Length;
            Array.Copy(data, 0, fullBytes, index, data.Length);
            index += data.Length;
            Array.Copy(tailBytes, 0, fullBytes, index, tailBytes.Length);

            return fullBytes;
        }
    }
}
