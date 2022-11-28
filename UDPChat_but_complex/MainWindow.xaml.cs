using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
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
        private bool ShowIp = false;
        private bool AutoUpdateImages = false;
        private int UpdateInterval = 5;
        UdpClient udpClient = new UdpClient();
        OptionDialog options = new OptionDialog();
        SqlConnection connection;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        public MainWindow()
        {
            connection = new SqlConnection(new SqlConnectionStringBuilder
            {
                InitialCatalog = "SimpleChat",
                DataSource = "127.0.0.1",
                IntegratedSecurity = true,
                MultipleActiveResultSets = true,
                TrustServerCertificate = true
            }.ConnectionString);
            try { connection.Open(); }catch(Exception ex) { MessageBox.Show(ex.Message); }

            InitializeComponent();

            tbInput.IsEnabled = false;
            btInput.IsEnabled = false;
            btFile.IsEnabled = false;


            IPAddress[] addresses = Dns.GetHostAddresses(System.Environment.MachineName);
            foreach(IPAddress address in addresses)
            {
                cbAdresses.Items.Add(address.ToString());
            }
            cbAdresses.Items.Add("127.0.0.1");
            cbAdresses.Items.Add("Custom...");

            dispatcherTimer.Interval = TimeSpan.FromSeconds(UpdateInterval);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            DbGetImages();
        }

        private void StartWork()
        {
            tbInput.IsEnabled = true;
            btInput.IsEnabled = true;
            btFile.IsEnabled = true;

            UdpClient listenClient = new UdpClient(
                new IPEndPoint(IPAddress.Parse(cbAdresses.SelectedItem.ToString()), Int32.Parse(tbPort.Text)));
            listenClient.BeginReceive(MyReceiveCallback, listenClient);
        }

        private void DbAddUser(string login, string password)
        {
            try {
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = $"INSERT INTO Users (login, password) VALUES ('{login}', '{password}');";
                cmd.ExecuteNonQuery();
            }
            catch (SqlException) 
            { 
                
            }
        }
        private void DbAddMessage(string login, string message)
        {
            try
            {
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = $"INSERT INTO Messages (login, message) VALUES ('{login}', '{message}');";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private async void DbAddImage(string login, BitmapImage image)
        {
            if (image is null) return;
            
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (MemoryStream stream = new MemoryStream()) 
            using (BinaryReader br = new BinaryReader(stream))
            {
                encoder.Save(stream);
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                SqlCommand command = connection.CreateCommand();
                command.CommandText = @"INSERT INTO Images(data, login) VALUES (@image, @login);";
                command.Parameters.Add("@image", SqlDbType.Image, (int)stream.Length).Value = stream;
                command.Parameters.Add("@login", SqlDbType.NVarChar, (int)login.Length).Value = login;
                await command.ExecuteNonQueryAsync();
            }
        }

        private void DbPrintMessages()
        {
            try
            {
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT login, message FROM Messages;";
                IDataReader reader = cmd.ExecuteReader();

                lbMessages.Items.Clear();
                while (reader.Read())
                {
                    lbMessages.Items.Add($"{reader.GetString(0)} : {reader.GetString(1)}");
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private async void DbGetImages()
        {
            lbImages.Items.Clear();
            SqlCommand command;
            SqlDataReader reader;
            lock (connection)
            {
                command = connection.CreateCommand();
                command.CommandText = "SELECT Data FROM Images";
                reader = command.ExecuteReader();
            }
            byte[] buffer = new byte[1024];
            using (MemoryStream stream = new MemoryStream())
            {
            while (await reader.ReadAsync())
            {
                    int pos = 0;
                    while (true)
                    {
                        int ret = (int)reader.GetBytes(0, pos, buffer, 0, buffer.Length);
                        if (ret <= 0)
                        {
                            break;
                        }
                        stream.Write(buffer, 0, ret);
                        stream.Flush();
                        pos += ret;
                    }

                    try
                    {
                        lbImages.Items.Add(pos);
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = stream;
                        image.EndInit();
                        Image imageControl = new Image { Source = image, MaxHeight = lbImages.ActualHeight, MaxWidth = lbImages.ActualWidth };
                        lbImages.Items.Add(imageControl);
                    }
                    catch (Exception ex)
                    {
                        lbImages.Items.Add(ex.Message);
                    }
                }
            }
        }

        private void MyReceiveCallback(IAsyncResult ar)
        {
            UdpClient client = ar.AsyncState as UdpClient;
            IPEndPoint ep = new IPEndPoint(0, 0);
            byte[] buffer = client.EndReceive(ar, ref ep);
            client.BeginReceive(MyReceiveCallback, client);
            Application.Current.Dispatcher.Invoke(() =>
            {
                string message = Encoding.ASCII.GetString(buffer);
                if (ShowIp)
                {
                    lbMessages.Items.Add($"({ep.Address.ToString()}) {message}");
                }
                else
                {
                    lbMessages.Items.Add(message);
                }

                string sender = message.Substring(0, message.IndexOf(':') - 1).Trim();
                string text = message.Substring(message.IndexOf(':') + 1).Trim();
                DbAddMessage(sender, text);

            });
        }

        private void btStart_Click(object sender, RoutedEventArgs e)
        {
            if (tbName.Text.Equals(string.Empty))
            {
                MessageBox.Show("Enter a nickname first.");
                return;
            }

            DbAddUser(tbName.Text, tbPassword.Text);

            cbAdresses.IsEnabled = false;
            tbPort.IsEnabled = false;
            btStart.IsEnabled = false;
            tbName.IsEnabled = false;
            tbPassword.IsEnabled = false;

            DbPrintMessages();
            DbGetImages();
            StartWork();
        }

        private void cbAdresses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbAdresses.SelectedIndex != cbAdresses.Items.Count - 1) return;
            SetStringDialog stringDialog = new SetStringDialog();
            stringDialog.ShowDialog();


            if (stringDialog.Text == string.Empty) return;
            if (cbAdresses.Items.IndexOf(stringDialog.Text) != -1) return;

            cbAdresses.Items.Insert(cbAdresses.Items.Count - 1, stringDialog.Text);
            cbAdresses.SelectedIndex = cbAdresses.Items.Count - 2;
        }

        private void btOptions_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
            options.ShowDialog();
            ShowIp = options.ShowIp;
            AutoUpdateImages = options.AutoUpdateImages;
            UpdateInterval = options.UpdateInterval;
            dispatcherTimer.Interval = TimeSpan.FromSeconds(UpdateInterval);
            if (AutoUpdateImages)
            {
                dispatcherTimer.Start();
            }
        }

        private void btSend_Click(object sender, RoutedEventArgs e)
        {
            if (tbInput.Text.Equals(string.Empty))
            {
                return;
            }
            byte[] buffer = Encoding.ASCII.GetBytes($"{tbName.Text} : {tbInput.Text}");
            IPAddress iPAddress = IPAddress.Parse(cbAdresses.SelectedItem.ToString().Substring(0, cbAdresses.SelectedItem.ToString().LastIndexOf('.') + 1) + "255");
            IPEndPoint ep = new IPEndPoint(iPAddress, Int32.Parse(tbPort.Text));
            udpClient.Send(buffer, buffer.Length, ep);
            tbInput.Clear();
        }

        private void btFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image (*.png, *.jpg, *.jpeg, *.bmp)|*.png;*.jpg;*.jpeg;*.bmp";
            bool? res = openFileDialog.ShowDialog(this);
            if(res.HasValue && res.Value)
            {
                dispatcherTimer.Stop();
                BitmapImage image = new BitmapImage(new Uri(openFileDialog.FileName));
                DbAddImage(tbName.Text, image);
                dispatcherTimer.Start();
            }
        }
        private void btRefresh_Click(object sender, RoutedEventArgs e)
        {
            DbGetImages();
        }


        private void Window_Closed(object sender, EventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            dispatcherTimer.Stop();
            connection.Close();
        }


        // Plus file sending without broadcast.
    }
}
