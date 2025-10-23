using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            string email = txtEmail.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Harap isi email dan password!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Contoh validasi sederhana
            if (email == "admin@tiketlaut.com" && password == "admin123")
            {
                MessageBox.Show("Login berhasil!", "Sukses",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                // Nanti bisa navigasi ke halaman utama
            }
            else
            {
                MessageBox.Show("Email atau password salah!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void txtEmail_TextChanged_1()
        {

        }
    }
}


