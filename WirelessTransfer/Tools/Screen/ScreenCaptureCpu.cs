using System.Drawing.Imaging;
using System.Drawing;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Size = System.Drawing.Size;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using SharpDX;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Reflection;

namespace WirelessTransfer.Tools.Screen
{
    public enum SplitType
    {
        NoSplit,
        /// <summary>
        /// Row = 1, Col = 2
        /// </summary>
        _1x2,
        /// <summary>
        /// Row = 2, Col = 2
        /// </summary>
        _2x2,
        /// <summary>
        /// Row = 2, Col = 3
        /// </summary>
        _2x3,
        /// <summary>
        /// Row = 3, Col = 3
        /// </summary>
        _3x3,
        /// <summary>
        /// Row = 3, Col = 4
        /// </summary>
        _3x4,
        /// <summary>
        /// Row = 4, Col = 4
        /// </summary>
        _4x4,
    }

    public class ScreenCaptureCpu
    {
        public EventHandler<Bitmap[]> ScreenRefreshed;

        Bitmap[] frame;
        Task[] tasks;
        bool startCmd, isCapturing;

        Rectangle captureRectangle;
        public int[] shiftDistanceX;
        public int[] shiftDistanceY;
        bool drawCursor;

        public int ScreenIndex { get; }
        public bool IsCapturing { get { return isCapturing; } }

