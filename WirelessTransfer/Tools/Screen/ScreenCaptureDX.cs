using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;

namespace WirelessTransfer.Tools.Screen
{
    public class ScreenCaptureDX
    {
        private bool isRunning;

        public int AdapterIndex { get; }
        public int OutputIndex { get; }

        public ScreenCaptureDX(int adapterIndex, int outputIndex)
        {
            Init();
            AdapterIndex = adapterIndex;
            OutputIndex = outputIndex;
        }

        public ScreenCaptureDX(string deviceName)
        {
            Init();
            FindScreen(deviceName, out int adapterIndex, out int outputIndex);
            if (adapterIndex == -1 || outputIndex == -1)
            {
                adapterIndex = 0;
                outputIndex = 0;
            }

            AdapterIndex = adapterIndex;
            OutputIndex = outputIndex;
        }

        private void Init()
        {
            isRunning = false;
        }

        /// <summary>
        /// adapterIndex = -1 and outputIndex = -1, if not found.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="adapterIndex"></param>
        /// <param name="outputIndex"></param>
        public static void FindScreen(string deviceName, out int adapterIndex, out int outputIndex)
        {
            var factory = new Factory1();
            for (int i = 0; i < factory.Adapters1.Length; i++)
            {
                for (int j = 0; j < factory.Adapters1[i].Outputs.Length; j++)
                {
                    if (factory.Adapters1[i].Outputs[j].Description.DeviceName.Equals(deviceName))
                    {
                        adapterIndex = i;
                        outputIndex = j;
                        return;
                    }
                }
            }
            adapterIndex = -1;
            outputIndex = -1;
        }

        public void Start(Action<Bitmap> onScreenRefreshed)
        {
            isRunning = true;
            var factory = new Factory1();
            //Get first adapter (one adapter can only handle two screen)
            var adapter = factory.GetAdapter1(AdapterIndex);
            //Get device from adapter
            var device = new SharpDX.Direct3D11.Device(adapter);
            //Get front buffer of the adapter
            var output = adapter.GetOutput(OutputIndex);
            var output1 = output.QueryInterface<Output1>();

            // Width/Height of desktop to capture
            int width = output.Description.DesktopBounds.Right - output.Description.DesktopBounds.Left;
            int height = output.Description.DesktopBounds.Bottom - output.Description.DesktopBounds.Top;

            // Create Staging texture CPU-accessible
            var textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };
            var screenTexture = new Texture2D(device, textureDesc);

            Task.Factory.StartNew(() =>
            {
                // Duplicate the output
                using (var duplicatedOutput = output1.DuplicateOutput(device))
                {
                    while (isRunning)
                    {
                        try
                        {
                            SharpDX.DXGI.Resource screenResource;
                            OutputDuplicateFrameInformation duplicateFrameInformation;

                            // Try to get duplicated frame within given time is ms
                            duplicatedOutput.TryAcquireNextFrame(500, out duplicateFrameInformation, out screenResource);
                            if (screenResource == null)
                                continue;

                            // copy resource into memory that can be accessed by the CPU
                            using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                            device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

                            // Get the desktop capture texture
                            var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                            var sourcePtr = mapSource.DataPointer;

                            var boundsRect = new Rectangle(0, 0, width, height);

                            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                            // Copy pixels from screen capture Texture to GDI bitmap
                            var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                            var destPtr = mapDest.Scan0;
                            for (int y = 0; y < height; y++)
                            {
                                // Copy a single line 
                                Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                                // Advance pointers
                                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                            }

                            // Release source and dest locks
                            bitmap.UnlockBits(mapDest);
                            device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                            // Draw cursor
                            DrawCursorOnBitmap(bitmap, output.Description.DesktopBounds.Left, output.Description.DesktopBounds.Top);

                            onScreenRefreshed.Invoke(bitmap);

                            screenResource.Dispose();
                            duplicatedOutput.ReleaseFrame();
                            GC.Collect();
                        }
                        catch (SharpDXException e)
                        {
                            /*
                            if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                            {
                                Trace.TraceError(e.Message);
                                Trace.TraceError(e.StackTrace);
                            }
                            */
                        }
                        catch { }
                    }
                }
            });
        }

        // WinAPI interop for cursor info
        [StructLayout(LayoutKind.Sequential)]
        public struct CursorInfo
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public Point ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CursorInfo pci);

        [DllImport("user32.dll")]
        public static extern IntPtr CopyIcon(IntPtr hCursor);

        [DllImport("user32.dll")]
        public static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

        [DllImport("user32.dll")]
        public static extern bool DrawIcon(IntPtr hdc, int x, int y, IntPtr hIcon);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private const int CURSOR_SHOWING = 0x00000001;

        public static void DrawCursorOnBitmap(Bitmap bitmap, int offsetX, int offsetY)
        {
            int x = Cursor.Position.X - offsetX;
            int y = Cursor.Position.Y - offsetY;

            var cursorInfo = new CursorInfo();
            cursorInfo.cbSize = Marshal.SizeOf(typeof(CursorInfo));

            if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    nint hdc = g.GetHdc();

                    var iconInfo = new IconInfo();
                    GetIconInfo(cursorInfo.hCursor, ref iconInfo);

                    x -= iconInfo.xHotspot;
                    y -= iconInfo.yHotspot;
                    DrawIcon(hdc, x, y, cursorInfo.hCursor);

                    DeleteObject(cursorInfo.hCursor);
                    DeleteObject(hdc);
                    DeleteObject(iconInfo.hbmMask);
                    DeleteObject(iconInfo.hbmColor);
                }
            }
        }

        public static unsafe bool CompareBitmapsFast(Bitmap bmp1, Bitmap bmp2)
        {
            // Check if the dimensions of the bitmaps are the same
            if (bmp1.Width != bmp2.Width || bmp1.Height != bmp2.Height)
                return false;

            // Lock the bits of both bitmaps
            BitmapData bmpData1 = bmp1.LockBits(new Rectangle(0, 0, bmp1.Width, bmp1.Height), ImageLockMode.ReadOnly, bmp1.PixelFormat);
            BitmapData bmpData2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, bmp2.PixelFormat);

            try
            {
                // Check if the pixel formats are the same
                if (bmpData1.Stride != bmpData2.Stride)
                    return false;

                int height = bmp1.Height;
                int widthInBytes = bmpData1.Stride;

                byte* ptr1 = (byte*)bmpData1.Scan0;
                byte* ptr2 = (byte*)bmpData2.Scan0;

                // Compare pixel by pixel
                for (int y = 0; y < height; y++)
                {
                    byte* row1 = ptr1 + (y * bmpData1.Stride);
                    byte* row2 = ptr2 + (y * bmpData2.Stride);

                    for (int x = 0; x < widthInBytes; x++)
                    {
                        if (row1[x] != row2[x])
                        {
                            return false; // Found a difference
                        }
                    }
                }
            }
            finally
            {
                // Unlock the bits
                bmp1.UnlockBits(bmpData1);
                bmp2.UnlockBits(bmpData2);
            }

            return true; // Bitmaps are identical
        }

        public void Stop()
        {
            isRunning = false;
        }
    }
}
