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

        EncoderParameters eps = new EncoderParameters(1);
        EncoderParameter ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50);
        ImageCodecInfo jpegCodec;

        /// <summary>
        /// For sender.
        /// </summary>
        public ScreenCmd(Bitmap screenBmp)
        {
            ScreenBmp = screenBmp;
            CmdType = CmdType.Screen;

            eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 75L);

            jpegCodec = GetEncoderInfo(ImageFormat.Jpeg);
        }

        /// <summary>
        /// For receiver.
        /// </summary>
        public ScreenCmd(byte[] buffer)
        {
            Data = buffer;
            CmdType = CmdType.Screen;
        }

        private static ImageCodecInfo GetEncoderInfo(ImageFormat imageFormat)
        {
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
                if (codec.FormatID == imageFormat.Guid)
                    return codec;

            return null;
        }

        public override byte[] Encode()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                ScreenBmp.Save(memoryStream, jpegCodec, eps);
                Data = memoryStream.GetBuffer();
            }
            return AddHeadTail(Data);
        }

        public override void Decode()
        {
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(Data))
                {
                    ScreenBmp = (Bitmap)Bitmap.FromStream(memoryStream);
                }
            }
            catch { ScreenBmp = null; }
        }
    }
}
