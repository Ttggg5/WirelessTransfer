using System.Windows;
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

        private void homePage_NavigateSignal(object? sender, System.Windows.Controls.Page e)
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
    }
}