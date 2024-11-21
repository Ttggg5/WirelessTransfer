using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Ini
{
    internal static class DefaultSetting
    {
        public static void CreateDefaultIniFile(string path)
        {
            // [Option]
            IniFile.WriteValueToIniFile(IniFileSections.Option, IniFileKeys.TcpPort, GetDefaultValue(IniFileSections.Option, IniFileKeys.TcpPort), path);
            IniFile.WriteValueToIniFile(IniFileSections.Option, IniFileKeys.UdpPort, GetDefaultValue(IniFileSections.Option, IniFileKeys.UdpPort), path);
            IniFile.WriteValueToIniFile(IniFileSections.Option, IniFileKeys.ReceivePath, GetDefaultValue(IniFileSections.Option, IniFileKeys.ReceivePath), path);
        }

        public static string GetDefaultValue(IniFileSections section, IniFileKeys key)
        {
            switch (section)
            {
                case IniFileSections.Option:
                    switch (key)
                    {
                        case IniFileKeys.TcpPort:
                            return "13215";
                        case IniFileKeys.UdpPort:
                            return "13210";
                        case IniFileKeys.ReceivePath:
                            return GetDownloadFolderPath();
                    }
                    break;
            }
            return "";
        }

        private static string GetDownloadFolderPath()
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", 
                "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();
        }
    }
}
