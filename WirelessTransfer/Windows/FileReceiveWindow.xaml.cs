using Ini;
using Microsoft.Toolkit.Uwp.Notifications;
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
using System.Windows.Shapes;
using WirelessTransfer.CustomControls;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyTcp;

namespace WirelessTransfer.Windows
{
    /// <summary>
    /// FileShareWindow.xaml 的互動邏輯
    /// </summary>
    public partial class FileReceiveWindow : Window
    {
        int leftCount = 0;
        int completeCount = 0;
        MyTcpClient myTcpClient;

        public FileReceiveWindow(MyTcpClient myTcpClient)
        {
            InitializeComponent();

            this.myTcpClient = myTcpClient;
            myTcpClient.ReceivedCmd += myTcpClient_ReceivedCmd;
            myTcpClient.Connected += myTcpClient_Connected;
            myTcpClient.Disconnected += myTcpClient_Disconnected;
            myTcpClient.Connect();
        }

        private void DeleteUnfinishedFile()
        {
            foreach (FileShareProgressTag fspt in progressTagSp.Children)
            {
                if (fspt.CurState == FileShareTagState.Processing)
                {
                    if (File.Exists(fspt.FilePath))
                        File.Delete(fspt.FilePath);
                }
            }
        }

        private void myTcpClient_Disconnected(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (leftCount > 0)
                    {
                        DeleteUnfinishedFile();
                        new ToastContentBuilder()
                            .AddArgument("conversationId", 1)
                            .AddText("連線中斷")
                            .AddText(leftCount + "個檔案傳輸已被取消")
                            .Show();
                        /*
                        MessageWindow messageWindow = new MessageWindow("連線中斷", false);
                        messageWindow.ShowDialog();
                        */
                        Close();
                    }
                    else
                    {
                        new ToastContentBuilder()
                            .AddArgument("conversationId", 1)
                            .AddText("全部檔案已下載完成")
                            .Show();
                        /*
                        MessageWindow messageWindow = new MessageWindow("全部檔案已下載完成", false);
                        messageWindow.ShowDialog();
                        */
                    }
                }
                catch { }
            });
        }

        private void myTcpClient_Connected(object? sender, EventArgs e)
        {
            
        }

        private void myTcpClient_ReceivedCmd(object? sender, Cmd e)
        {
            switch (e.CmdType)
            {
                case CmdType.FileInfo:
                    FileInfoCmd fic = (FileInfoCmd)e;
                    string filePath = IniFile.ReadValueFromIniFile(IniFileSections.Option, IniFileKeys.ReceivePath, IniFile.DEFAULT_PATH) 
                        + "\\" + fic.FileName;
                    Dispatcher.Invoke(() =>
                    {
                        FileShareProgressTag fspt = new FileShareProgressTag(filePath, fic.FileName, fic.FileSize, fic.MD5, true);
                        fspt.Completed += FileShareProgressTag_Completed;
                        fspt.Margin = new Thickness(10);
                        progressTagSp.Children.Add(fspt);

                        leftCount++;
                        fileLeftTb.Text = "剩餘" + leftCount + "個下載";
                    });
                    break;
                case CmdType.FileData:
                    FileDataCmd fdc = (FileDataCmd)e;
                    FileShareProgressTag tmp = null;
                    Dispatcher.Invoke(() =>
                    {
                        foreach (FileShareProgressTag fspt in progressTagSp.Children)
                        {
                            if (fspt.FileName.Equals(fdc.FileName))
                            {
                                tmp = fspt;
                                break;
                            }
                        }
                    });
                    myTcpClient.SendCmd(new RequestCmd(RequestType.FileShare, Environment.MachineName));
                    tmp.WriteDataToFile(fdc.FileData);
                    break;
                case CmdType.Request:
                    RequestCmd rc = (RequestCmd)e;
                    if (rc.RequestType == RequestType.FileShare)
                        myTcpClient.SendCmd(new ReplyCmd(ReplyType.Accept));
                    else if (rc.RequestType == RequestType.Disconnect)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (leftCount > 0)
                            {
                                DeleteUnfinishedFile();
                                new ToastContentBuilder()
                                    .AddArgument("conversationId", 1)
                                    .AddText("連線中斷")
                                    .AddText(leftCount + "個檔案傳輸已被取消")
                                    .Show();
                                Close();
                            }
                        });
                    }
                    break;
            }
        }

        private void FileShareProgressTag_Completed(object? sender, EventArgs e)
        {
            leftCount--;
            fileLeftTb.Text = "剩餘" + leftCount + "個下載";

            completeCount++;
            fileCompleteTb.Text = "已完成" + completeCount + "個下載";

            if (leftCount == 0) myTcpClient.Disconnect();
        }

        private void closeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            if (leftCount > 0)
            {
                MessageWindow messageWindow = new MessageWindow("傳輸尚未完成，確定要取消嗎?", true);
                if ((bool)messageWindow.ShowDialog())
                {
                    DeleteUnfinishedFile();
                    myTcpClient.SendCmd(new RequestCmd(RequestType.Disconnect, Environment.MachineName));
                    Close();
                }
            }
            else Close();
        }

        private void minimizeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void titlebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            myTcpClient.SendCmd(new RequestCmd(RequestType.Disconnect, Environment.MachineName));
            myTcpClient.Disconnect();
        }
    }
}
