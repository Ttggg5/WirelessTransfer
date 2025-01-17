﻿using Microsoft.Toolkit.Uwp.Notifications;
using SharpDX.Direct3D11;
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
using System.Windows.Shapes;
using WirelessTransfer.CustomControls;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyTcp;

namespace WirelessTransfer.Windows
{
    /// <summary>
    /// FileSendWindow.xaml 的互動邏輯
    /// </summary>
    public partial class FileSendWindow : Window
    {
        int leftCount = 0;
        int completeCount = 0;
        MyTcpServer myTcpServer;

        public FileSendWindow(MyTcpServer myTcpServer)
        {
            InitializeComponent();

            this.myTcpServer = myTcpServer;
            myTcpServer.ClientConnected += myTcpServer_ClientConnected;
            myTcpServer.ClientDisconnected += myTcpServer_ClientDisconnected;
            myTcpServer.ReceivedCmd += myTcpServer_ReceivedCmd;
        }

        public void AddFile(string filePath, string fileName, long fileSize)
        {
            FileShareProgressTag fspt = new FileShareProgressTag(filePath, fileName, fileSize, "", false);
            fspt.Completed += FileShareProgressTag_Completed;
            fspt.Margin = new Thickness(10);
            progressTagSp.Children.Add(fspt);

            leftCount++;
            fileLeftTb.Text = "剩餘" + leftCount + "個檔案";
        }

        private void FileShareProgressTag_Completed(object? sender, EventArgs e)
        {
            leftCount--;
            fileLeftTb.Text = "剩餘" + leftCount + "個檔案";

            if (leftCount == 0)
            {
                new ToastContentBuilder()
                    .AddArgument("conversationId", 2)
                    .AddText("全部檔案已傳輸完成")
                    .Show();
            }
        }

        private void myTcpServer_ReceivedCmd(object? sender, Cmd e)
        {
            switch (e.CmdType)
            {
                case CmdType.Reply:
                    ReplyCmd rc = (ReplyCmd)e;
                    if (rc.ReplyType == ReplyType.Accept)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            bool isAllComplete = false;
                            while (true)
                            {
                                if (myTcpServer.ConnectedClients.Count == 0) break;

                                int curIndex = 0;
                                string filePath = "";
                                string fileName = "";
                                Dispatcher.Invoke(() =>
                                {
                                    isAllComplete = true;
                                    for (int i = 0; i < progressTagSp.Children.Count; i++)
                                    {
                                        FileShareProgressTag fspt = (FileShareProgressTag)progressTagSp.Children[i];
                                        if (fspt.CurState != FileShareTagState.Complete)
                                        {
                                            isAllComplete = false;
                                            curIndex = i;
                                            filePath = fspt.FilePath;
                                            fileName = fspt.FileName;
                                            break;
                                        }
                                    }
                                });

                                if (isAllComplete) break;

                                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                                {
                                    byte[] buffer = new byte[4194304]; // 4MB
                                    int actualLength = 0;
                                    while ((actualLength = fs.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        try
                                        {
                                            myTcpServer.SendCmd(new FileDataCmd(fileName, buffer.Take(actualLength).ToArray()), 
                                                myTcpServer.ConnectedClients.First());
                                        }
                                        catch
                                        {
                                            break;
                                        }

                                        Dispatcher.Invoke(() =>
                                        {
                                            ((FileShareProgressTag)progressTagSp.Children[curIndex]).AddCurProgress(actualLength);
                                        });
                                    }
                                }
                            }
                        }, TaskCreationOptions.LongRunning);
                    }
                    break;
            }
        }

        private void myTcpServer_ClientDisconnected(object? sender, MyTcpClientInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (leftCount > 0)
                    {
                        new ToastContentBuilder()
                            .AddArgument("conversationId", 2)
                            .AddText("連線中斷")
                            .AddText("檔案傳輸已被取消")
                            .Show();
                        /*
                        MessageWindow messageWindow = new MessageWindow("連線已斷開!", false);
                        messageWindow.ShowDialog();
                        */
                        Close();
                    }
                }
                catch { }
            });
        }

        private void myTcpServer_ClientConnected(object? sender, MyTcpClientInfo e)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (FileShareProgressTag fspt in progressTagSp.Children)
                {
                    myTcpServer.SendCmd(new FileInfoCmd(fspt.FileName, fspt.FileSize, fspt.MD5), e);
                }
                myTcpServer.SendCmd(new RequestCmd(RequestType.FileShare, Environment.MachineName), e);
            });
        }

        private void closeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            if (leftCount > 0)
            {
                MessageWindow messageWindow = new MessageWindow("傳輸尚未完成，確定要取消嗎?", true);
                if ((bool)messageWindow.ShowDialog()) Close();
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
            if (myTcpServer.ConnectedClients.FirstOrDefault() != null)
                myTcpServer.SendCmd(new RequestCmd(RequestType.Disconnect, Environment.MachineName), myTcpServer.ConnectedClients.First());
            myTcpServer.Stop();
        }
    }
}
