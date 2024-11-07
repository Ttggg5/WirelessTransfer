using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WirelessTransfer.Pages;

namespace WirelessTransfer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HomePage homePage;

        public MainWindow()
        {
            InitializeComponent();

            homePage = new HomePage();
            homePage.BackSignal += homePage_BackSignal;
            homePage.NavigateSignal += homePage_NavigateSignal;
            mainFrame.Navigate(homePage);
        }

        private void homePage_NavigateSignal(object? sender, Page e)
        {
            mainFrame.Navigate(e);
        }

        private void homePage_BackSignal(object? sender, EventArgs e)
        {
            mainFrame.GoBack();
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

        // Disable the keyboard shotcut of frame navigation
        private void mainFrame_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    e.Handled = true;
                    break;
                case Key.Right:
                    e.Handled = true;
                    break;
            }
        }

        // Disable the mouse shotcut of frame navigation
        MouseButtonState xBtn1 = MouseButtonState.Released;
        MouseButtonState xBtn2 = MouseButtonState.Released;

        private void mainFrame_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (xBtn1 == MouseButtonState.Pressed && e.XButton1 == MouseButtonState.Released) e.Handled = true;
            else if (xBtn2 == MouseButtonState.Pressed && e.XButton2 == MouseButtonState.Released) e.Handled = true;
        }

        private void mainFrame_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            xBtn1 = e.XButton1;
            xBtn2 = e.XButton2;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            homePage.StopAll();
        }
    }
}