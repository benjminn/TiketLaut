using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly PenggunaService _penggunaService;

        public RegisterWindow()
        {
            InitializeComponent();
            _penggunaService = new PenggunaService();

            // Set default values
            cmbJenisKelamin.SelectedIndex = -1;
            dpTanggalLahir.SelectedDate = null;
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            // Validasi input
            if (!ValidateInput())
                return;

            // Ambil data dari form
            var pengguna = new Pengguna
            {
                nama = txtNamaLengkap.Text.Trim(),
                jenis_kelamin = (cmbJenisKelamin.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "",
                tanggal_lahir = DateOnly.FromDateTime(dpTanggalLahir.SelectedDate!.Value),
                email = txtEmail.Text.Trim(),
                password = txtPassword.Password,
                no_hp = txtNIK.Text.Trim(),
                kewarganegaraan = "Indonesia",
                alamat = null,
                tanggal_daftar = DateTime.UtcNow  // ? UBAH dari DateTime.Now ke DateTime.UtcNow
            };

            // Show loading
            btnRegister.IsEnabled = false;
            btnRegister.Content = "Memproses...";

            try
            {
                var (success, message) = await _penggunaService.RegisterAsync(pengguna);

                if (success)
                {
                    MessageBox.Show(
                        "? Registrasi berhasil!\n\nSilakan login dengan akun baru Anda.",
                        "Sukses",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show(
                        $"? Registrasi gagal!\n\n{message}",
                        "Registrasi Gagal",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"? Terjadi kesalahan:\n\n{ex.Message}\n\n" +
                    $"Detail: {ex.InnerException?.Message ?? "Tidak ada detail tambahan"}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                btnRegister.IsEnabled = true;
                btnRegister.Content = "Daftar";
            }
        }

        private bool ValidateInput()
        {
            // Validasi Nama Lengkap
            if (string.IsNullOrWhiteSpace(txtNamaLengkap.Text))
            {
                MessageBox.Show("Nama lengkap tidak boleh kosong!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNamaLengkap.Focus();
                return false;
            }

            // Validasi Jenis Kelamin
            if (cmbJenisKelamin.SelectedIndex == -1)
            {
                MessageBox.Show("Silakan pilih jenis kelamin!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbJenisKelamin.Focus();
                return false;
            }

            // Validasi Tanggal Lahir
            if (!dpTanggalLahir.SelectedDate.HasValue)
            {
                MessageBox.Show("Silakan pilih tanggal lahir!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpTanggalLahir.Focus();
                return false;
            }

            // Validasi umur minimal 17 tahun
            if (dpTanggalLahir.SelectedDate.HasValue)
            {
                var age = DateTime.Now.Year - dpTanggalLahir.SelectedDate.Value.Year;
                if (dpTanggalLahir.SelectedDate.Value.AddYears(age) > DateTime.Now)
                    age--;

                if (age < 17)
                {
                    MessageBox.Show("Usia minimal untuk registrasi adalah 17 tahun!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    dpTanggalLahir.Focus();
                    return false;
                }
            }

            // Validasi NIK
            if (string.IsNullOrWhiteSpace(txtNIK.Text))
            {
                MessageBox.Show("NIK tidak boleh kosong!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNIK.Focus();
                return false;
            }

            if (txtNIK.Text.Length != 16 || !txtNIK.Text.All(char.IsDigit))
            {
                MessageBox.Show("NIK harus berupa 16 digit angka!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNIK.Focus();
                return false;
            }

            // Validasi Email
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Email tidak boleh kosong!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            if (!IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show("Format email tidak valid!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            // Validasi Password
            if (string.IsNullOrEmpty(txtPassword.Password))
            {
                MessageBox.Show("Password tidak boleh kosong!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            if (txtPassword.Password.Length < 8)
            {
                MessageBox.Show("Password minimal 8 karakter!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            if (!txtPassword.Password.Any(char.IsLower) || !txtPassword.Password.Any(char.IsUpper))
            {
                MessageBox.Show("Password harus mengandung huruf kecil dan besar!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return false;
            }

            // Validasi Konfirmasi Password
            if (txtPassword.Password != txtKonfirmasiPassword.Password)
            {
                MessageBox.Show("Konfirmasi password tidak cocok!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtKonfirmasiPassword.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void BtnFacebookRegister_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Registrasi dengan Facebook akan segera tersedia!", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnGoogleRegister_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Registrasi dengan Google akan segera tersedia!", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TxtMasuk_Click(object sender, MouseButtonEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void txtNamaLengkap_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}

