using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WirelessTransfer.CustomButtons
{
    /// <summary>
    /// BackBtn.xaml 的互動邏輯
    /// </summary>
    public partial class BackBtn : System.Windows.Controls.UserControl
    {
        public event EventHandler<MouseButtonEventArgs> Click;

        BitmapImage normal = new BitmapImage(new Uri("../Assets/back_arrow.png", UriKind.Relative));
        BitmapImage hover = new BitmapImage(new Uri("../Assets/back_arrow_hover.png", UriKind.Relative));
        BitmapImage pressed = new BitmapImage(new Uri("../Assets/back_arrow_pressed.png", UriKind.Relative));
        BitmapImage disabled = new BitmapImage(new Uri("../Assets/back_arrow_disable.png", UriKind.Relative));
        bool isDown = false;

        public BackBtn()
        {
            InitializeComponent();

            img.Source = normal;
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDown = true;
            img.Source = pressed;
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDown && IsEnabled) Click?.Invoke(this, e);
            isDown = false;
            img.Source = hover;
        }

        private void UserControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            img.Source = hover;
        }

        private void UserControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            img.Source = normal;
        }

        private void UserControl_IsEnabledChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (IsEnabled) img.Source = normal;
            else img.Source = disabled;
        }
    }
}