        public ScreenCaptureCpu(int screenIndex, bool drawCursor, SplitType splitType = SplitType.NoSplit)
        {
            ScreenIndex = screenIndex;
            captureRectangle = System.Windows.Forms.Screen.AllScreens[ScreenIndex].Bounds;
            this.drawCursor = drawCursor;

            startCmd = false;
            isCapturing = false;

            int width = captureRectangle.Width;
            int height = captureRectangle.Height;
            int count = 1;
            shiftDistanceX = [0];
            shiftDistanceY = [0];
            switch (splitType)
            {
                case SplitType._1x2:
                    width = captureRectangle.Width / 2;
                    height = captureRectangle.Height;
                    count = 2;
                    shiftDistanceX = [0, width];
                    shiftDistanceY = [0, 0];
                    break;

                case SplitType._2x2:
                    width = captureRectangle.Width / 2;
                    height = captureRectangle.Height / 2;
                    count = 4;
                    shiftDistanceX = [
                        0, width, 
                        0, width,
                    ];
                    shiftDistanceY = [
                        0, 0, 
                        height, height,
                    ];
                    break;

                case SplitType._2x3:
                    width = captureRectangle.Width / 3;
                    height = captureRectangle.Height / 2;
                    count = 6;
                    shiftDistanceX = [
                        0, width, width * 2, 
                        0, width, width * 2,
                    ];
                    shiftDistanceY = [
                        0, 0, 0, 
                        height, height, height,
                    ];
                    break;

                case SplitType._3x3:
                    width = captureRectangle.Width / 3;
                    height = captureRectangle.Height / 3;
                    count = 9;
                    shiftDistanceX = [
                        0, width, width * 2, 
                        0, width, width * 2, 
                        0, width, width * 2,
                    ];
                    shiftDistanceY = [
                        0, 0, 0, 
                        height, height, height, 
                        height * 2, height * 2, height * 2,
                    ];
                    break;

                case SplitType._3x4:
                    width = captureRectangle.Width / 4;
                    height = captureRectangle.Height / 3;
                    count = 12;
                    shiftDistanceX = [
                        0, width, width * 2, width * 3, 
                        0, width, width * 2, width * 3, 
                        0, width, width * 2, width * 3,
                    ];
                    shiftDistanceY = [
                        0, 0, 0, 0, 
                        height, height, height, height,
                        height * 2, height * 2, height * 2, height * 2,
                    ];
                    break;

                case SplitType._4x4:
                    width = captureRectangle.Width / 4;
                    height = captureRectangle.Height / 4;
                    count = 16;
                    shiftDistanceX = [
                        0, width, width * 2, width * 3,
                        0, width, width * 2, width * 3,
                        0, width, width * 2, width * 3,
                        0, width, width * 2, width * 3,
                    ];
                    shiftDistanceY = [
                        0, 0, 0, 0,
                        height, height, height, height,
                        height * 2, height * 2, height * 2, height * 2,
                        height * 3, height * 3, height *3 , height * 3,
                    ];
                    break;
            }

            tasks = new Task[count];
            frame = new Bitmap[count];
            for (int i = 0; i < count; i++) frame[i] = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        public void StartCapture()
        {
            startCmd = true;
            Task.Factory.StartNew(() =>
            {
                isCapturing = true;

                while (startCmd)
                {
                    CaptureScreenSeparately();
                    ScreenRefreshed?.Invoke(this, frame);
                }

                isCapturing = false;
            });
        }

        public void StopCapture()
        {
            startCmd = false;
            //while (IsCapturing) Task.Delay(50).Wait();
        }

        private void CaptureScreenSeparately()
        {
            try
            {
                Parallel.For(0, frame.Length, i => Capture(i));
            }
            catch (Exception ex)
            {
                //System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void Capture(int index)
        {
            Graphics captureGraphics = Graphics.FromImage(frame[index]);
            captureGraphics.CopyFromScreen(
                captureRectangle.Left + shiftDistanceX[index],
                captureRectangle.Top + shiftDistanceY[index],
                0, 0, frame[index].Size);
            if (drawCursor)
                DrawCursor(
                    frame[index], ScreenIndex,
                    captureRectangle.Left + shiftDistanceX[index],
                    captureRectangle.Top + shiftDistanceY[index]);
        }

        public static Bitmap? CaptureScreen(int screenIndex, bool drawCursor, int x, int y, int width, int height)
        {
            try
            {
                Rectangle captureRectangle = System.Windows.Forms.Screen.AllScreens[screenIndex].Bounds;
                Bitmap captureBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                Graphics captureGraphics = Graphics.FromImage(captureBitmap);
                captureGraphics.CopyFromScreen(captureRectangle.Left + x, captureRectangle.Top + y, 0, 0, captureBitmap.Size);
                if (drawCursor) DrawCursor(captureBitmap, screenIndex, captureRectangle.Left + x, captureRectangle.Top + y);
                return captureBitmap;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
            return null;
        }

        public static Bitmap? CaptureFullScreen(int screenIndex, bool drawCursor)
        {
            try
            {
                Rectangle captureRectangle = System.Windows.Forms.Screen.AllScreens[screenIndex].Bounds;
                Bitmap captureBitmap = new Bitmap(captureRectangle.Width, captureRectangle.Height, PixelFormat.Format32bppArgb);
                Graphics captureGraphics = Graphics.FromImage(captureBitmap);
                captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
                if (drawCursor) DrawCursor(captureBitmap, screenIndex, captureRectangle.Left, captureRectangle.Top);
                return captureBitmap;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
            return null;
        }

        private static void DrawCursor(Bitmap bitmap, int screenIndex, int x, int y)
        {
            System.Drawing.Point position = Cursor.Position;
            if (position.X > x && position.X < x + System.Windows.Forms.Screen.AllScreens[screenIndex].Bounds.Width)
            {
                position.X -= x;
                position.Y -= y;

                // Draw the cursor on the bitmap using Graphics
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    var cursor = GetCurrentCursorType();
                    // Draw the cursor at the correct location
                    cursor?.Draw(g, new Rectangle(position.X, position.Y, cursor.Size.Width, cursor.Size.Height));
                }
            }
        }

        // Cursor types handle value
        const nint IDC_ARROW = 0x0000000000010003;
        const nint IDC_IBEAM = 0x0000000000010005;
        const nint IDC_WAIT = 0x0000000000010007;
        const nint IDC_CROSS = 0x0000000000010009;
        const nint IDC_UPARROW = 0x000000000001000b;
        const nint IDC_SIZENWSE = 0x000000000001000d;
        const nint IDC_SIZENESW = 0x000000000001000f;
        const nint IDC_SIZEWE = 0x0000000000010011;
        const nint IDC_SIZENS = 0x0000000000010013;
        const nint IDC_SIZEALL = 0x0000000000010015;
        const nint IDC_NO = 0x0000000000010017;
        const nint IDC_HAND = 0x000000000001001f;
        const nint IDC_APPSTARTING = 0x0000000000010019;
        const nint IDC_HELP = 0x000000000001001b;

        private static Cursor GetCurrentCursorType()
        {
            CURSORINFO pci;
            pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
            if (GetCursorInfo(out pci))
            {
                switch (pci.hCursor)
                {
                    case IDC_ARROW:
                        return Cursors.Arrow;
                    case IDC_IBEAM:
                        return Cursors.IBeam;
                    case IDC_WAIT:
                        return Cursors.WaitCursor;
                    case IDC_CROSS:
                        return Cursors.Cross;
                    case IDC_UPARROW:
                        return Cursors.UpArrow;
                    case IDC_SIZENWSE:
                        return Cursors.SizeNWSE;
                    case IDC_SIZENESW:
                        return Cursors.SizeNESW;
                    case IDC_SIZEWE:
                        return Cursors.SizeWE;
                    case IDC_SIZENS:
                        return Cursors.SizeNS;
                    case IDC_SIZEALL:
                        return Cursors.SizeAll;
                    case IDC_NO:
                        return Cursors.No;
                    case IDC_HAND:
                        return Cursors.Hand;
                    case IDC_APPSTARTING:
                        return Cursors.AppStarting;
                    case IDC_HELP:
                        return Cursors.Help;
                }
            }
            return Cursors.Default;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public Int32 x;
            public Int32 y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public Int32 cbSize;        // Specifies the size, in bytes, of the structure. 
                                        // The caller must set this to Marshal.SizeOf(typeof(CURSORINFO)).
            public Int32 flags;         // Specifies the cursor state. This parameter can be one of the following values:
                                        // 0             The cursor is hidden.
                                        // CURSOR_SHOWING    The cursor is showing.
            public IntPtr hCursor;      // Handle to the cursor. 
            public POINT ptScreenPos;   // A POINT structure that receives the screen coordinates of the cursor. 
        }

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);
    }
}