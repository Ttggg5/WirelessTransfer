using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ini
{
    public static class IniFile
    {
        public static event EventHandler SettingChanged;

        public const string DEFAULT_PATH = @".\Config.ini";

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public static void WriteValueFromIniFile(IniFileSections section, IniFileKeys key, string value, string filePath)
        {
            WriteValueFromIniFile(Enum.GetName(typeof(IniFileSections), section),
                                  Enum.GetName(typeof(IniFileKeys), key), value, filePath);
        }

        public static void WriteValueFromIniFile(string section, string key, string value, string filePath)
        {
            WritePrivateProfileString(section, key, value, filePath);
            SettingChanged?.Invoke(new object(), new EventArgs());
        }

        public static string ReadValueFromIniFile(IniFileSections section, IniFileKeys key, string filePath)
        {
            return ReadValueFromIniFile(Enum.GetName(typeof(IniFileSections), section),
                                        Enum.GetName(typeof(IniFileKeys), key), 
                                        DefaultSetting.GetDefaultValue(section, key), filePath);
        }

        public static string ReadValueFromIniFile(IniFileSections section, IniFileKeys key, string def, string filePath)
        {
            return ReadValueFromIniFile(Enum.GetName(typeof(IniFileSections), section),
                                        Enum.GetName(typeof(IniFileKeys), key), def, filePath);
        }

        public static string ReadValueFromIniFile(string section, string key, string def, string filePath)
        {
            StringBuilder temp = new StringBuilder(4096);
            int i = GetPrivateProfileString(section, key, def, temp, 4096, filePath);
            return temp.ToString();
        }
    }
}
