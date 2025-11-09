using System;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;
using TiketLaut.Models;

namespace TiketLaut.Views
{
    public partial class ProfileWindow : Window
    {
        private readonly PenggunaService _penggunaService;
        private bool _isEditMode = false;
        private string _originalNama = "";
        private string _originalEmail = "";
        private string _originalNIK = "";
        private string _originalJenisKelamin = "";
        private DateOnly _originalTanggalLahir;
        private string _originalAlamat = "";

        public ProfileWindow()
        {
            InitializeComponent();
            _penggunaService = new PenggunaService();

            // Set user info di navbar
            if (SessionManager.IsLoggedIn && SessionManager.CurrentUser != null)
            {
                navbarPostLogin.SetUserInfo(SessionManager.CurrentUser.nama);
                LoadUserData();
            }
            else
            {
                // Redirect ke login jika belum login
                MessageBox.Show("Silakan login terlebih dahulu!", "Peringatan",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        /// <summary>
        /// Load data user dari SessionManager
        /// </summary>
        private void LoadUserData()
        {
            var user = SessionManager.CurrentUser;
            if (user != null)
            {
                txtEmail.Text = user.email;
                txtNama.Text = user.nama;
                txtNIK.Text = user.nomor_induk_kependudukan;
                
                // Set jenis kelamin - default -1 jika belum ada
                if (user.jenis_kelamin == "Laki-laki")
                {
                    cmbJenisKelamin.SelectedIndex = 0;
                }
                else if (user.jenis_kelamin == "Perempuan")
                {
                    cmbJenisKelamin.SelectedIndex = 1;
                }
                else
                {
                    cmbJenisKelamin.SelectedIndex = -1; // Tidak ada yang dipilih
                }
                
                // Set tanggal lahir
                dpTanggalLahir.SelectedDate = user.tanggal_lahir.ToDateTime(TimeOnly.MinValue);
                
                // Alamat kosong jika belum ada di database
                txtAlamat.Text = string.IsNullOrWhiteSpace(user.alamat) ? "" : user.alamat;

                // Simpan data original untuk cancel
                _originalEmail = user.email;
                _originalNama = user.nama;
                _originalNIK = user.nomor_induk_kependudukan;
                _originalJenisKelamin = user.jenis_kelamin;
                _originalTanggalLahir = user.tanggal_lahir;
                _originalAlamat = user.alamat ?? "";
            }
        }

        /// <summary>
        /// Toggle antara mode view dan edit
        /// </summary>
        private void BtnToggleEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditMode)
            {
                // Masuk mode edit
                EnterEditMode();
            }
            else
            {
                // Save changes
                SaveProfile();
            }
        }

        /// <summary>
        /// Aktifkan mode edit
        /// </summary>
        private void EnterEditMode()
        {
            _isEditMode = true;

            // Enable editing
            txtNama.IsReadOnly = false;
            txtEmail.IsReadOnly = false;
            txtNIK.IsReadOnly = false;
            cmbJenisKelamin.IsEnabled = true;
            dpTanggalLahir.IsEnabled = true;
            txtAlamat.IsReadOnly = false;

            // Show password fields
            pnlPasswordLama.Visibility = Visibility.Visible;
            pnlPassword.Visibility = Visibility.Visible;
            pnlConfirmPassword.Visibility = Visibility.Visible;

            // Show cancel button
            btnCancel.Visibility = Visibility.Visible;

            // Change button text
            btnToggleEdit.Content = "Simpan Perubahan";
            btnToggleEdit.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00658D"));

            System.Diagnostics.Debug.WriteLine("[ProfileWindow] Entered edit mode");
        }

        /// <summary>
        /// Keluar dari mode edit tanpa menyimpan
        /// </summary>
        private void ExitEditMode()
        {
            _isEditMode = false;

            // Disable editing
            txtNama.IsReadOnly = true;
            txtEmail.IsReadOnly = true;
            txtNIK.IsReadOnly = true;
            cmbJenisKelamin.IsEnabled = false;
            dpTanggalLahir.IsEnabled = false;
            txtAlamat.IsReadOnly = true;

            // Hide password fields
            pnlPasswordLama.Visibility = Visibility.Collapsed;
            pnlPassword.Visibility = Visibility.Collapsed;
            pnlConfirmPassword.Visibility = Visibility.Collapsed;

            // Hide cancel button
            btnCancel.Visibility = Visibility.Collapsed;

            // Reset button
            btnToggleEdit.Content = "Edit Profil";
            btnToggleEdit.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00658D"));

            // Clear password fields
            txtPasswordLama.Clear();
            txtPassword.Clear();
            txtConfirmPassword.Clear();

            System.Diagnostics.Debug.WriteLine("[ProfileWindow] Exited edit mode");
        }

        /// <summary>
        /// Simpan perubahan profil
        /// </summary>
        private async void SaveProfile()
        {
            try
            {
                // Validasi input
                if (string.IsNullOrWhiteSpace(txtNama.Text))
                {
                    MessageBox.Show("Nama tidak boleh kosong!", "Validasi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Email tidak boleh kosong!", "Validasi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtNIK.Text))
                {
                    MessageBox.Show("NIK tidak boleh kosong!", "Validasi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (txtNIK.Text.Length != 16)
                {
                    MessageBox.Show("NIK harus 16 digit!", "Validasi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpTanggalLahir.SelectedDate.HasValue)
                {
                    MessageBox.Show("Pilih tanggal lahir!", "Validasi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi email format
                if (!IsValidEmail(txtEmail.Text))
                {
                    MessageBox.Show("Format email tidak valid!", "Validasi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi password jika ingin diubah
                string? newPassword = null;
                if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    // Jika ingin ganti password, harus isi password lama dulu
                    if (string.IsNullOrWhiteSpace(txtPasswordLama.Password))
                    {
                        MessageBox.Show("Masukkan password lama untuk mengubah password!", "Validasi",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Verifikasi password lama
                    if (SessionManager.CurrentUser != null && txtPasswordLama.Password != SessionManager.CurrentUser.password)
                    {
                        MessageBox.Show("Password lama tidak sesuai!", "Validasi",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Validasi password baru
                    if (txtPassword.Password.Length < 6)
                    {
                        MessageBox.Show("Password baru minimal 6 karakter!", "Validasi",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (txtPassword.Password != txtConfirmPassword.Password)
                    {
                        MessageBox.Show("Konfirmasi password tidak cocok!", "Validasi",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    newPassword = txtPassword.Password;
                }

                // Konfirmasi save
                var result = MessageBox.Show(
                    "Apakah Anda yakin ingin menyimpan perubahan?",
                    "Konfirmasi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Update data
                var currentUser = SessionManager.CurrentUser;
                if (currentUser == null)
                {
                    MessageBox.Show("Sesi login tidak valid!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Update ke database
                // Jenis kelamin bisa kosong jika tidak dipilih
                var jenisKelamin = cmbJenisKelamin.SelectedIndex == -1 
                    ? "" 
                    : ((ComboBoxItem)cmbJenisKelamin.SelectedItem).Content.ToString() ?? "";
                var tanggalLahir = DateOnly.FromDateTime(dpTanggalLahir.SelectedDate!.Value);
                
                bool success = await _penggunaService.UpdateProfile(
                    currentUser.pengguna_id,
                    txtNama.Text.Trim(),
                    txtEmail.Text.Trim(),
                    txtNIK.Text.Trim(),
                    jenisKelamin,
                    tanggalLahir,
                    txtAlamat.Text.Trim(),
                    newPassword);

                if (success)
                {
                    // Update SessionManager
                    currentUser.nama = txtNama.Text.Trim();
                    currentUser.email = txtEmail.Text.Trim();
                    currentUser.nomor_induk_kependudukan = txtNIK.Text.Trim();
                    currentUser.jenis_kelamin = jenisKelamin;
                    currentUser.tanggal_lahir = tanggalLahir;
                    currentUser.alamat = txtAlamat.Text.Trim();

                    // Update navbar
                    navbarPostLogin.SetUserInfo(currentUser.nama);

                    // Update original values
                    _originalNama = currentUser.nama;
                    _originalEmail = currentUser.email;
                    _originalNIK = currentUser.nomor_induk_kependudukan;
                    _originalJenisKelamin = currentUser.jenis_kelamin;
                    _originalTanggalLahir = currentUser.tanggal_lahir;
                    _originalAlamat = currentUser.alamat ?? "";

                    MessageBox.Show("Profil berhasil diperbarui!", "Sukses",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Exit edit mode
                    ExitEditMode();
                }
                else
                {
                    MessageBox.Show("Gagal memperbarui profil. Silakan coba lagi.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ProfileWindow] Error saving profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Validasi format email
        /// </summary>
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

        /// <summary>
        /// Cancel edit dan restore data original
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Batalkan perubahan?",
                "Konfirmasi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Restore original data
                txtNama.Text = _originalNama;
                txtEmail.Text = _originalEmail;
                txtNIK.Text = _originalNIK;
                
                // Restore jenis kelamin
                if (_originalJenisKelamin == "Laki-laki")
                    cmbJenisKelamin.SelectedIndex = 0;
                else if (_originalJenisKelamin == "Perempuan")
                    cmbJenisKelamin.SelectedIndex = 1;
                else
                    cmbJenisKelamin.SelectedIndex = -1;
                    
                dpTanggalLahir.SelectedDate = _originalTanggalLahir.ToDateTime(TimeOnly.MinValue);
                txtAlamat.Text = _originalAlamat;

                // Exit edit mode
                ExitEditMode();
            }
        }

        /// <summary>
        /// Kembali ke halaman sebelumnya
        /// </summary>
        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditMode)
            {
                var result = MessageBox.Show(
                    "Ada perubahan yang belum disimpan. Tetap kembali?",
                    "Konfirmasi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            var homePage = new HomePage(true, SessionManager.CurrentUser?.nama ?? "");
            homePage.Show();
            this.Close();
        }
    }
}
