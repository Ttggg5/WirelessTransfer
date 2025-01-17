﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WirelessTransfer.Tools.Screen;

namespace WirelessTransfer.Pages
{
    public enum PageFunction
    {
        Mirror,
        Extend,
        FileShare,
        Setting,
    }

    /// <summary>
    /// BasePage.xaml 的互動邏輯
    /// </summary>
    public partial class BasePage : Page
    {
        public event EventHandler Back;

        Page page;

        public BasePage(PageFunction function)
        {
            InitializeComponent();

            switch (function)
            {
                case PageFunction.Mirror:
                    page = new MirrorPage();
                    ((MirrorPage)page).DeviceChoosed += Mirror_or_Extend_DeviceChoosed;
                    ((MirrorPage)page).DeviceDisconnected += Mirror_or_Extend_DeviceDisconnected;

                    functionIcon.Source = BitmapConverter.ByteArrayToBitmapImage(PageIconResources.mirror_icon);
                    functionTitle.Text = PageTitleResources.MirrorPageTitle;
                    break;
                case PageFunction.Extend:
                    page = new ExtendPage();
                    ((ExtendPage)page).DeviceChoosed += Mirror_or_Extend_DeviceChoosed;
                    ((ExtendPage)page).DeviceDisconnected += Mirror_or_Extend_DeviceDisconnected;

                    functionIcon.Source = BitmapConverter.ByteArrayToBitmapImage(PageIconResources.extend_screen_icon);
                    functionTitle.Text = PageTitleResources.ExtendPageTitle;
                    break;
                case PageFunction.FileShare:
                    page = new FileSharePage();
                    ((FileSharePage)page).DeviceChoosed += Mirror_or_Extend_DeviceChoosed;
                    ((FileSharePage)page).DeviceDisconnected += Mirror_or_Extend_DeviceDisconnected;

                    functionIcon.Source = BitmapConverter.ByteArrayToBitmapImage(PageIconResources.file_sharing_icon);
                    functionTitle.Text = PageTitleResources.FileSharePageTitle;
                    break;
                case PageFunction.Setting:
                    page = new SettingPage();
                    functionIcon.Source = BitmapConverter.ByteArrayToBitmapImage(PageIconResources.setting_icon);
                    functionTitle.Text = PageTitleResources.SettingPageTitle;
                    break;
            }
            mainContent.Navigate(page);
        }

        private void Mirror_or_Extend_DeviceChoosed(object? sender, CustomControls.DeviceTag e)
        {
            Dispatcher.Invoke(() =>
            {
                backBtn.IsEnabled = false;
            });
        }

        private void Mirror_or_Extend_DeviceDisconnected(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                backBtn.IsEnabled = true;
            });
        }

        public void StopAllProcess()
        {
            if (page == null) return;

            switch ((PageFunction)page.Tag)
            {
                case PageFunction.Mirror:
                    ((MirrorPage)page).StopAll();
                    break;
                case PageFunction.Extend:
                    ((ExtendPage)page).StopAll();
                    break;
                case PageFunction.FileShare:
                    ((FileSharePage)page).StopAll();
                    break;
            }
            page = null;
            GC.Collect();
        }

        private void backBtn_Click(object sender, MouseButtonEventArgs e)
        {
            Back?.Invoke(this, e);
        }
    }
}
