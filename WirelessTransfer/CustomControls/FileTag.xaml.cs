using System.IO;
using System.Windows;
using WirelessTransfer.Pages;
using WirelessTransfer.Tools.Screen;

namespace WirelessTransfer.CustomControls
{
    /// <summary>
    /// FileTag.xaml 的互動邏輯
    /// </summary>
    public partial class FileTag : System.Windows.Controls.UserControl
    {
        public event EventHandler DeleteBtnClick;

        public string FilePath { get; }
        public string FileName { get; }
        public long FileSize { get; }

        string[] imageExtensions = { ".jpg", ".png", ".bmp", ".gif", ".jpeg", ".svg" };
        string[] videoExtensions = { ".mp4", ".mov", ".mkv", ".avi", ".mpeg", ".m4v", ".svi" };
        string[] musicExtensions = { ".mp3", ".flv", ".m4a", ".dvf", ".m4p", ".mmf", ".movpkg", ".wav" };

        int maxShowLength = 24;

        public FileTag(string filePath, long fileByteSize)
        {
            InitializeComponent();

            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            FileSize = fileByteSize;

            if (FileName.Length > maxShowLength)
                fileNameTb.Text = FileName.Substring(0, maxShowLength) + "...";
            else
                fileNameTb.Text = FileName;

            if (FileSize > 1099511627776)
                fileSizeTb.Text = (FileSize / 1099511627776.0).ToString("#0.00") + "TB";
            else if (FileSize > 1073741824)
                fileSizeTb.Text = (FileSize / 1073741824.0).ToString("#0.00") + "GB";
            else if (FileSize > 1048576)
                fileSizeTb.Text = (FileSize / 1048576.0).ToString("#0.00") + "MB";
            else if (FileSize > 1024)
                fileSizeTb.Text = (FileSize / 1024.0).ToString("#0.00") + "KB";
            else
                fileSizeTb.Text = FileSize.ToString() + "Bytes";

            string extension = Path.GetExtension(filePath);
            if (imageExtensions.Contains(extension))
                fileIconImg.Source = BitmapConverter.ByteArrayToBitmapImage(FileIconResources.image_icon);
            else if (videoExtensions.Contains(extension))
                fileIconImg.Source = BitmapConverter.ByteArrayToBitmapImage(FileIconResources.video_icon);
            else if (musicExtensions.Contains(extension))
                fileIconImg.Source = BitmapConverter.ByteArrayToBitmapImage(FileIconResources.music_icon);
            else
                fileIconImg.Source = BitmapConverter.ByteArrayToBitmapImage(FileIconResources.file_icon);
        }

        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            DeleteBtnClick?.Invoke(this, e);
        }
    }
}
