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
        static byte frontSymbol = Encoding.UTF8.GetBytes("<")[0];
        static byte backSymbol = Encoding.UTF8.GetBytes(">")[0];
        static byte endSymbol = Encoding.UTF8.GetBytes("!")[0];

        /// <summary>
        /// Correct message format:
        ///---------------------------------------------------------------------------------
        /// <CmdType> + DataLength + Data + <!CmdType>
        ///---------------------------------------------------------------------------------
        /// CmdType: no limit.
        /// DataLength: 7 bytes to present (max: 5242880 bytes = 5MB), fill 0 in front if it not full.
        /// Data: max length is 5242880 bytes.
        /// </summary>
        public static Cmd? DecodeCmd(byte[] buffer, int startIndex, int length)
        {
            Cmd? cmd = null;
            CmdType cmdType;
            if (buffer[startIndex] == frontSymbol)
            {
                int fl = startIndex + length >= buffer.Length ? buffer.Length - length : length;
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
                            string cmdStr = Encoding.UTF8.GetString(tmpBuffer, previousIndex, curIndex - previousIndex);
                            if(!Enum.TryParse<CmdType>(cmdStr, out cmdType))
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
                if(!int.TryParse(tmpBuffer.Skip(previousIndex).Take(7).ToArray(), out int dataLength))
                    return null;

                // find end symbol
                curIndex = previousIndex + 7 + dataLength + 1;
                if (tmpBuffer[curIndex] == endSymbol)
                {
                    // create cmd class
                    byte[] data = tmpBuffer.Skip(previousIndex + 7).Take(dataLength).ToArray();
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
                            break;
                        case CmdType.Screen:
                            break;
                        case CmdType.Keyboard:
                            break;
                        case CmdType.Mouse:
                            break;
                        case CmdType.Webcam:
                            break;
                        case CmdType.Request:
                            cmd = new RequestCmd(data);
                            break;
                    }
                    cmd?.Decode();
                }
            }
            return cmd;
        }
    }
}
