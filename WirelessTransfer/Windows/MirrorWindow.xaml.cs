using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyTcp;
using WirelessTransfer.Tools.Screen;

namespace WirelessTransfer.Windows
{
    /// <summary>
    /// MirrorWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MirrorWindow : Window
    {
        MyTcpClient myTcpClient;
        WriteableBitmap screenWB;

        public MirrorWindow(MyTcpClient myTcpClient)
        {
            InitializeComponent();

            screenWB = new WriteableBitmap(1920, 1080, 96, 96, PixelFormats.Bgr32, null); // jpg format
            screenImg.Source = screenWB;

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
            if (e.CmdType == CmdType.Screen)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    BitmapConverter.DrawBitmapToWriteableBitmap(((ScreenCmd)e).ScreenBmp, screenWB, 0, 0);
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            myTcpClient.Disconnect();
        }
    }
}
