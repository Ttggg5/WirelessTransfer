using System.Windows.Controls;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// MirrorPage.xaml 的互動邏輯
    /// </summary>
    public partial class MirrorPage : Page
    {
        public event EventHandler Back;

        public MirrorPage()
        {
            InitializeComponent();
        }

        private void backBtn_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Back?.Invoke(this, e);
        }
    }
}
