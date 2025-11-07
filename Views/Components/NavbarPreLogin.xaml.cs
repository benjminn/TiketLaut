using System;
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
            // Cek apakah sudah di HomePage
            var currentWindow = Window.GetWindow(this);
            if (currentWindow is HomePage)
            {
                // Sudah di HomePage, tidak perlu navigasi
                return;
            }

            // Navigasi ke HomePage (non-logged-in state)
            var homePage = new HomePage(isLoggedIn: false);
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
            // User belum login - tampilkan dialog login untuk Cek Booking
            var result = MessageBox.Show(
                "Silakan login terlebih dahulu untuk mengakses Cek Booking.\n\n" +
                "Ingin login sekarang?",
                "Login Diperlukan",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // User ingin login - buka LoginWindow
                try
                {
                    var currentWindow = Window.GetWindow(this);
                    var loginWindow = new LoginWindow();

                    // Preserve window size and position for login window
                    if (currentWindow != null)
                    {
                        loginWindow.Left = currentWindow.Left;
                        loginWindow.Top = currentWindow.Top;
                        loginWindow.Width = currentWindow.Width;
                        loginWindow.Height = currentWindow.Height;
                        loginWindow.WindowState = currentWindow.WindowState;
                    }

                    loginWindow.Show();
                    currentWindow?.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Terjadi kesalahan saat membuka halaman login:\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            // Jika user pilih "No", tidak ada aksi (tetap di tempat)
        }

        private void BtnNotifikasi_Click(object sender, RoutedEventArgs e)
        {
            // User belum login - tampilkan dialog login untuk Notifikasi
            var result = MessageBox.Show(
                "Silakan login terlebih dahulu untuk mengakses Notifikasi.\n\n" +
                "Ingin login sekarang?",
                "Login Diperlukan",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // User ingin login - buka LoginWindow
                try
                {
                    var currentWindow = Window.GetWindow(this);
                    var loginWindow = new LoginWindow();

                    // Preserve window size and position for login window
                    if (currentWindow != null)
                    {
                        loginWindow.Left = currentWindow.Left;
                        loginWindow.Top = currentWindow.Top;
                        loginWindow.Width = currentWindow.Width;
                        loginWindow.Height = currentWindow.Height;
                        loginWindow.WindowState = currentWindow.WindowState;
                    }

                    loginWindow.Show();
                    currentWindow?.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Terjadi kesalahan saat membuka halaman login:\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            // Jika user pilih "No", tidak ada aksi (tetap di tempat)
        }

        private void BtnFAQ_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Navigasi ke FAQ", "Info");
        }

        private void BtnMasuk_Click(object sender, RoutedEventArgs e)
        {
            var currentWindow = Window.GetWindow(this);
            var loginWindow = new LoginWindow();

            // Preserve window size and position for login window
            if (currentWindow != null)
            {
                loginWindow.Left = currentWindow.Left;
                loginWindow.Top = currentWindow.Top;
                loginWindow.Width = currentWindow.Width;
                loginWindow.Height = currentWindow.Height;
                loginWindow.WindowState = currentWindow.WindowState;
            }

            loginWindow.Show();
            currentWindow?.Close();
        }
    }
}
