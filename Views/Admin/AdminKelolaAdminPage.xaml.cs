using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;
using TiketLaut.Views.Admin;
using AdminModel = TiketLaut.Admin;

namespace TiketLaut.Views
{
    public partial class AdminKelolaAdminPage : UserControl
    {
        private readonly AdminService _adminService;
        private List<AdminModel> _allAdmins;
        private List<AdminModel> _filteredAdmins;

        public AdminKelolaAdminPage()
        {
            InitializeComponent();
            _adminService = new AdminService();
            _allAdmins = new List<AdminModel>();
            _filteredAdmins = new List<AdminModel>();
            
            // Set placeholder text AFTER InitializeComponent
            txtSearch.Text = "Cari berdasarkan nama atau email...";
            txtSearch.TextChanged += TxtSearch_TextChanged;
            CheckSuperAdminAccess();
            LoadAdmins();
        }

        private void CheckSuperAdminAccess()
        {
            var currentAdmin = SessionManager.CurrentAdmin;
            if (currentAdmin == null || !currentAdmin.canCreateAdmin())
            {
                MessageBox.Show("Anda tidak memiliki akses ke halaman ini!\nHanya Super Admin yang dapat mengelola admin.", 
                    "Akses Ditolak", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Disable all controls
                dgAdmin.IsEnabled = false;
                txtSearch.IsEnabled = false;
                this.IsEnabled = false;
            }
        }

        private async void LoadAdmins()
        {
            try
            {
                _allAdmins = await _adminService.GetAllAdminsAsync();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error memuat data admin: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            // Guard against null collections during initialization
            if (_allAdmins == null || dgAdmin == null)
                return;

            var searchText = txtSearch?.Text?.ToLower() ?? "";
            
            // Skip if placeholder text
            if (searchText == "cari berdasarkan nama atau email...")
            {
                searchText = "";
            }
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredAdmins = _allAdmins.ToList();
            }
            else
            {
                _filteredAdmins = _allAdmins
                    .Where(a => a.nama.ToLower().Contains(searchText) || 
                               a.email.ToLower().Contains(searchText))
                    .ToList();
            }

            dgAdmin.ItemsSource = _filteredAdmins.Select(a => new
            {
                id_admin = a.admin_id,
                a.nama,
                a.email,
                role_text = a.role == "0" ? "Super Admin" : "Admin Operasional",
                admin = a
            }).ToList();

            // Show/hide empty state
            if (txtEmptyState != null)
            {
                if (dgAdmin.ItemsSource != null && _filteredAdmins.Any())
                {
                    dgAdmin.Visibility = Visibility.Visible;
                    txtEmptyState.Visibility = Visibility.Collapsed;
                }
                else
                {
                    dgAdmin.Visibility = Visibility.Collapsed;
                    txtEmptyState.Visibility = Visibility.Visible;
                }
            }
        }

        // Search box placeholder handling
        private void TxtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Cari berdasarkan nama atau email...")
            {
                txtSearch.Text = "";
                txtSearch.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#212529"));
            }
        }

        private void TxtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Cari berdasarkan nama atau email...";
                txtSearch.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ADB5BD"));
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "Cari berdasarkan nama atau email...";
            txtSearch.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ADB5BD"));
            LoadAdmins();
        }

        private void BtnTambahAdmin_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AdminFormDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadAdmins();
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag == null) return;

                // Extract admin object from anonymous type
                var item = button.Tag;
                var adminProperty = item.GetType().GetProperty("admin");
                if (adminProperty == null) return;
                
                var admin = adminProperty.GetValue(item) as AdminModel;
                if (admin == null)
                {
                    MessageBox.Show("Admin tidak ditemukan!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new AdminDetailDialog(admin);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error membuka detail admin: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag == null) return;

                // Extract admin object from anonymous type
                var item = button.Tag;
                var adminProperty = item.GetType().GetProperty("admin");
                if (adminProperty == null) return;
                
                var admin = adminProperty.GetValue(item) as AdminModel;
                if (admin == null)
                {
                    MessageBox.Show("Admin tidak ditemukan!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new AdminFormDialog(admin);
                if (dialog.ShowDialog() == true)
                {
                    LoadAdmins();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error membuka form edit admin: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnHapus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag == null) return;

                // Extract admin object from anonymous type
                var item = button.Tag;
                var adminProperty = item.GetType().GetProperty("admin");
                if (adminProperty == null) return;
                
                var admin = adminProperty.GetValue(item) as AdminModel;
                if (admin == null)
                {
                    MessageBox.Show("Admin tidak ditemukan!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (admin.role == "0")
                {
                    MessageBox.Show("Tidak dapat menghapus Super Admin!", "Peringatan", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (admin.admin_id == SessionManager.CurrentAdmin?.admin_id)
                {
                    MessageBox.Show("Tidak dapat menghapus akun Anda sendiri!", "Peringatan", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Apakah Anda yakin ingin menghapus admin '{admin.nama}'?\n\nAksi ini tidak dapat dibatalkan!", 
                    "Konfirmasi Hapus", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    bool success = await _adminService.DeleteAdminAsync(admin.admin_id);
                    if (success)
                    {
                        MessageBox.Show("Admin berhasil dihapus!", "Sukses", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadAdmins();
                    }
                    else
                    {
                        MessageBox.Show("Gagal menghapus admin!", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error menghapus admin: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
