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
            IniFile.WriteValueFromIniFile(IniFileSections.Option, IniFileKeys.Port, "8888", path);

            IniFile.WriteValueFromIniFile(IniFileSections.Option, IniFileKeys.ReceivePath, GetDownloadFolderPath(), path);
        }

        private static string GetDownloadFolderPath()
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", 
                "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();
        }
    }
}
