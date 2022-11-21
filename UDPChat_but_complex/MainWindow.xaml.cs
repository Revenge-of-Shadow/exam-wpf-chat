using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace UDPChat_but_complex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UdpClient udpClient = new UdpClient();
        public MainWindow()
        {
            InitializeComponent();

            tbInput.IsEnabled = false;
            btInput.IsEnabled = false;


            IPAddress[] addresses = Dns.GetHostAddresses(System.Environment.MachineName);
            foreach(IPAddress address in addresses)
            {
                cbAdresses.Items.Add(address.ToString());
            }
            cbAdresses.Items.Add("127.0.0.1");
            cbAdresses.Items.Add("Custom...");
        }

        private void btSend_Click(object sender, RoutedEventArgs e)
        {
            if (tbInput.Text.Equals(string.Empty))
            {
                return;
            }
            byte[] buffer = Encoding.ASCII.GetBytes($"{tbName.Text} : {tbInput.Text}");
            IPAddress iPAddress = IPAddress.Parse(cbAdresses.SelectedItem.ToString().Substring(0, cbAdresses.SelectedItem.ToString().LastIndexOf('.')+1) + "255");
            IPEndPoint ep = new IPEndPoint(iPAddress, Int32.Parse(tbPort.Text));
            udpClient.Send(buffer, buffer.Length, ep);
            tbInput.Clear();
        }

        private void StartWork()
        {
            tbInput.IsEnabled = true;
            btInput.IsEnabled = true;

            UdpClient listenClient = new UdpClient(
                new IPEndPoint(IPAddress.Parse(cbAdresses.SelectedItem.ToString()), Int32.Parse(tbPort.Text)));
            listenClient.BeginReceive(MyReceiveCallback, listenClient);
        }

        private void MyReceiveCallback(IAsyncResult ar)
        {
            UdpClient client = ar.AsyncState as UdpClient;
            IPEndPoint ep = new IPEndPoint(0, 0);
            byte[] buffer = client.EndReceive(ar, ref ep);
            client.BeginReceive(MyReceiveCallback, client);
            Application.Current.Dispatcher.Invoke(() =>
            {
                lbMessages.Items.Add(Encoding.ASCII.GetString(buffer));
            });
        }


        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            if (tbName.Text.Equals(string.Empty))
            {
                MessageBox.Show("Enter a nickname first.");
                return;
            }

            cbAdresses.IsEnabled = false;
            tbPort.IsEnabled = false;
            btStart.IsEnabled = false;
            tbName.IsEnabled = false;
            StartWork();
        }

        private void cbAdresses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbAdresses.SelectedIndex != cbAdresses.Items.Count - 1) return;
            SetStringDialog stringDialog = new SetStringDialog();
            stringDialog.ShowDialog();

            if (stringDialog.Text == string.Empty) return;
        }

        // Plus file sending without broadcast.
        //  which is x.x.x.255.
    }
}
