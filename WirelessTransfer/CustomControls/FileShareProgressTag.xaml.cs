using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup.Localizer;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WirelessTransfer.Tools.InternetSocket.MyTcp;
using Path = System.IO.Path;

namespace WirelessTransfer.CustomControls
{
    public enum FileShareTagState
    {
        Waiting,
        Processing,
        Complete,
    }

    /// <summary>
    /// FileShareProgressTag.xaml 的互動邏輯
    /// </summary>
    public partial class FileShareProgressTag : System.Windows.Controls.UserControl
    {
        public event EventHandler Completed;

        public string FilePath { get; }
        public string FileName { get; }
        public long FileSize { get; }
        public long CurProgress { get; private set; }
        public bool IsReceiver { get; }
        public string MD5 { get; }
        public bool IsComplete { get; }
        public FileShareTagState CurState { get; private set; }

        int maxShowLength = 24;
        MyTcpServer myTcpServer;
        MyTcpClient myTcpClient;

        public FileShareProgressTag(string filePath, string fileName, long fileSize, string md5, bool isReceiver)
        {
            InitializeComponent();

            FilePath = filePath;
            FileName = fileName;
            FileSize = fileSize;
            IsReceiver = isReceiver;
            CurProgress = 0;
            if (isReceiver)
            {
                MD5 = string.Empty;
                if (File.Exists(filePath))
                    FilePath = GetNonDuplicateFilePath(filePath);
            }
            MD5 = md5;
            CurState = FileShareTagState.Waiting;
            RefreshShowedState();

            string extension = Path.GetExtension(filePath);
            fileIconImg.Source = FileInfoPresenter.GetFileIcon(extension);

            if (FileName.Length > maxShowLength)
                fileNameTb.Text = FileName.Substring(0, maxShowLength) + "...";
            else
                fileNameTb.Text = FileName;

            fileSizeTb.Text = FileInfoPresenter.GetFileSizePresent(fileSize);
        }

        private string GetNonDuplicateFilePath(string filePath)
        {
            int count = 0;
            string result = filePath;
            string extension = Path.GetExtension(filePath);
            while (true)
            {
                result = Path.GetFileNameWithoutExtension(filePath) + "(" + count + ")" + extension;
                count++;

                if (!File.Exists(result)) break;
            }

            return result;
        }

        public void WriteDataToFile(byte[] data)
        {
            CurState = FileShareTagState.Processing;
            Dispatcher.Invoke(() =>
            {
                RefreshShowedState();
            });

            using (FileStream fs = new FileStream(FilePath, FileMode.Append))
            {
                fs.Write(data, 0, data.Length);
            }
            CurProgress += data.Length;

            Dispatcher.Invoke(() =>
            {
                pb.Value = (CurProgress / FileSize) * 100;
                if (pb.Value >= 100)
                {
                    CurState = FileShareTagState.Complete;
                    RefreshShowedState();
                    Completed?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        public void AddCurProgress(long dataLength)
        {
            CurState = FileShareTagState.Processing;
            Dispatcher.Invoke(() =>
            {
                RefreshShowedState();
            });

            CurProgress += dataLength;
            Dispatcher.Invoke(() =>
            {
                pb.Value = (CurProgress / FileSize) * 100;
                if (pb.Value >= 100)
                {
                    CurState = FileShareTagState.Complete;
                    RefreshShowedState();
                    Completed?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        private void RefreshShowedState()
        {
            switch (CurState)
            {
                case FileShareTagState.Waiting:
                    waitingGrid.Visibility = Visibility.Visible;
                    processingGrid.Visibility = Visibility.Collapsed;
                    completeGrid.Visibility = Visibility.Collapsed;
                    break;
                case FileShareTagState.Processing:
                    waitingGrid.Visibility = Visibility.Collapsed;
                    processingGrid.Visibility = Visibility.Visible;
                    completeGrid.Visibility = Visibility.Collapsed;
                    break;
                case FileShareTagState.Complete:
                    waitingGrid.Visibility = Visibility.Collapsed;
                    processingGrid.Visibility = Visibility.Collapsed;
                    completeGrid.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
