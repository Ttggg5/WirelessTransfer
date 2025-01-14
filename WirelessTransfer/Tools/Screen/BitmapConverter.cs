using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Drawing.Drawing2D;
using SharpDX.DXGI;
using Windows.Graphics.Imaging;
using SharpDX.WIC;
using SharpDX.Direct2D1;
using Bitmap = System.Drawing.Bitmap;

namespace WirelessTransfer.Tools.Screen
{
    public static class BitmapConverter
    {
        static MemoryStream bmpMS = new MemoryStream();

        public static BitmapImage BitmapToBitmapImage(Bitmap bitmap, int decodePixelWidth)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bmpMS.Position = 0;
            bitmap.Save(bmpMS, ImageFormat.Bmp);
            bmpMS.Position = 0;
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = bmpMS;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.DecodePixelWidth = decodePixelWidth;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            return BitmapToBitmapImage(bitmap, bitmap.Width);
        }

        public static BitmapImage? ByteArrayToBitmapImage(byte[] byteArray)
        {
            BitmapImage? bmp = null;
            try
            {
                bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = new MemoryStream(byteArray);
                bmp.EndInit();
            }
            catch
            {
                bmp = null;
            }
            return bmp;
        }

        // Method to convert Bitmap to WriteableBitmap and copy at specified position
        public static void DrawBitmapToWriteableBitmap(Bitmap bitmap, WriteableBitmap writeableBitmap, int x, int y)
        {
            // Lock the Bitmap bits to access its pixel data
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                // Lock the WriteableBitmap back buffer to gain direct access
                writeableBitmap.Lock();

                // Get the pointer to the WriteableBitmap's back buffer
                nint pBackBuffer = writeableBitmap.BackBuffer;
                int writeableBitmapStride = writeableBitmap.BackBufferStride;

                // Calculate the destination pointer in the WriteableBitmap's back buffer (with offset x, y)
                nint pDestBackBuffer = pBackBuffer + y * writeableBitmapStride + x * 4; // 4 bytes per pixel (Bgra32)

                // Copy the Bitmap data row by row, starting at the desired position (x, y)
                for (int i = 0; i < bitmap.Height; i++)
                {
                    nint pSourceRow = bitmapData.Scan0 + i * bitmapData.Stride;
                    nint pDestRow = pDestBackBuffer + i * writeableBitmapStride;
                    Utilities.CopyMemory(pDestRow, pSourceRow, bitmapData.Stride);
                }

                // Mark the area we modified as dirty, so WPF knows to redraw it
                writeableBitmap.AddDirtyRect(new Int32Rect(x, y, bitmap.Width, bitmap.Height));
            }
            finally
            {
                // Unlock both the WriteableBitmap and the Bitmap bits
                writeableBitmap.Unlock();
                bitmap.UnlockBits(bitmapData);
            }
        }

        public static Bitmap ResizeBitmap(System.Drawing.Image source, int width, int height, System.Drawing.Drawing2D.InterpolationMode quality)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var bmp = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = quality;
                g.DrawImage(source, new Rectangle(0, 0, width, height));
                g.Save();
            }

            return bmp;
        }
    }
}
