using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public class FileInfoCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = fileNameLength + fileName + fileSize + "," + md5
        //---------------------------------------------------------------------------------
        // fileNameLength: 3 bytes to present (fill 0 in front if it not full).

        public string FileName { get; private set; }
        public long FileSize { get; private set; }
        public string MD5 { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public FileInfoCmd(string fileName, long fileSize, string md5)
        {
            FileName = fileName;
            FileSize = fileSize;
            MD5 = md5;
            CmdType = CmdType.FileInfo;
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public FileInfoCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.FileInfo;
        }

        public override byte[] Encode()
        {
            byte[] tmp = Encoding.UTF8.GetBytes(FileName);
            Data = Encoding.UTF8.GetBytes(tmp.Length.ToString("000") + FileName + 
                FileSize.ToString() + "," + MD5);
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            int nameLength = int.Parse(Encoding.UTF8.GetString(Data.Take(3).ToArray()));
            FileName = Encoding.UTF8.GetString(Data.Skip(3).Take(nameLength).ToArray());
            string[] tmp = Encoding.UTF8.GetString(Data.Skip(nameLength + 3).ToArray()).Split(",");
            FileSize = long.Parse(tmp[0]);
            MD5 = tmp[1];
        }
    }
}
