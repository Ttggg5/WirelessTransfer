using Ini;
using System;
using System.Collections.Generic;
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

        private void myTcpClient_Disconnected(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    Close();
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
                        FileShareProgressTag fspt = new FileShareProgressTag(filePath, fic.FileName, fic.FileSize, fic.MD5, false);
                        fspt.Completed += FileShareProgressTag_Completed;
                        fspt.Margin = new Thickness(10);
                        progressTagSp.Children.Add(fspt);

                        leftCount++;
                        fileLeftTb.Text = "剩餘" + leftCount + "個下載";
                    });
                    break;
                case CmdType.FileData:
                    FileDataCmd fdc = (FileDataCmd)e;
                    Dispatcher.Invoke(() =>
                    {
                        foreach (FileShareProgressTag fspt in progressTagSp.Children)
                        {
                            if (fspt.FileName.Equals(fdc.FileName))
                            {
                                fspt.WriteDataToFile(fdc.FileData);
                                break;
                            }
                        }
                    });
                    break;
                case CmdType.Request:
                    RequestCmd rc = (RequestCmd)e;
                    if (rc.RequestType == RequestType.FileShare)
                        myTcpClient.SendCmd(new ReplyCmd(ReplyType.Accept));
                    break;
            }
        }

        private void FileShareProgressTag_Completed(object? sender, EventArgs e)
        {
            leftCount--;
            fileLeftTb.Text = "剩餘" + leftCount + "個下載";

            completeCount++;
            fileLeftTb.Text = "已完成" + completeCount + "個下載";
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
            myTcpClient.SendCmd(new RequestCmd(RequestType.Disconnect, Environment.MachineName));
            myTcpClient.Disconnect();
        }
    }
}
