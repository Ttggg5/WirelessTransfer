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
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyTcp;

namespace WirelessTransfer.Windows
{
    /// <summary>
    /// FileShareWindow.xaml 的互動邏輯
    /// </summary>
    public partial class FileReceiveWindow : Window
    {
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
                    break;
                case CmdType.FileData:
                    break;
            }
        }

        private void titlebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
