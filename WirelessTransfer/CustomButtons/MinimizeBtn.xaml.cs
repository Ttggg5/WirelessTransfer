﻿using System.Windows.Input;
using System.Windows.Media;

namespace WirelessTransfer.CustomButtons
{
    /// <summary>
    /// MinimizeBtn.xaml 的互動邏輯
    /// </summary>
    public partial class MinimizeBtn : System.Windows.Controls.UserControl
    {
        public event EventHandler<MouseButtonEventArgs> Click;

        SolidColorBrush normalScb = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 39, 145, 116));
        SolidColorBrush hoverScb = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 79, 171, 146));
        SolidColorBrush downScb = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 44, 103, 87));
        bool isDown = false;

        public MinimizeBtn()
        {
            InitializeComponent();
            backgroundBorder.Background = normalScb;
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDown = true;
            backgroundBorder.Background = downScb;
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDown) Click?.Invoke(this, e);
            isDown = false;
            backgroundBorder.Background = hoverScb;
        }

        private void UserControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            backgroundBorder.Background = hoverScb;
        }

        private void UserControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            backgroundBorder.Background = normalScb;
        }
    }
}
