using Ini;
using System.IO;

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
        }
    }
}
