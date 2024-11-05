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

namespace WirelessTransfer.CustomControls
{
    /// <summary>
    /// FileShareProgressTag.xaml 的互動邏輯
    /// </summary>
    public partial class FileShareProgressTag : System.Windows.Controls.UserControl
    {
        public string FilePath { get; }
        public string FileName { get; }
        public long FileSize { get; }
        public bool IsReceiver { get; }

        int maxShowLength = 24;

        public FileShareProgressTag(string filePath, string fileName, long fileSize, bool isReceiver)
        {
            InitializeComponent();

            FilePath = filePath;
            FileName = fileName;
            FileSize = fileSize;
            IsReceiver = isReceiver;

            string extension = System.IO.Path.GetExtension(filePath);
            fileIconImg.Source = FileInfoPresenter.GetFileIcon(extension);

            if (FileName.Length > maxShowLength)
                fileNameTb.Text = FileName.Substring(0, maxShowLength) + "...";
            else
                fileNameTb.Text = FileName;

            fileSizeTb.Text = FileInfoPresenter.GetFileSizePresent(fileSize);
        }
    }
}
