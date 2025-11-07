using System;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;
using AdminModel = TiketLaut.Admin;

namespace TiketLaut.Views
{
    public partial class AdminDashboard : Window
    {
        private readonly AdminService _adminService;
        private AdminModel? _currentAdmin;

        public AdminDashboard()
        {
            InitializeComponent();
            _adminService = new AdminService();
            _currentAdmin = SessionManager.CurrentAdmin;

            if (_currentAdmin == null)
            {
                MessageBox.Show("Anda tidak memiliki akses!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            InitializeUI();
            LoadDashboardStats();
        }

        private void InitializeUI()
        {
            if (_currentAdmin == null) return;

            // Set admin info
            txtAdminName.Text = _currentAdmin.nama;
            
            // Display role dengan text yang user-friendly
            txtAdminRole.Text = _currentAdmin.role == "0" ? "Super Admin" : "Admin";

            // Show/hide menu based on role
            if (_currentAdmin.canCreateAdmin())
            {
                btnMenuAdmin.Visibility = Visibility.Visible;
            }
        }

        private async void LoadDashboardStats()
        {
            try
            {
                var stats = await _adminService.GetDashboardStatsAsync();

                txtTotalPengguna.Text = stats.TotalPengguna.ToString();
                txtTotalTiket.Text = stats.TotalTiket.ToString();
                txtTotalKapal.Text = stats.TotalKapal.ToString();
                txtTotalPelabuhan.Text = stats.TotalPelabuhan.ToString();
                txtPembayaranMenunggu.Text = stats.PembayaranMenungguKonfirmasi.ToString();
                txtPendapatanHariIni.Text = $"IDR {stats.TotalPendapatanHariIni:N0}";
                txtPendapatanBulanIni.Text = $"IDR {stats.TotalPendapatanBulanIni:N0}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Menu Navigation Methods
        private void SetActiveMenu(Button activeButton)
        {
            // Reset all buttons
            btnMenuDashboard.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuKapal.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuPelabuhan.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuJadwal.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuPembayaran.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuNotifikasi.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuAdmin.Style = (Style)FindResource("SidebarButtonStyle");

            // Set active
            activeButton.Style = (Style)FindResource("ActiveSidebarButton");
        }

        private void BtnMenuDashboard_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(btnMenuDashboard);
            txtPageTitle.Text = "Dashboard";
            contentArea.Content = pnlDashboard;
            LoadDashboardStats();
        }

        private void BtnMenuKapal_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(btnMenuKapal);
            txtPageTitle.Text = "Kelola Kapal";
            
            var kapalManagement = new AdminKapalPage();
            contentArea.Content = kapalManagement;
        }

        private void BtnMenuPelabuhan_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(btnMenuPelabuhan);
            txtPageTitle.Text = "Kelola Pelabuhan";
            
            var pelabuhanManagement = new AdminPelabuhanPage();
            contentArea.Content = pelabuhanManagement;
        }

        private void BtnMenuJadwal_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(btnMenuJadwal);
            txtPageTitle.Text = "Kelola Jadwal";
            
            var jadwalManagement = new AdminJadwalPage();
            contentArea.Content = jadwalManagement;
        }

        private void BtnMenuPembayaran_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(btnMenuPembayaran);
            txtPageTitle.Text = "Kelola Pembayaran";
            
            var pembayaranManagement = new AdminPembayaranPage();
            contentArea.Content = pembayaranManagement;
        }

        private void BtnMenuNotifikasi_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(btnMenuNotifikasi);
            txtPageTitle.Text = "Kirim Notifikasi";
            
            var notifikasiManagement = new AdminNotifikasiPage();
            contentArea.Content = notifikasiManagement;
        }

        private void BtnMenuAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (_currentAdmin == null || !_currentAdmin.canCreateAdmin())
            {
                MessageBox.Show("Anda tidak memiliki akses ke menu ini!", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetActiveMenu(btnMenuAdmin);
            txtPageTitle.Text = "Kelola Admin";
            
            var adminManagement = new AdminKelolaAdminPage();
            contentArea.Content = adminManagement;
        }

        private void BtnRefreshDashboard_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboardStats();
            MessageBox.Show("Data berhasil di-refresh!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Apakah Anda yakin ingin logout?", "Konfirmasi Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                SessionManager.Logout();
                
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
