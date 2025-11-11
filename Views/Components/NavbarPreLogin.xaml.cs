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
            // User belum login - tampilkan custom dialog login untuk Cek Booking
            var result = CustomDialog.ShowWarning(
                "Masuk diperlukan",
                "Silakan masuk terlebih dahulu untuk mengakses fitur Cek Booking.\nIngin masuk sekarang?",
                CustomDialog.DialogButtons.YesNo);

            if (result == true)
            {
                // User ingin login - buka LoginWindow
                try
                {
                    var currentWindow = Window.GetWindow(this);

                                        LoginSource source = LoginSource.HomePage; // Default

                    if (currentWindow is ScheduleWindow)
                    {
                        source = LoginSource.ScheduleWindow;
                        System.Diagnostics.Debug.WriteLine("[NavbarPreLogin] BtnCekBooking_Click from ScheduleWindow");
                    }
                    else if (currentWindow is HomePage)
                    {
                        source = LoginSource.HomePage;
                        System.Diagnostics.Debug.WriteLine("[NavbarPreLogin] BtnCekBooking_Click from HomePage");
                    }

                    var loginWindow = new LoginWindow(source);                     // Preserve window size and position for login window
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
                    CustomDialog.ShowError(
                        "Error",
                        $"Terjadi kesalahan saat membuka halaman login:\n{ex.Message}");
                }
            }
            // Jika user pilih "No", tidak ada aksi (tetap di tempat)
        }

        private void BtnNotifikasi_Click(object sender, RoutedEventArgs e)
        {
            // User belum login - tampilkan dialog login untuk Notifikasi
            var result = CustomDialog.ShowWarning(
                "Masuk diperlukan",
                "Silakan masuk terlebih dahulu untuk mengakses Notifikasi.\nIngin masuk sekarang?",
                CustomDialog.DialogButtons.YesNo);

            if (result == true)
            {
                // User ingin login - buka LoginWindow
                try
                {
                    var currentWindow = Window.GetWindow(this);

                                        LoginSource source = LoginSource.HomePage; // Default

                    if (currentWindow is ScheduleWindow)
                    {
                        source = LoginSource.ScheduleWindow;
                        System.Diagnostics.Debug.WriteLine("[NavbarPreLogin] BtnNotifikasi_Click from ScheduleWindow");
                    }
                    else if (currentWindow is HomePage)
                    {
                        source = LoginSource.HomePage;
                        System.Diagnostics.Debug.WriteLine("[NavbarPreLogin] BtnNotifikasi_Click from HomePage");
                    }

                    var loginWindow = new LoginWindow(source);                     // Preserve window size and position for login window
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
                    CustomDialog.ShowError(
                        "Error",
                        $"Terjadi kesalahan saat membuka halaman login:\n{ex.Message}");
                }
            }
            // Jika user pilih "No", tidak ada aksi (tetap di tempat)
        }


        private void BtnMasuk_Click(object sender, RoutedEventArgs e)
        {
            var currentWindow = Window.GetWindow(this);

                        LoginSource source = LoginSource.HomePage; // Default

            if (currentWindow is ScheduleWindow)
            {
                source = LoginSource.ScheduleWindow;
                System.Diagnostics.Debug.WriteLine("[NavbarPreLogin] BtnMasuk_Click from ScheduleWindow");
            }
            else if (currentWindow is HomePage)
            {
                source = LoginSource.HomePage;
                System.Diagnostics.Debug.WriteLine("[NavbarPreLogin] BtnMasuk_Click from HomePage");
            }
            else
            {
                // Window lain, gunakan Other atau fallback ke HomePage
                source = LoginSource.HomePage;
                System.Diagnostics.Debug.WriteLine($"[NavbarPreLogin] BtnMasuk_Click from {currentWindow?.GetType().Name ?? "Unknown"}");
            }

            var loginWindow = new LoginWindow(source);             // Preserve window size and position for login window
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
