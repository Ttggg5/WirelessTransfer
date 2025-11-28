using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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
            FileData = Encrypt(FileData);
            Data = Encoding.UTF8.GetBytes(tmp.Length.ToString("000") + FileName).Concat(FileData).ToArray();
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            int nameLength = int.Parse(Encoding.UTF8.GetString(Data.Take(3).ToArray()));
            FileName = Encoding.UTF8.GetString(Data.Skip(3).Take(nameLength).ToArray());
            FileData = Decrypt(Data.Skip(nameLength + 3).ToArray());
        }

        public byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(FileName));
            aes.IV = MD5.HashData(Encoding.UTF8.GetBytes(FileName));

            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        public byte[] Decrypt(byte[] cipher)
        {
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(FileName));
            aes.IV = MD5.HashData(Encoding.UTF8.GetBytes(FileName));

            using var ms = new MemoryStream();
            using var cs = new CryptoStream(new MemoryStream(cipher), aes.CreateDecryptor(), CryptoStreamMode.Read);
            cs.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
