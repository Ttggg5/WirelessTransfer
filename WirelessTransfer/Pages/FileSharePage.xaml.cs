using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using WirelessTransfer.CustomControls;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// FileSharePage.xaml 的互動邏輯
    /// </summary>
    public partial class FileSharePage : Page
    {
        public FileSharePage()
        {
            InitializeComponent();
            Tag = PageFunction.FileShare;
        }

        private void addFileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            if ((bool)openFileDialog.ShowDialog())
            {
                bool isAdded = false;
                foreach (string fileName in openFileDialog.FileNames)
                {
                    foreach (FileTag ft in fileTagSp.Children)
                    {
                        if (ft.FilePath.Equals(fileName))
                        {
                            isAdded = true;
                            break;
                        }
                    }

                    if (!isAdded)
                    {
                        FileInfo fileInfo = new FileInfo(fileName);
                        FileTag fileTag = new FileTag(fileInfo.FullName, fileInfo.Length);
                        fileTag.DeleteBtnClick += fileTag_DeleteBtnClick;
                        fileTag.Margin = new Thickness(0, 10, 0, 10);
                        fileTagSp.Children.Add(fileTag);
                    }
                }
            }
        }

        private void fileTag_DeleteBtnClick(object? sender, EventArgs e)
        {
            FileTag fileTag = (FileTag)sender;
            if (fileTag != null)
                fileTagSp.Children.Remove(fileTag);
        }
    }
}
