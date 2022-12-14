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

namespace UDPChat_but_complex
{
    /// <summary>
    /// Interaction logic for SetStringDialog.xaml
    /// </summary>
    public partial class SetStringDialog : Window
    {
        public string Text = string.Empty;
        public SetStringDialog()
        {
            InitializeComponent();
        }

        private void btAccept_Click(object sender, RoutedEventArgs e)
        {
            Text = tbText.Text;
            Close();
        }

        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
