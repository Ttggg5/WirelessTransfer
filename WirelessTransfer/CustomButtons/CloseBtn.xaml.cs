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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WirelessTransfer.CustomButtons
{
    /// <summary>
    /// CloseBtn.xaml 的互動邏輯
    /// </summary>
    public partial class CloseBtn : System.Windows.Controls.UserControl
    {
        public event EventHandler<MouseButtonEventArgs> Click;

        SolidColorBrush normalScb = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 205, 92, 92));
        SolidColorBrush hoverScb = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 232, 138, 138));
        SolidColorBrush downScb = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 169, 70, 70));
        bool isDown = false;

        public CloseBtn()
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
            backgroundBorder.Background = normalScb;
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
