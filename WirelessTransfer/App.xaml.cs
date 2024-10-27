﻿using Ini;
using System.IO;
using System.Windows.Shapes;

namespace WirelessTransfer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public App()
        {
            if (!File.Exists(IniFile.DEFAULT_PATH))
                DefaultSetting.CreateDefaultIniFile(IniFile.DEFAULT_PATH);
            else
            {
                string tmp = IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.TcpPort, "", IniFile.DEFAULT_PATH);
                if (tmp.Equals("") || !int.TryParse(tmp, out int result))
                    IniFile.WriteValueFromIniFile(IniFileSections.Option, IniFileKeys.TcpPort, DefaultSetting.GetDefaultValue(IniFileSections.Option, IniFileKeys.TcpPort), IniFile.DEFAULT_PATH);

                tmp = IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, "", IniFile.DEFAULT_PATH);
                if (tmp.Equals("") || !int.TryParse(tmp, out result))
                    IniFile.WriteValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, DefaultSetting.GetDefaultValue(IniFileSections.Option, IniFileKeys.UdpPort), IniFile.DEFAULT_PATH);

                tmp = IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.ReceivePath, "", IniFile.DEFAULT_PATH);
                if (tmp.Equals("") || !System.IO.Path.Exists(tmp))
                    IniFile.WriteValueFromIniFile(IniFileSections.Option, IniFileKeys.ReceivePath, DefaultSetting.GetDefaultValue(IniFileSections.Option, IniFileKeys.ReceivePath), IniFile.DEFAULT_PATH);
            }
        }
    }
}
