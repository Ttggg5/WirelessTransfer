using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public class FileDataCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = fileNameLength + fileName + FileData
        //---------------------------------------------------------------------------------
        // fileNameLength: 3 bytes to present (fill 0 in front if it not full).

        public string FileName { get; private set; }
        public byte[] FileData { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public FileDataCmd(string fileName, byte[] fileData)
        {
            FileName = fileName;
            FileData = fileData;
            CmdType = CmdType.FileData;
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public FileDataCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.FileData;
        }

        public override byte[] Encode()
        {
            byte[] tmp = Encoding.UTF8.GetBytes(FileName);
            Data = Encoding.UTF8.GetBytes(tmp.Length.ToString("000") + FileName).Concat(FileData).ToArray();
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            int nameLength = int.Parse(Encoding.UTF8.GetString(Data.Take(3).ToArray()));
            FileName = Encoding.UTF8.GetString(Data.Skip(3).Take(nameLength).ToArray());
            FileData = Data.Skip(nameLength + 3).ToArray();
        }
    }
}
