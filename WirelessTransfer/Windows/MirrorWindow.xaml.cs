﻿using System;
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
        WriteableBitmap screenWB = null;

        public MirrorWindow(MyTcpClient myTcpClient)
        {
            InitializeComponent();

            this.myTcpClient = myTcpClient;
            myTcpClient.ReceivedCmd += myTcpClient_ReceivedCmd;
            myTcpClient.Connected += myTcpClient_Connected;
            myTcpClient.Disconnected += myTcpClient_Disconnected;
        }

        private void myTcpClient_Disconnected(object? sender, EventArgs e)
        {
            Close();
        }

        private void myTcpClient_Connected(object? sender, EventArgs e)
        {
            
        }

        private void myTcpClient_ReceivedCmd(object? sender, Cmd e)
        {
            if (e.CmdType == CmdType.Screen)
            {
                ScreenCmd screenCmd = (ScreenCmd)e;
                if (screenWB == null)
                {
                    screenWB = new WriteableBitmap(screenCmd.ScreenBmp.Width, screenCmd.ScreenBmp.Height, 96, 96, PixelFormats.Bgra32, null);
                    screenImg.Source = screenWB;
                }
                BitmapConverter.DrawBitmapToWriteableBitmap(screenCmd.ScreenBmp, screenWB, 0, 0);
            }
        }
    }
}