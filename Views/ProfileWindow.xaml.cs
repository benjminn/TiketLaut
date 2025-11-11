using System;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;
using TiketLaut.Models;
using TiketLaut.Views.Components;

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
                CustomDialog.ShowWarning("Peringatan", "Silakan login terlebih dahulu!");
                
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
        private void LoadUserData()
        {
            var user = SessionManager.CurrentUser;
            if (user != null)
            {
                // Set data untuk VIEW MODE (TextBlock - Read Only)
                txtEmailView.Text = user.email;
                txtNamaView.Text = user.nama;
                txtNIKView.Text = user.nomor_induk_kependudukan;
                txtJenisKelaminView.Text = user.jenis_kelamin;
                txtTanggalLahirView.Text = user.tanggal_lahir.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID"));
                txtAlamatView.Text = string.IsNullOrWhiteSpace(user.alamat) ? "-" : user.alamat;

                // Set data untuk EDIT MODE (Input Controls)
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
        private void BtnToggleEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditMode)
            {
                // Masuk mode edit
                EnterEditMode();
            }
            else
            {
                SaveProfile();
            }
        }
        private void EnterEditMode()
        {
            _isEditMode = true;

            // Hide View Mode, Show Edit Mode
            borderEmailView.Visibility = Visibility.Collapsed;
            borderEmailEdit.Visibility = Visibility.Visible;
            
            borderNamaView.Visibility = Visibility.Collapsed;
            borderNamaEdit.Visibility = Visibility.Visible;
            
            borderNIKView.Visibility = Visibility.Collapsed;
            borderNIKEdit.Visibility = Visibility.Visible;
            
            borderJenisKelaminView.Visibility = Visibility.Collapsed;
            borderJenisKelaminEdit.Visibility = Visibility.Visible;
            
            borderTanggalLahirView.Visibility = Visibility.Collapsed;
            borderTanggalLahirEdit.Visibility = Visibility.Visible;
            
            borderAlamatView.Visibility = Visibility.Collapsed;
            borderAlamatEdit.Visibility = Visibility.Visible;

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
        private void ExitEditMode()
        {
            _isEditMode = false;

            // Show View Mode, Hide Edit Mode
            borderEmailView.Visibility = Visibility.Visible;
            borderEmailEdit.Visibility = Visibility.Collapsed;
            
            borderNamaView.Visibility = Visibility.Visible;
            borderNamaEdit.Visibility = Visibility.Collapsed;
            
            borderNIKView.Visibility = Visibility.Visible;
            borderNIKEdit.Visibility = Visibility.Collapsed;
            
            borderJenisKelaminView.Visibility = Visibility.Visible;
            borderJenisKelaminEdit.Visibility = Visibility.Collapsed;
            
            borderTanggalLahirView.Visibility = Visibility.Visible;
            borderTanggalLahirEdit.Visibility = Visibility.Collapsed;
            
            borderAlamatView.Visibility = Visibility.Visible;
            borderAlamatEdit.Visibility = Visibility.Collapsed;

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
            txtPasswordLama.Clear();
            txtPassword.Clear();
            txtConfirmPassword.Clear();

            System.Diagnostics.Debug.WriteLine("[ProfileWindow] Exited edit mode");
        }
        private async void SaveProfile()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNama.Text))
                {
                    CustomDialog.ShowWarning("Validasi", "Nama tidak boleh kosong!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    CustomDialog.ShowWarning("Validasi", "Email tidak boleh kosong!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtNIK.Text))
                {
                    CustomDialog.ShowWarning("Validasi", "NIK tidak boleh kosong!");
                    return;
                }

                if (txtNIK.Text.Length != 16)
                {
                    CustomDialog.ShowWarning("Validasi", "NIK harus 16 digit!");
                    return;
                }

                if (!dpTanggalLahir.SelectedDate.HasValue)
                {
                    CustomDialog.ShowWarning("Validasi", "Pilih tanggal lahir!");
                    return;
                }
                if (!IsValidEmail(txtEmail.Text))
                {
                    CustomDialog.ShowWarning("Validasi", "Format email tidak valid!");
                    return;
                }
                string? newPassword = null;
                if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    // Jika ingin ganti password, harus isi password lama dulu
                    if (string.IsNullOrWhiteSpace(txtPasswordLama.Password))
                    {
                        CustomDialog.ShowWarning("Validasi", "Masukkan password lama untuk mengubah password!");
                        return;
                    }

                    // Verifikasi password lama
                    if (SessionManager.CurrentUser != null && txtPasswordLama.Password != SessionManager.CurrentUser.password)
                    {
                        CustomDialog.ShowWarning("Validasi", "Password lama tidak sesuai!");
                        return;
                    }
                    if (txtPassword.Password.Length < 6)
                    {
                        CustomDialog.ShowWarning("Validasi", "Password baru minimal 6 karakter!");
                        return;
                    }

                    if (txtPassword.Password != txtConfirmPassword.Password)
                    {
                        CustomDialog.ShowWarning("Validasi", "Konfirmasi password tidak cocok!");
                        return;
                    }

                    newPassword = txtPassword.Password;
                }

                // Konfirmasi save
                var result = CustomDialog.ShowQuestion(
                    "Konfirmasi",
                    "Apakah Anda yakin ingin menyimpan perubahan?",
                    CustomDialog.DialogButtons.YesNo);

                if (result != true)
                    return;
                var currentUser = SessionManager.CurrentUser;
                if (currentUser == null)
                {
                    CustomDialog.ShowError("Error", "Sesi login tidak valid!");
                    return;
                }
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
                    currentUser.nama = txtNama.Text.Trim();
                    currentUser.email = txtEmail.Text.Trim();
                    currentUser.nomor_induk_kependudukan = txtNIK.Text.Trim();
                    currentUser.jenis_kelamin = jenisKelamin;
                    currentUser.tanggal_lahir = tanggalLahir;
                    currentUser.alamat = txtAlamat.Text.Trim();
                    navbarPostLogin.SetUserInfo(currentUser.nama);
                    _originalNama = currentUser.nama;
                    _originalEmail = currentUser.email;
                    _originalNIK = currentUser.nomor_induk_kependudukan;
                    _originalJenisKelamin = currentUser.jenis_kelamin;
                    _originalTanggalLahir = currentUser.tanggal_lahir;
                    _originalAlamat = currentUser.alamat ?? "";
                    txtNamaView.Text = currentUser.nama;
                    txtEmailView.Text = currentUser.email;
                    txtNIKView.Text = currentUser.nomor_induk_kependudukan;
                    txtJenisKelaminView.Text = currentUser.jenis_kelamin;
                    txtTanggalLahirView.Text = currentUser.tanggal_lahir.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("id-ID"));
                    txtAlamatView.Text = string.IsNullOrWhiteSpace(currentUser.alamat) ? "-" : currentUser.alamat;

                    CustomDialog.ShowSuccess("Sukses", "Profil berhasil diperbarui!");

                    // Exit edit mode
                    ExitEditMode();
                }
                else
                {
                    CustomDialog.ShowError("Error", "Gagal memperbarui profil. Silakan coba lagi.");
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Terjadi kesalahan: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ProfileWindow] Error saving profile: {ex.Message}");
            }
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
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = CustomDialog.ShowQuestion(
                "Konfirmasi",
                "Batalkan perubahan?",
                CustomDialog.DialogButtons.YesNo);

            if (result == true)
            {
                txtNama.Text = _originalNama;
                txtEmail.Text = _originalEmail;
                txtNIK.Text = _originalNIK;
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
        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            // Jika sedang edit mode, tanyakan konfirmasi
            if (_isEditMode)
            {
                var result = CustomDialog.ShowQuestion(
                    "Konfirmasi",
                    "Ada perubahan yang belum disimpan. Tetap kembali?",
                    CustomDialog.DialogButtons.YesNo);

                if (result != true)
                    return;
            }

            // Kembali ke HomePage (bukan menutup window)
            var homePage = new HomePage(true, SessionManager.CurrentUser?.nama ?? "");
            homePage.Show();
            this.Close();
        }
    }
}
