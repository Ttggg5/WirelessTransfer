using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        Stopwatch frameSw;

        int frameCount = 0;
        double fps = 0;

        public MirrorWindow(MyTcpClient myTcpClient)
        {
            InitializeComponent();

            this.myTcpClient = myTcpClient;
            myTcpClient.ReceivedCmd += myTcpClient_ReceivedCmd;
            myTcpClient.Connected += myTcpClient_Connected;
            myTcpClient.Disconnected += myTcpClient_Disconnected;
            myTcpClient.Connect();

            frameSw = Stopwatch.StartNew();
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
                ScreenCmd sc = (ScreenCmd)e;
                if (sc.ScreenBmp != null && screenWB != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        BitmapConverter.DrawBitmapToWriteableBitmap(sc.ScreenBmp, screenWB, 0, 0);
                    });
                }

                frameCount++;
                // Every second, calculate the FPS (frames per second)
                if (frameSw.ElapsedMilliseconds >= 1000)
                {
                    fps = frameCount / (frameSw.ElapsedMilliseconds / 1000.0);
                    frameCount = 0;
                    frameSw.Restart();
                    Dispatcher.BeginInvoke(() => { this.Title = $"FPS: {fps:F2}"; });
                }
            }
            else if (e.CmdType == CmdType.ScreenInfo)
            {
                ScreenInfoCmd sic = (ScreenInfoCmd)e;
                Dispatcher.Invoke(() =>
                {
                    screenWB = new WriteableBitmap(sic.Width, sic.Height, 96, 96, PixelFormats.Bgr32, null); // jpg format
                    screenImg.Source = screenWB;
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            myTcpClient.Disconnect();
        }
    }
}
