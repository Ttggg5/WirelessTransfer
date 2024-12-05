using Ini;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
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
using static System.Net.Mime.MediaTypeNames;

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
            qualityTb.Text = IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.ScreenQuality, IniFile.DEFAULT_PATH);

            qualityTb.TextChanged += qualityTb_TextChanged;
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

        private static readonly Regex regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private void qualityTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = regex.IsMatch(e.Text);
        }

        private void qualityTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(qualityTb.Text, out int tmp))
            {
                if (tmp > 100)
                    qualityTb.Text = "100";
            }
            else
                qualityTb.Text = "0";

            IniFile.WriteValueToIniFile(IniFileSections.Option, IniFileKeys.ScreenQuality, qualityTb.Text, IniFile.DEFAULT_PATH);
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            StackPanel stackPanel = sender as StackPanel;
            stackPanel.Focus();
        }
    }
}
