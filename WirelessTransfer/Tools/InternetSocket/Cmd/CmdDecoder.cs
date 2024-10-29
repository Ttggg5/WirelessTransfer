using WirelessTransfer.Tools.InternetSocket.MyTcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public static class CmdDecoder
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // <CmdType> + DataLength + Data + <!CmdType>
        //---------------------------------------------------------------------------------
        // CmdType: no limit.
        // DataLength: 7 bytes to present (max: 5242880 bytes = 5MB), fill 0 in front if it not full.
        // Data: max length is 5242880 bytes.

        static byte frontSymbol = Encoding.ASCII.GetBytes("<")[0];
        static byte backSymbol = Encoding.ASCII.GetBytes(">")[0];
        static byte endSymbol = Encoding.ASCII.GetBytes("!")[0];

        /// <summary>
        /// Special decode (has cycle).
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        public static Cmd? DecodeCmd(byte[] buffer, ref int startIndex, ref int endIndex)
        {
            Cmd? cmd = null;
            CmdType cmdType;
            string cmdStr;
            int length = endIndex - startIndex < 0 ? buffer.Length - startIndex + endIndex + 1 : endIndex - startIndex;
            if (buffer[startIndex] == frontSymbol)
            {
                int fl = startIndex + length > buffer.Length ? buffer.Length - startIndex : length;
                byte[] tmpBuffer = new byte[length];
                Array.Copy(buffer, startIndex, tmpBuffer, 0, fl);
                Array.Copy(buffer, 0, tmpBuffer, fl, length - fl);

                // find cmd type
                int previousIndex = 1;
                int curIndex = previousIndex;
                try
                {
                    while (true)
                    {
                        if (tmpBuffer[curIndex] == backSymbol)
                        {
                            cmdStr = Encoding.ASCII.GetString(tmpBuffer, previousIndex, curIndex - previousIndex);
                            if(!Enum.TryParse<CmdType>(cmdStr, out cmdType))
                            {
                                startIndex = endIndex;
                                return null;
                            }
                            break;
                        }
                        curIndex++;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    return null;
                }

                // find data length
                previousIndex = curIndex + 1;
                if(!int.TryParse(tmpBuffer.Skip(previousIndex).Take(7).ToArray(), out int dataLength))
                {
                    startIndex = endIndex;
                    return null;
                }

                if (dataLength > length) return null;

                // find end symbol
                curIndex = previousIndex + 7 + dataLength + 1;
                if (tmpBuffer[curIndex] == endSymbol)
                {
                    // create cmd class
                    byte[] data = tmpBuffer.Skip(previousIndex + 7).Take(dataLength).ToArray();
                    cmd = CreateDecodeCmd(cmdType, data);

                    startIndex += curIndex + cmdStr.Length + 2;
                    if (startIndex >= buffer.Length) startIndex -= buffer.Length;

                    return cmd;
                }
                else return null;
            }
            else
            {
                startIndex = endIndex;
                return null;
            }
        }

        /// <summary>
        /// Normal decode (no cycle).
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Cmd? DecodeCmd(byte[] buffer, int startIndex, int length)
        {
            Cmd? cmd = null;
            CmdType cmdType;
            if (buffer[startIndex] == frontSymbol)
            {
                byte[] tmpBuffer = new byte[length];
                Array.Copy(buffer, startIndex, tmpBuffer, 0, length);

                // find cmd type
                int previousIndex = 1;
                int curIndex = previousIndex;
                try
                {
                    while (true)
                    {
                        if (tmpBuffer[curIndex] == backSymbol)
                        {
                            string cmdStr = Encoding.ASCII.GetString(tmpBuffer, previousIndex, curIndex - previousIndex);
                            if (!Enum.TryParse<CmdType>(cmdStr, out cmdType))
                                return null;
                            break;
                        }
                        curIndex++;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    return null;
                }

                // find data length
                previousIndex = curIndex + 1;
                if (!int.TryParse(tmpBuffer.Skip(previousIndex).Take(7).ToArray(), out int dataLength))
                    return null;

                if (dataLength > length)
                    return null;

                // find end symbol
                curIndex = previousIndex + 7 + dataLength + 1;
                if (tmpBuffer[curIndex] == endSymbol)
                {
                    // create cmd class
                    byte[] data = tmpBuffer.Skip(previousIndex + 7).Take(dataLength).ToArray();
                    cmd = CreateDecodeCmd(cmdType, data);
                }
            }
            return cmd;
        }

        public static Cmd CreateDecodeCmd(CmdType cmdType, byte[] data)
        {
            Cmd cmd = null;
            switch (cmdType)
            {
                case CmdType.Alive:
                    break;
                case CmdType.ClientInfo:
                    cmd = new ClientInfoCmd(data);
                    break;
                case CmdType.FileData:
                    break;
                case CmdType.FileInfo:
                    break;
                case CmdType.Reply:
                    cmd = new ReplyCmd(data);
                    break;
                case CmdType.Screen:
                    cmd = new ScreenCmd(data);
                    break;
                case CmdType.Keyboard:
                    cmd = new KeyboardCmd(data);
                    break;
                case CmdType.Mouse:
                    cmd = new MouseCmd(data);
                    break;
                case CmdType.Webcam:
                    break;
                case CmdType.Request:
                    cmd = new RequestCmd(data);
                    break;
                case CmdType.ScreenInfo:
                    cmd = new ScreenInfoCmd(data);
                    break;
            }
            cmd?.Decode();
            return cmd;
        }
    }
}
