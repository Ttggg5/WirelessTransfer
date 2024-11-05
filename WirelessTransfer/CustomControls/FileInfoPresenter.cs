using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WirelessTransfer.Tools.Screen;

namespace WirelessTransfer.CustomControls
{
    public static class FileInfoPresenter
    {
        static string[] imageExtensions = { ".jpg", ".png", ".bmp", ".gif", ".jpeg", ".svg" };
        static string[] videoExtensions = { ".mp4", ".mov", ".mkv", ".avi", ".mpeg", ".m4v", ".svi" };
        static string[] musicExtensions = { ".mp3", ".flv", ".m4a", ".dvf", ".m4p", ".mmf", ".movpkg", ".wav" };

        public static BitmapImage GetFileIcon(string fileExtension)
        {
            if (imageExtensions.Contains(fileExtension))
                return BitmapConverter.ByteArrayToBitmapImage(FileIconResources.image_icon);
            else if (videoExtensions.Contains(fileExtension))
                return BitmapConverter.ByteArrayToBitmapImage(FileIconResources.video_icon);
            else if (musicExtensions.Contains(fileExtension))
                return BitmapConverter.ByteArrayToBitmapImage(FileIconResources.music_icon);
            else
                return BitmapConverter.ByteArrayToBitmapImage(FileIconResources.file_icon);
        }

        public static string GetFileSizePresent(long fileSize)
        {
            if (fileSize > 1099511627776)
                return (fileSize / 1099511627776.0).ToString("#0.00") + "TB";
            else if (fileSize > 1073741824)
                return (fileSize / 1073741824.0).ToString("#0.00") + "GB";
            else if (fileSize > 1048576)
                return (fileSize / 1048576.0).ToString("#0.00") + "MB";
            else if (fileSize > 1024)
                return (fileSize / 1024.0).ToString("#0.00") + "KB";
            else
                return fileSize.ToString() + "Bytes";
        }

        public static string CalculateMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
