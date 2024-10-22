using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WirelessTransfer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void titlebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void closeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void minimizeBtn_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}