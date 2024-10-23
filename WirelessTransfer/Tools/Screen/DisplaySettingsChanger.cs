using System;
using System.Runtime.InteropServices;
using System.Windows.Media.Media3D;

namespace WirelessTransfer.Tools.Screen
{
    public static class DisplaySettingsChanger
    {
        // Define the DEVMODE structure
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;

            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;

            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;

            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        const int ENUM_CURRENT_SETTINGS = -1;
        const int CDS_UPDATEREGISTRY = 0x00000001;
        const int DISP_CHANGE_SUCCESSFUL = 0;
        const int DISP_CHANGE_FAILED = -1;

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE devMode, IntPtr hwnd, int dwFlags, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        // Function to change display settings of a specific monitor
        public static void ChangeScreenResolution(string deviceName, int width, int height, int frequency)
        {
            DEVMODE dm = new DEVMODE();
            dm.dmDeviceName = new string(new char[32]);
            dm.dmFormName = new string(new char[32]);
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            // Get current settings to modify them
            if (EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref dm))
            {
                dm.dmPelsWidth = width;
                dm.dmPelsHeight = height;
                dm.dmDisplayFrequency = frequency; // Set refresh rate
                dm.dmFields = 0x180000; // DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY

                int result = ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);

                if (result == DISP_CHANGE_SUCCESSFUL)
                {
                    Console.WriteLine($"Display settings changed for {deviceName} to {width}x{height} @ {frequency}Hz");
                }
                else
                {
                    Console.WriteLine($"Failed to change display settings for {deviceName}");
                }
            }
            else
            {
                Console.WriteLine("Unable to retrieve settings for " + deviceName);
            }
        }
    }
}