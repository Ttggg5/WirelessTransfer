using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WirelessTransfer.CustomButtons
{
    /// <summary>
    /// FunctionBtn.xaml 的互動邏輯
    /// </summary>
    public partial class FunctionBtn : System.Windows.Controls.UserControl
    {
        public event EventHandler<MouseButtonEventArgs> Click;


        public BitmapSource Icon
        {
            get { return (BitmapSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Icon.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(BitmapSource), typeof(FunctionBtn), new PropertyMetadata(null));

        public string IconDescription
        {
            get { return (string)GetValue(IconDescriptionProperty); }
            set { SetValue(IconDescriptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IconDescription.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IconDescriptionProperty =
            DependencyProperty.Register("IconDescription", typeof(string), typeof(FunctionBtn), new PropertyMetadata(""));


        SolidColorBrush normalScb = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 78, 78, 78));
        SolidColorBrush hoverScb = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 130, 130, 130));
        SolidColorBrush downScb = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 60, 60, 60));
        bool isDown = false;

        public FunctionBtn()
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
