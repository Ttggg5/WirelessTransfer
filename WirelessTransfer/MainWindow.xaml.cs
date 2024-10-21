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
        bool mouseDown;
        System.Drawing.Point previousPoint;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void titlebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseDown = true;
            previousPoint = System.Windows.Forms.Cursor.Position;
        }

        private void titlebar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mouseDown = false;
        }

        private void titlebar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (mouseDown)
            {
                System.Drawing.Point curPoint = System.Windows.Forms.Cursor.Position;
                this.Left += curPoint.X - previousPoint.X;
                this.Top += curPoint.Y - previousPoint.Y;
                previousPoint = curPoint;
            }
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