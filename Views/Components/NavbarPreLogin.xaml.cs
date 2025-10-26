using System.Windows;
using System.Windows.Controls;

namespace TiketLaut.Views.Components
{
    public partial class NavbarPreLogin : UserControl
    {
        public NavbarPreLogin()
        {
            InitializeComponent();
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Navigasi ke Home", "Info");
        }

        private void BtnCekBooking_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Navigasi ke Cek Booking", "Info");
        }

        private void BtnNotifikasi_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Navigasi ke Notifikasi", "Info");
        }

        private void BtnFAQ_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Navigasi ke FAQ", "Info");
        }

        private void BtnMasuk_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            Window.GetWindow(this)?.Close();
            loginWindow.Show();
        }
    }
}