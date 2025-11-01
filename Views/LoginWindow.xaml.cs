using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class LoginWindow : Window
    {
        private readonly PenggunaService _penggunaService;

        public LoginWindow()
        {
            InitializeComponent();
            _penggunaService = new PenggunaService();
            
            // Test database connection on load
            TestDatabaseConnection();
        }

        private async void TestDatabaseConnection()
        {
            try
            {
                var isConnected = await DatabaseService.TestConnectionAsync();
                if (!isConnected)
                {
                    MessageBox.Show(
                        "⚠️ Tidak dapat terhubung ke database Supabase!\n\n" +
                        "Pastikan:\n" +
                        "1. Koneksi internet aktif\n" +
                        "2. Connection string di appsettings.json benar\n" +
                        "3. Database Supabase sudah dibuat",
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    // Optional: tampilkan pesan sukses (comment jika tidak perlu)
                    // MessageBox.Show("✅ Koneksi ke database berhasil!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error testing connection: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            // Validasi input
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Email dan password tidak boleh kosong!",
                               "Login Gagal",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            // Show loading
            btnLogin.IsEnabled = false;
            btnLogin.Content = "Memproses...";

            try
            {
                // Validate login dari database
                var pengguna = await _penggunaService.ValidateLoginAsync(email, password);

                if (pengguna != null)
                {
                    // Login berhasil - simpan session
                    SessionManager.CurrentUser = pengguna;

                    MessageBox.Show($"Selamat datang, {pengguna.nama}!",
                                   "Login Berhasil",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);

                    // Buka HomePage
                    var homePage = new HomePage(isLoggedIn: true, username: pengguna.nama);
                    homePage.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Email atau password salah!",
                                   "Login Gagal",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan:\n{ex.Message}",
                               "Error",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "Masuk";
            }
        }

        private void BtnFacebook_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Login dengan Facebook akan segera tersedia!", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnGoogle_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Login dengan Google akan segera tersedia!", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void txtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void txtEmail_TextChanged_1(object sender, TextChangedEventArgs e)
        {
        }

        private void TxtBuatAkun_Click(object sender, MouseButtonEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}


