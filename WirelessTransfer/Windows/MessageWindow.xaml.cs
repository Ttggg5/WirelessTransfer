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
using System.Windows.Shapes;

namespace WirelessTransfer.Windows
{
    /// <summary>
    /// MessageWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MessageWindow : Window
    {
        public MessageWindow(string message, bool hasOption)
        {
            InitializeComponent();

            messageTb.Text = message;

            if (!hasOption)
                cancelBtn.Visibility = Visibility.Collapsed;
        }

        private void acceptBtn_Click(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void cancelBtn_Click(object sender, MouseButtonEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
