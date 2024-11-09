using Ini;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// SettingPage.xaml 的互動邏輯
    /// </summary>
    public partial class SettingPage : Page
    {
        public SettingPage()
        {
            InitializeComponent();
            Tag = PageFunction.Setting;

            filePathTBox.Text = IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.ReceivePath, IniFile.DEFAULT_PATH);
        }

        private void choosePathBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.Multiselect = false;
            openFolderDialog.DefaultDirectory = filePathTBox.Text;
            if ((bool)openFolderDialog.ShowDialog())
            {
                IniFile.WriteValueToIniFile(IniFileSections.Option, IniFileKeys.ReceivePath, openFolderDialog.FolderName, IniFile.DEFAULT_PATH);
                filePathTBox.Text = openFolderDialog.FolderName;
            }
        }
    }
}
