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
            FileShareProgressTag fspt = new FileShareProgressTag(filePath, fileName, fileSize, FileInfoPresenter.CalculateMD5(filePath), false);
            fspt.Margin = new Thickness(10);
            progressTagSp.Children.Add(fspt);
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

                                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                                {
                                    byte[] buffer = new byte[4194304]; // 4MB
                                    int actualLength = 0;
                                    while ((actualLength = fs.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        myTcpServer.SendCmd(new FileDataCmd(fileName, buffer.Take(actualLength).ToArray()), 
                                            myTcpServer.ConnectedClients.First());

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
                    MessageWindow messageWindow = new MessageWindow("連線已斷開!", false);
                    messageWindow.ShowDialog();
                    Close();
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
            this.Close();
        }

        private void minimizeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void titlebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            myTcpServer.SendCmd(new RequestCmd(RequestType.Disconnect, Environment.MachineName), myTcpServer.ConnectedClients.FirstOrDefault());
            myTcpServer.Stop();
        }
    }
}
