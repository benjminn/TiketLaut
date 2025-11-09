using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;

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

            // Navigasi ke HomePage dengan mempertahankan session yang sebenarnya
            bool isLoggedIn = SessionManager.IsLoggedIn;
            string username = SessionManager.CurrentUser?.nama ?? "";

            var homePage = new HomePage(isLoggedIn: isLoggedIn, username: username);
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
            // Cek apakah sudah di NotifikasiWindow
            var currentWindow = Window.GetWindow(this);
            if (currentWindow is NotifikasiWindow)
            {
                // Sudah di NotifikasiWindow, tidak perlu navigasi
                return;
            }

            // Navigasi ke NotifikasiWindow (dengan database)
            var notifikasiWindow = new NotifikasiWindow();
            if (currentWindow != null)
            {
                notifikasiWindow.Left = currentWindow.Left;
                notifikasiWindow.Top = currentWindow.Top;
                notifikasiWindow.Width = currentWindow.Width;
                notifikasiWindow.Height = currentWindow.Height;
                notifikasiWindow.WindowState = currentWindow.WindowState;
            }
            notifikasiWindow.Show();
            currentWindow?.Close();
        }

        private void BtnFAQ_Click(object sender, RoutedEventArgs e)
        {
            CustomDialog.ShowInfo("Info", "Navigasi ke FAQ");
        }

        private void BtnUserInfo_Click(object sender, RoutedEventArgs e)
        {
            popupLogout.IsOpen = !popupLogout.IsOpen;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            popupLogout.IsOpen = false;

            var result = CustomDialog.ShowQuestion(
                "Konfirmasi Logout",
                "Apakah Anda yakin ingin logout?",
                CustomDialog.DialogButtons.YesNo);

            if (result == true)
            {
                // Clear session terlebih dahulu
                SessionManager.Logout();

                var homePage = new HomePage(isLoggedIn: false);
                Window.GetWindow(this)?.Close();
                homePage.Show();
            }
        }
    }
}