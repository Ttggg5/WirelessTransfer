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

namespace WirelessTransfer.Tools.Screen
{
    public class ScreenCaptureDX
    {
        public EventHandler<Bitmap[]>? ScreenRefreshed;

        private Bitmap[]? previousScreen;
        private bool isRunning;

        public int ScreenIndex { get; }
        public int AdapterIndex { get; }
        public int OutputIndex { get; }
        public int BitmapCount { get; }
        public Size BitmapSize { get; private set; }

        /// <param name="screenIndex"></param>
        /// <param name="splitTimes">This only split vertically</param>
        public ScreenCaptureDX(int screenIndex, int splitTimes)
        {
            ScreenRefreshed = null;
            previousScreen = null;
            isRunning = false;
            ScreenIndex = screenIndex;
            AdapterIndex = screenIndex / 2;
            OutputIndex = screenIndex % 2;
            BitmapCount = splitTimes + 1;
        }

        public void Start()
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

            BitmapSize = new Size(width, height / BitmapCount);

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

                            var boundsRect = new Rectangle(0, 0, BitmapSize.Width, BitmapSize.Height);

                            var bitmaps = new Bitmap[BitmapCount];
                            for (int i = 0; i < BitmapCount; i++)
                            {
                                bitmaps[i] = new Bitmap(BitmapSize.Width, BitmapSize.Height, PixelFormat.Format32bppArgb);

                                // Copy pixels from screen capture Texture to GDI bitmap
                                var mapDest = bitmaps[i].LockBits(boundsRect, ImageLockMode.WriteOnly, bitmaps[i].PixelFormat);
                                var destPtr = mapDest.Scan0;
                                for (int y = i * BitmapSize.Height; y < (i + 1) * BitmapSize.Height; y++)
                                {
                                    // Copy a single line 
                                    Utilities.CopyMemory(destPtr, sourcePtr, BitmapSize.Width * 4);

                                    // Advance pointers
                                    sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                                }

                                // Release source and dest locks
                                bitmaps[i].UnlockBits(mapDest);
                            }
                            device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                            ScreenRefreshed?.Invoke(this, bitmaps);

                            screenResource.Dispose();
                            duplicatedOutput.ReleaseFrame();
                            previousScreen = bitmaps;
                            GC.Collect();
                        }
                        catch (SharpDXException e)
                        {
                            if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                            {
                                Trace.TraceError(e.Message);
                                Trace.TraceError(e.StackTrace);
                            }
                        }
                    }
                }
            });
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
