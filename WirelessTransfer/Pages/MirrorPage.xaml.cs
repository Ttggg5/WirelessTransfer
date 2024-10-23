using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WirelessTransfer.Pages
{
    /// <summary>
    /// MirrorPage.xaml 的互動邏輯
    /// </summary>
    public partial class MirrorPage : Page
    {
        public MirrorPage()
        {
            InitializeComponent();
            Tag = PageFunction.Mirror;
        }

        public void StopSearching()
        {
            deviceFinder.StopSearching();
        }
    }
}
