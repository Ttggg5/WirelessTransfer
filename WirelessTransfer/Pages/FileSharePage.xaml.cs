using Ini;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyTcp;
using WirelessTransfer.Tools.Screen;
using WirelessTransfer.Windows;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// FileSharePage.xaml 的互動邏輯
    /// </summary>
    public partial class FileSharePage : Page
    {
        const int MAX_CLIENT = 1;

        int udpPort, tcpPort;
        long totalSize = 0;
        MyTcpServer myTcpServer;
        FileSendWindow fileSendWindow;

        public FileSharePage()
        {
            InitializeComponent();
            Tag = PageFunction.FileShare;

            chooseFileBorder.Visibility = Visibility.Visible;
            maskBorder.Visibility = Visibility.Collapsed;

            confirmBtn.IsEnabled = false;

            udpPort = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.UdpPort, IniFile.DEFAULT_PATH));
            tcpPort = int.Parse(IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.TcpPort, IniFile.DEFAULT_PATH));

            deviceFinder.DeviceChoosed += deviceFinder_DeviceChoosed;
            deviceFinder.StartSearching();
        }

        private void deviceFinder_DeviceChoosed(object? sender, DeviceTag e)
        {
            deviceFinder.StopSearching();
            maskBorder.Visibility = Visibility.Visible;

            myTcpServer = new MyTcpServer(tcpPort);
            myTcpServer.Start(MAX_CLIENT);
            fileSendWindow = new FileSendWindow(myTcpServer);

            // send request
            UdpClient udpClient = new UdpClient();
            byte[] bytes = new RequestCmd(RequestType.FileShare, Environment.MachineName).Encode();
            udpClient.Send(bytes, bytes.Length, new System.Net.IPEndPoint(e.Address, udpPort));

            // waiting for accept
            Task<UdpReceiveResult> receiveTask = udpClient.ReceiveAsync();
            Task.Run(() =>
            {
                if (receiveTask.Wait(30000))
                {
                    byte[] bytes = receiveTask.Result.Buffer;
                    Cmd cmd = CmdDecoder.DecodeCmd(bytes, 0, bytes.Length);
                    if (cmd != null && cmd.CmdType == CmdType.Reply)
                    {
                        ReplyCmd replyCmd = (ReplyCmd)cmd;
                        if (replyCmd.ReplyType == ReplyType.Refuse)
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                Disconnect();
                                MessageWindow messageWindow = new MessageWindow("對方已拒絕連接!", false);
                                messageWindow.ShowDialog();
                                maskBorder.Visibility = Visibility.Collapsed;
                            });
                        }
                        else if (replyCmd.ReplyType == ReplyType.Accept)
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                foreach (FileTag ft in fileTagSp.Children)
                                {
                                    fileSendWindow.AddFile(ft.FilePath, ft.FileName, ft.FileSize);
                                }
                                fileSendWindow.ShowDialog();
                                maskBorder.Visibility = Visibility.Collapsed;
                            });
                        }
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        Disconnect();
                        MessageWindow messageWindow = new MessageWindow("請求超時!", false);
                        messageWindow.ShowDialog();
                        maskBorder.Visibility = Visibility.Collapsed;
                    });
                }
                udpClient.Close();
            });
        }

        private void addFileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            if ((bool)openFileDialog.ShowDialog())
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    bool isAdded = false;
                    foreach (FileTag ft in fileTagSp.Children)
                    {
                        if (ft.FilePath.Equals(filePath))
                        {
                            isAdded = true;
                            break;
                        }
                    }

                    if (!isAdded)
                    {
                        FileInfo fileInfo = new FileInfo(filePath);
                        FileTag fileTag = new FileTag(fileInfo.FullName, GetNonDuplicateFileName(fileInfo.Name), fileInfo.Length);
                        fileTag.DeleteBtnClick += fileTag_DeleteBtnClick;
                        fileTag.Margin = new Thickness(0, 10, 0, 10);
                        fileTagSp.Children.Add(fileTag);

                        totalSize += fileInfo.Length;

                        confirmBtn.IsEnabled = true;
                    }
                }
                RefreshTotalSize();
            }
        }

        private string GetNonDuplicateFileName(string fileName)
        {
            int count = 0;
            string result = fileName;
            string extension = Path.GetExtension(fileName);
            for (int i = 0; i < fileTagSp.Children.Count; i++)
            {
                if (((FileTag)fileTagSp.Children[i]).FileName.Equals(fileName))
                {
                    result = Path.GetFileNameWithoutExtension(fileName) + "(" + count + ")" + extension;
                    count++;
                    i = 0;
                }
            }
            return result;
        }

        private void RefreshTotalSize()
        {
            if (totalSize > 1099511627776)
                totalSizeTb.Text = (totalSize / 1099511627776.0).ToString("#0.00") + "TB";
            else if (totalSize > 1073741824)
                totalSizeTb.Text = (totalSize / 1073741824.0).ToString("#0.00") + "GB";
            else if (totalSize > 1048576)
                totalSizeTb.Text = (totalSize / 1048576.0).ToString("#0.00") + "MB";
            else if (totalSize > 1024)
                totalSizeTb.Text = (totalSize / 1024.0).ToString("#0.00") + "KB";
            else
                totalSizeTb.Text = totalSize.ToString() + "Bytes";
        }

        private void fileTag_DeleteBtnClick(object? sender, EventArgs e)
        {
            FileTag fileTag = (FileTag)sender;
            if (fileTag != null)
            {
                totalSize -= fileTag.FileSize;
                fileTagSp.Children.Remove(fileTag);
                RefreshTotalSize();
            }

            if (fileTagSp.Children.Count == 0)
                confirmBtn.IsEnabled = false;
        }

        private void confirmBtn_Click(object sender, RoutedEventArgs e)
        {
            chooseFileBorder.Visibility = Visibility.Collapsed;
        }

        public void StopAll()
        {
            deviceFinder.StopSearching();
            Disconnect();
        }

        private void changeFileBtn_Click(object sender, RoutedEventArgs e)
        {
            chooseFileBorder.Visibility = Visibility.Visible;
        }

        private void Disconnect()
        {
            if (myTcpServer != null)
            {
                lock (myTcpServer.ConnectedClients)
                {
                    if (myTcpServer.ConnectedClients.Count > 0)
                        myTcpServer.SendCmd(new RequestCmd(RequestType.Disconnect, Environment.MachineName), myTcpServer.ConnectedClients.First());
                }
                myTcpServer?.Stop();
                myTcpServer = null;
            }
        }
    }
}
