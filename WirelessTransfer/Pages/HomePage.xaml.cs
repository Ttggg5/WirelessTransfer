using System.Windows.Controls;
using System.Windows.Input;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// HomePage.xaml 的互動邏輯
    /// </summary>
    public partial class HomePage : Page
    {
        public event EventHandler BackSignal;
        public event EventHandler<Page> NavigateSignal;

        public HomePage()
        {
            InitializeComponent();
        }

        private void mirrorBtn_Click(object sender, MouseButtonEventArgs e)
        {
            MirrorPage mirrorPage = new MirrorPage();
            mirrorPage.Back += MirrorPage_Back;
            NavigateSignal?.Invoke(this, mirrorPage);
        }

        private void MirrorPage_Back(object? sender, EventArgs e)
        {
            BackSignal?.Invoke(sender, e);
        }

        private void extendBtn_Click(object sender, MouseButtonEventArgs e)
        {

        }

        private void fileShareBtn_Click(object sender, MouseButtonEventArgs e)
        {

        }

        private void settingBtn_Click(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
