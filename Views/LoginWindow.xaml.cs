using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TiketLaut.Views;  // UBAH dari TiketLaut.Views.Assets

namespace TiketLaut.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Ganti dengan nama TextBox yang sesuai di XAML Anda
            // Misalnya jika di XAML namanya "txtEmail", ubah menjadi:
            string username = txtEmail.Text;  // ← Sesuaikan dengan nama di XAML
            string password = txtPassword.Password;

            // Validasi sederhana (nanti bisa diganti dengan database)
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                // Login berhasil
                var homePage = new HomePage(isLoggedIn: true, username: username);
                this.Close();
                homePage.Show();
            }
            else
            {
                MessageBox.Show("Username dan password tidak boleh kosong!", 
                               "Login Gagal", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Warning);
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


