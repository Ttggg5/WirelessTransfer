using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;
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

        int maxShowLength = 24;

        public FileTag(string filePath, string fileName, long fileSize)
        {
            InitializeComponent();

            FilePath = filePath;
            FileName = fileName;
            FileSize = fileSize;

            if (FileName.Length > maxShowLength)
                fileNameTb.Text = FileName.Substring(0, maxShowLength) + "...";
            else
                fileNameTb.Text = FileName;

            fileSizeTb.Text = FileInfoPresenter.GetFileSizePresent(FileSize);

            string extension = Path.GetExtension(filePath);
            fileIconImg.Source = FileInfoPresenter.GetFileIcon(extension);
        }

        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            tagTranslateTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, -600, TimeSpan.FromSeconds(0.2)));
            Task.Delay(300).ContinueWith((t) =>
            {
                Dispatcher.Invoke(() => {
                    DeleteBtnClick?.Invoke(this, e);
                });
            });
        }
    }
}
