﻿using Ini;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using WindowsInput.Native;
using WirelessTransfer.Tools.InternetSocket.Cmd;
using WirelessTransfer.Tools.InternetSocket.MyTcp;
using WirelessTransfer.Tools.Screen;

namespace WirelessTransfer.Windows
{
    /// <summary>
    /// DisplayScreenWindow.xaml 的互動邏輯
    /// </summary>
    public partial class DisplayScreenWindow : Window
    {
        MyTcpClient myTcpClient;
        WriteableBitmap screenWB;
        Stopwatch frameSw;

        int screenWidth = 0;
        int screenHeight = 0;
        double widthScale = 1.0;
        double heightScale = 1.0;
        int frameCount = 0;
        double fps = 0;

        public DisplayScreenWindow(MyTcpClient myTcpClient)
        {
            InitializeComponent();

            WindowState = WindowState.Maximized;

            //InputLanguageManager.SetInputLanguage();

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
            switch (e.CmdType)
            {
                case CmdType.Screen:
                    ScreenCmd sc = (ScreenCmd)e;
                    if (sc.ScreenBmp != null && screenWB != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            BitmapConverter.DrawBitmapToWriteableBitmap(sc.ScreenBmp, screenWB, 0, 0);
                        });
                        frameCount++;
                    }
                    e = null;
                    GC.Collect();

                    // Every second, calculate the FPS (frames per second)
                    if (frameSw.ElapsedMilliseconds >= 1000)
                    {
                        fps = frameCount / (frameSw.ElapsedMilliseconds / 1000.0);
                        frameCount = 0;
                        frameSw.Restart();
                        Dispatcher.BeginInvoke(() => { curPageLb.Text = $"FPS: {fps:F2}"; });
                    }
                    break;
                case CmdType.ScreenInfo:
                    ScreenInfoCmd sic = (ScreenInfoCmd)e;
                    Dispatcher.Invoke(() =>
                    {
                        screenWidth = sic.Width;
                        screenHeight = sic.Height;

                        screenWB = new WriteableBitmap(sic.Width, sic.Height, 96, 96, PixelFormats.Bgra32, null); // jpg format
                        screenImg.Source = screenWB;
                    });
                    break;
                case CmdType.Request:
                    RequestCmd rc = (RequestCmd)e;
                    if (rc.RequestType == RequestType.Disconnect) myTcpClient.Disconnect();
                    break;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            myTcpClient.SendCmd(new RequestCmd(RequestType.Disconnect, Environment.MachineName));
            myTcpClient.Disconnect();
            frameSw.Stop();
        }

        private System.Windows.Point GetRealPoint(System.Windows.Point point)
        {
            point.X *= widthScale;
            point.Y *= heightScale;
            return point;
        }

        private void screenImg_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            myTcpClient.SendCmd(new MouseCmd(GetRealPoint(e.GetPosition(screenImg)), MouseAct.None, 0, true));
        }

        private void screenImg_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            widthScale = screenWidth / screenImg.ActualWidth;
            heightScale = screenHeight / screenImg.ActualHeight;
        }

        private void screenImg_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            myTcpClient.SendCmd(new MouseCmd(GetRealPoint(e.GetPosition(screenImg)), MouseAct.RightButtonDown, 0, true));
        }

        private void screenImg_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            myTcpClient.SendCmd(new MouseCmd(GetRealPoint(e.GetPosition(screenImg)), MouseAct.RightButtonUp, 0, true));
        }

        private void screenImg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            myTcpClient.SendCmd(new MouseCmd(GetRealPoint(e.GetPosition(screenImg)), MouseAct.LeftButtonDown, 0, true));
        }

        private void screenImg_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            myTcpClient.SendCmd(new MouseCmd(GetRealPoint(e.GetPosition(screenImg)), MouseAct.LeftButtonUp, 0, true));
        }

        private void screenImg_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            myTcpClient.SendCmd(new MouseCmd(GetRealPoint(e.GetPosition(screenImg)), MouseAct.MiddleButtonRolled, e.Delta, true));
        }

        private void screenImg_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Forms.Cursor.Hide();
        }

        private void screenImg_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Forms.Cursor.Show();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int keyCode = KeyInterop.VirtualKeyFromKey(e.Key);
            myTcpClient.SendCmd(new KeyboardCmd((VirtualKeyCode)keyCode, KeyState.Down));
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int keyCode = KeyInterop.VirtualKeyFromKey(e.Key);
            myTcpClient.SendCmd(new KeyboardCmd((VirtualKeyCode)keyCode, KeyState.Up));
        }

        private void closeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void minimizeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void titlebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
