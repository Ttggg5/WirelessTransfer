﻿using System.IO;
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
            DeleteBtnClick?.Invoke(this, e);
        }
    }
}