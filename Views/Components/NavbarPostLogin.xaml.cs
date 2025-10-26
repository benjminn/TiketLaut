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
            // Cek apakah sudah di HomePage
            var currentWindow = Window.GetWindow(this);
            if (currentWindow is HomePage)
            {
                // Sudah di HomePage, tidak perlu navigasi
                return;
            }

            // Navigasi ke HomePage dengan mempertahankan ukuran window
            var homePage = new HomePage(isLoggedIn: true);
            if (currentWindow != null)
            {
                homePage.Left = currentWindow.Left;
                homePage.Top = currentWindow.Top;
                homePage.Width = currentWindow.Width;
                homePage.Height = currentWindow.Height;
                homePage.WindowState = currentWindow.WindowState;
            }
            homePage.Show();
            currentWindow?.Close();
        }

        private void BtnCekBooking_Click(object sender, RoutedEventArgs e)
        {
            // Cek apakah sudah di CekBookingWindow
            var currentWindow = Window.GetWindow(this);
            if (currentWindow is CekBookingWindow)
            {
                // Sudah di CekBookingWindow, tidak perlu navigasi
                return;
            }

            // Navigasi ke halaman Cek Booking dengan mempertahankan ukuran window
            var cekBookingWindow = new CekBookingWindow();
            if (currentWindow != null)
            {
                cekBookingWindow.Left = currentWindow.Left;
                cekBookingWindow.Top = currentWindow.Top;
                cekBookingWindow.Width = currentWindow.Width;
                cekBookingWindow.Height = currentWindow.Height;
                cekBookingWindow.WindowState = currentWindow.WindowState;
            }
            cekBookingWindow.Show();
            currentWindow?.Close();
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