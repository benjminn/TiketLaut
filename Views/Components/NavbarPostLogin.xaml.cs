using System.Windows;
using System.Windows.Controls;

namespace TiketLaut.Views.Components
{
    public partial class NavbarPostLogin : UserControl
    {
        public NavbarPostLogin()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set nama user yang akan ditampilkan di navbar
        /// </summary>
        /// <param name="username">Nama user yang login</param>
        public void SetUserInfo(string username)
        {
            txtUserInfo.Text = username;
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

        private void BtnUserInfo_Click(object sender, RoutedEventArgs e)
        {
            popupLogout.IsOpen = !popupLogout.IsOpen;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            popupLogout.IsOpen = false;

            var result = MessageBox.Show("Apakah Anda yakin ingin logout?",
                                        "Konfirmasi Logout",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var homePage = new HomePage(isLoggedIn: false);
                Window.GetWindow(this)?.Close();
                homePage.Show();
            }
        }
    }
}