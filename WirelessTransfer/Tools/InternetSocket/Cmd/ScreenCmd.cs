using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WirelessTransfer.Tools.InternetSocket.Cmd
{
    public class ScreenCmd : Cmd
    {
        // Correct message format:
        //---------------------------------------------------------------------------------
        // data = screenBmp
        //---------------------------------------------------------------------------------
        // screenBmp: bitmap in byte[].

        public Bitmap ScreenBmp { get; private set; }

        /// <summary>
        /// For sender.
        /// </summary>
        public ScreenCmd(Bitmap screenBmp)
        {
            ScreenBmp = screenBmp;
            CmdType = CmdType.Screen;
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public ScreenCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.Screen;
        }

        public override byte[] Encode()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                ScreenBmp.Save(memoryStream, ImageFormat.Jpeg);
                Data = memoryStream.GetBuffer();
            }
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            using (MemoryStream memoryStream = new MemoryStream(Data))
            {
                ScreenBmp = (Bitmap)Bitmap.FromStream(memoryStream);
            }
        }
    }
}
