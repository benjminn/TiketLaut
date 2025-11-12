using System;
using System.Windows;
using TiketLaut.Services;
using AdminModel = TiketLaut.Admin;

namespace TiketLaut.Views.Admin
{
    public partial class AdminFormDialog : Window
    {
        private readonly AdminService _adminService;
        private readonly AdminModel? _existingAdmin;
        private readonly bool _isEditMode;
        public AdminFormDialog()
        {
            InitializeComponent();
            _adminService = new AdminService();
            _isEditMode = false;
            _existingAdmin = null;
            
            txtTitle.Text = "Tambah Admin";
            cmbRole.SelectedIndex = 0; // Default: Admin Operasional
        }
        public AdminFormDialog(AdminModel admin)
        {
            InitializeComponent();
            _adminService = new AdminService();
            _isEditMode = true;
            _existingAdmin = admin;
            
            txtTitle.Text = "Edit Admin";
            btnSave.Content = "Update";
            
            // Load data existing
            LoadAdminData();
            
            // Show edit info
            pnlEditInfo.Visibility = Visibility.Visible;
            
            // Password tidak wajib saat edit - update hint text directly via XAML name or leave as is
            // pnlPassword contains TextBlock as first child, need to cast properly
        }

        private void LoadAdminData()
        {
            if (_existingAdmin == null) return;

            txtNama.Text = _existingAdmin.nama;
            txtEmail.Text = _existingAdmin.email;
            cmbRole.SelectedIndex = _existingAdmin.role == "0" ? 1 : 0;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                {
                    MessageBox.Show("Nama tidak boleh kosong!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNama.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                {
                    MessageBox.Show("Email tidak boleh kosong!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtEmail.Focus();
                    return;
                }

                if (!IsValidEmail(txtEmail.Text))
                {
                    MessageBox.Show("Format email tidak valid!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtEmail.Focus();
                    return;
                }

                // Password validation (wajib untuk mode tambah, opsional untuk edit)
                bool isChangingPassword = !string.IsNullOrWhiteSpace(txtPassword.Password);
                
                if (!_isEditMode && string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    MessageBox.Show("Password tidak boleh kosong!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPassword.Focus();
                    return;
                }

                if (isChangingPassword)
                {
                    if (txtPassword.Password.Length < 6)
                    {
                        MessageBox.Show("Password minimal 6 karakter!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtPassword.Focus();
                        return;
                    }

                    if (txtPassword.Password != txtConfirmPassword.Password)
                    {
                        MessageBox.Show("Password dan Konfirmasi Password tidak sama!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtConfirmPassword.Focus();
                        return;
                    }
                }

                if (cmbRole.SelectedItem == null)
                {
                    MessageBox.Show("Pilih role admin!", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbRole.Focus();
                    return;
                }

                var selectedRole = (cmbRole.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "1";

                // Debug: Log selected role
                System.Diagnostics.Debug.WriteLine($"Selected Role: {selectedRole}");

                btnSave.IsEnabled = false;

                if (_isEditMode && _existingAdmin != null)
                {
                    _existingAdmin.nama = txtNama.Text.Trim();
                    _existingAdmin.email = txtEmail.Text.Trim();
                    _existingAdmin.role = selectedRole;
                    
                    if (isChangingPassword)
                    {
                        _existingAdmin.password = txtPassword.Password;
                    }

                    bool success = await _adminService.UpdateAdminAsync(_existingAdmin);
                    if (success)
                    {
                        MessageBox.Show("Admin berhasil diupdate!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Gagal mengupdate admin!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        btnSave.IsEnabled = true;
                    }
                }
                else
                {
                    var newAdmin = new AdminModel
                    {
                        nama = txtNama.Text.Trim(),
                        email = txtEmail.Text.Trim(),
                        password = txtPassword.Password,
                        role = selectedRole
                    };

                    // Debug: Log data being saved
                    System.Diagnostics.Debug.WriteLine($"Creating admin: {newAdmin.nama}, Email: {newAdmin.email}, Role: {newAdmin.role}");

                    var createdAdmin = await _adminService.CreateAdminAsync(newAdmin);
                    if (createdAdmin != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Admin created with role: {createdAdmin.role}");
                        MessageBox.Show("Admin berhasil ditambahkan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Gagal menambahkan admin! Email mungkin sudah terdaftar.", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        btnSave.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnSave.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
    }
}
