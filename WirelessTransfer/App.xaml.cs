using Ini;
using System.Diagnostics;
using System.IO;
using System.Windows.Shapes;
using WirelessTransfer.Tools.InternetSocket;
using WirelessTransfer.Windows;

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
                    IniFile.WriteValueToIniFile(IniFileSections.Option, IniFileKeys.TcpPort, DefaultSetting.GetDefaultValue(IniFileSections.Option, IniFileKeys.TcpPort), IniFile.DEFAULT_PATH);

                tmp = IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, "", IniFile.DEFAULT_PATH);
                if (tmp.Equals("") || !int.TryParse(tmp, out result))
                    IniFile.WriteValueToIniFile(IniFileSections.Option, IniFileKeys.UdpPort, DefaultSetting.GetDefaultValue(IniFileSections.Option, IniFileKeys.UdpPort), IniFile.DEFAULT_PATH);

                tmp = IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.ReceivePath, "", IniFile.DEFAULT_PATH);
                if (tmp.Equals("") || !System.IO.Path.Exists(tmp))
                    IniFile.WriteValueToIniFile(IniFileSections.Option, IniFileKeys.ReceivePath, DefaultSetting.GetDefaultValue(IniFileSections.Option, IniFileKeys.ReceivePath), IniFile.DEFAULT_PATH);
            }
        }

        private void Application_Exit(object sender, System.Windows.ExitEventArgs e)
        {
            Process process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Arguments = "enableidd 0";
            process.StartInfo.FileName = ".\\deviceinstaller64.exe";
            process.StartInfo.Verb = "runas";
            process.Start();
            process.WaitForExit();
        }

        private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            /*
            if (InternetInfo.GetSSID().Equals("No wifi connected!"))
            {
                MessageWindow messageWindow = new MessageWindow("請連接至網路", false);
                messageWindow.ShowDialog();
                System.Windows.Application.Current.Shutdown();
            }
            */
        }
    }
}
