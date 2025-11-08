using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;
using AdminModel = TiketLaut.Admin;
using ClosedXML.Excel;
using Microsoft.Win32;

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
            
            // Load data setelah window loaded
            this.Loaded += AdminDashboard_Loaded;
        }

        private async void AdminDashboard_Loaded(object sender, RoutedEventArgs e)
        {
            // Pastikan UI sudah sepenuhnya loaded
            await System.Threading.Tasks.Task.Delay(100);
            
            // Initialize month filter
            InitializeMonthFilter();
            
            // Load dashboard stats dan pendapatan table
            await LoadDashboardStats();
            await LoadPendapatanDetailTable();
        }

        private void InitializeMonthFilter()
        {
            try
            {
                var currentDate = DateTime.UtcNow;
                var culture = System.Globalization.CultureInfo.GetCultureInfo("id-ID");
                
                // Clear existing items
                cmbBulanFilter.Items.Clear();
                
                // Generate 12 bulan ke belakang dari bulan sekarang
                for (int i = 0; i < 12; i++)
                {
                    var date = currentDate.AddMonths(-i);
                    var monthName = culture.DateTimeFormat.GetMonthName(date.Month);
                    var displayText = $"{char.ToUpper(monthName[0]) + monthName.Substring(1)} {date.Year}";
                    var tag = $"{date.Month:D2}-{date.Year}";
                    
                    var item = new ComboBoxItem
                    {
                        Content = displayText,
                        Tag = tag
                    };
                    
                    cmbBulanFilter.Items.Add(item);
                    
                    // Set bulan sekarang sebagai default (index 0)
                    if (i == 0)
                    {
                        item.IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Error InitializeMonthFilter: {ex.Message}");
            }
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

        private async System.Threading.Tasks.Task LoadDashboardStats()
        {
            try
            {
                // Test database connection first
                var canConnect = await DatabaseService.TestConnectionAsync();
                
                if (!canConnect)
                {
                    MessageBox.Show("Tidak dapat terhubung ke database!\n\nPastikan:\n1. Koneksi internet aktif\n2. Database server dapat diakses\n3. Connection string di appsettings.json benar", 
                        "Database Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var stats = await _adminService.GetDashboardStatsAsync();

                // Update existing stats
                txtTotalPengguna.Text = stats.TotalPengguna.ToString();
                txtTotalTiket.Text = stats.TotalTiket.ToString();
                txtTotalKapal.Text = stats.TotalKapal.ToString();
                txtTotalPelabuhan.Text = stats.TotalPelabuhan.ToString();
                txtPembayaranMenunggu.Text = stats.PembayaranMenungguKonfirmasi.ToString();
                txtPendapatanHariIni.Text = $"Rp {stats.TotalPendapatanHariIni:N0}";
                txtPendapatanBulanIni.Text = $"Rp {stats.TotalPendapatanBulanIni:N0}";
                
                // Update new insights
                txtPenggunaBaru.Text = stats.PenggunaBaru7Hari.ToString();
                txtTiketHariIni.Text = stats.TiketHariIni.ToString();
                txtJadwalMingguDepan.Text = stats.JadwalMingguDepan.ToString();
                txtRataPendapatan.Text = $"Rp {stats.RataRataPendapatanPerHari:N0}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Error: {ex.Message}");
                MessageBox.Show($"Error loading dashboard data:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadPendapatanDetailTable()
        {
            try
            {
                // Pastikan UI components sudah ada
                if (cmbBulanFilter == null || dgPendapatanDetail == null || 
                    txtNoPendapatan == null || txtTotalPendapatanTable == null || 
                    txtPeriodePendapatan == null)
                {
                    System.Diagnostics.Debug.WriteLine("[Dashboard] UI components not ready yet");
                    return;
                }

                // Pastikan service sudah ada
                if (_adminService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[Dashboard] AdminService is null");
                    MessageBox.Show("Service tidak tersedia. Silakan restart aplikasi.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get selected month and year from filter
                int bulan, tahun;
                
                try
                {
                    if (cmbBulanFilter.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
                    {
                        var tag = selectedItem.Tag.ToString();
                        if (!string.IsNullOrEmpty(tag))
                        {
                            var parts = tag.Split('-');
                            if (parts.Length == 2)
                            {
                                bulan = int.Parse(parts[0]);
                                tahun = int.Parse(parts[1]);
                            }
                            else
                            {
                                // Default to current month if format invalid
                                bulan = DateTime.UtcNow.Month;
                                tahun = DateTime.UtcNow.Year;
                            }
                        }
                        else
                        {
                            // Default to current month if tag is empty
                            bulan = DateTime.UtcNow.Month;
                            tahun = DateTime.UtcNow.Year;
                        }
                    }
                    else
                    {
                        // Default to current month
                        bulan = DateTime.UtcNow.Month;
                        tahun = DateTime.UtcNow.Year;
                    }
                }
                catch (Exception exParse)
                {
                    System.Diagnostics.Debug.WriteLine($"[Dashboard] Error parsing month filter: {exParse.Message}");
                    // Default to current month on parse error
                    bulan = DateTime.UtcNow.Month;
                    tahun = DateTime.UtcNow.Year;
                }

                // Call service with additional error handling
                List<PendapatanPerRuteKapal> pendapatanList;
                try
                {
                    pendapatanList = await _adminService.GetPendapatanPerRuteKapalAsync(bulan, tahun);
                }
                catch (Exception exService)
                {
                    System.Diagnostics.Debug.WriteLine($"[Dashboard] Error calling service: {exService.Message}");
                    System.Diagnostics.Debug.WriteLine($"[Dashboard] Service Stack Trace: {exService.StackTrace}");
                    
                    // Show user-friendly error
                    MessageBox.Show($"Gagal memuat data pendapatan:\n\n{exService.Message}\n\nSilakan coba lagi atau hubungi administrator.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // Set to empty list to show "no data" message
                    pendapatanList = new List<PendapatanPerRuteKapal>();
                }
                
                // Check if there's data
                if (pendapatanList == null || pendapatanList.Count == 0)
                {
                    // Show message, hide table
                    txtNoPendapatan.Visibility = Visibility.Visible;
                    dgPendapatanDetail.Visibility = Visibility.Collapsed;
                    
                    // Set total to 0
                    txtTotalPendapatanTable.Text = "Rp 0";
                    
                    // Update footer label
                    var bulanNama = System.Globalization.CultureInfo.GetCultureInfo("id-ID").DateTimeFormat.GetMonthName(bulan);
                    txtPeriodePendapatan.Text = $"Total Pendapatan {bulanNama.ToUpper()} {tahun}";
                }
                else
                {
                    // Hide message, show table
                    txtNoPendapatan.Visibility = Visibility.Collapsed;
                    dgPendapatanDetail.Visibility = Visibility.Visible;
                    
                    // Add numbering - dengan null check untuk setiap property
                    var numberedList = pendapatanList.Select((item, index) => new PendapatanPerRuteKapalView
                    {
                        No = index + 1,
                        PelabuhanAsal = item?.PelabuhanAsal ?? "-",
                        PelabuhanTujuan = item?.PelabuhanTujuan ?? "-",
                        NamaKapal = item?.NamaKapal ?? "-",
                        JumlahTiket = item?.JumlahTiket ?? 0,
                        TotalPendapatan = item?.TotalPendapatan ?? 0,
                        TotalPendapatanFormatted = $"Rp {(item?.TotalPendapatan ?? 0):N0}"
                    }).ToList();

                    dgPendapatanDetail.ItemsSource = numberedList;

                    // Update total
                    var total = pendapatanList.Sum(p => p?.TotalPendapatan ?? 0);
                    txtTotalPendapatanTable.Text = $"Rp {total:N0}";
                    
                    // Update footer label with selected period
                    var bulanNama = System.Globalization.CultureInfo.GetCultureInfo("id-ID").DateTimeFormat.GetMonthName(bulan);
                    txtPeriodePendapatan.Text = $"Total Pendapatan {bulanNama.ToUpper()} {tahun}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Error LoadPendapatanDetail: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Dashboard] Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Error memuat data pendapatan:\n\n{ex.Message}\n\nDetail:\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CmbBulanFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Reload table when month filter changes
            await LoadPendapatanDetailTable();
        }

        // Menu Navigation Methods
        private void SetActiveMenu(Button activeButton)
        {
            // Reset all buttons
            btnMenuDashboard.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuTiket.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuKapal.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuPelabuhan.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuJadwal.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuPembayaran.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuNotifikasi.Style = (Style)FindResource("SidebarButtonStyle");
            btnMenuAdmin.Style = (Style)FindResource("SidebarButtonStyle");

            // Set active
            activeButton.Style = (Style)FindResource("ActiveSidebarButton");
        }

        private async void BtnMenuDashboard_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(btnMenuDashboard);
            txtPageTitle.Text = "Dashboard";
            contentArea.Content = pnlDashboard;
            await LoadDashboardStats();
        }

        private void BtnMenuTiket_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(btnMenuTiket);
            txtPageTitle.Text = "Kelola Tiket";

            var tiketPage = new AdminTiketPage();
            contentArea.Content = tiketPage;
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

        private async void BtnRefreshDashboard_Click(object sender, RoutedEventArgs e)
        {
            await LoadDashboardStats();
            MessageBox.Show("Data berhasil di-refresh!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get selected month and year from filter
                int bulan, tahun;
                string bulanNama;
                if (cmbBulanFilter.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
                {
                    var tag = selectedItem.Tag.ToString();
                    var parts = tag!.Split('-');
                    bulan = int.Parse(parts[0]);
                    tahun = int.Parse(parts[1]);
                    bulanNama = System.Globalization.CultureInfo.GetCultureInfo("id-ID").DateTimeFormat.GetMonthName(bulan);
                }
                else
                {
                    // Default to current month
                    bulan = DateTime.UtcNow.Month;
                    tahun = DateTime.UtcNow.Year;
                    bulanNama = System.Globalization.CultureInfo.GetCultureInfo("id-ID").DateTimeFormat.GetMonthName(bulan);
                }

                // Get data for selected month
                var pendapatanList = await _adminService.GetPendapatanPerRuteKapalAsync(bulan, tahun);
                
                if (pendapatanList.Count == 0)
                {
                    MessageBox.Show($"Tidak ada data untuk bulan {bulanNama} {tahun}!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show save file dialog
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"Pendapatan_{bulanNama}_{tahun}.xlsx",
                    Title = "Export ke Excel"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                // Create Excel workbook
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Pendapatan");

                    // Header
                    worksheet.Cell(1, 1).Value = $"LAPORAN PENDAPATAN {bulanNama.ToUpper()} {tahun}";
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Range(1, 1, 1, 6).Merge();

                    worksheet.Cell(2, 1).Value = $"Periode: {bulanNama} {tahun}";
                    worksheet.Cell(2, 1).Style.Font.FontSize = 12;
                    worksheet.Range(2, 1, 2, 6).Merge();

                    // Table header
                    worksheet.Cell(4, 1).Value = "No";
                    worksheet.Cell(4, 2).Value = "Pelabuhan Asal";
                    worksheet.Cell(4, 3).Value = "Pelabuhan Tujuan";
                    worksheet.Cell(4, 4).Value = "Nama Kapal";
                    worksheet.Cell(4, 5).Value = "Jumlah Tiket";
                    worksheet.Cell(4, 6).Value = "Total Pendapatan (Rp)";

                    // Style header
                    var headerRange = worksheet.Range(4, 1, 4, 6);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#00658D");
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Data
                    int row = 5;
                    foreach (var (item, index) in pendapatanList.Select((item, index) => (item, index)))
                    {
                        worksheet.Cell(row, 1).Value = index + 1;
                        worksheet.Cell(row, 2).Value = item.PelabuhanAsal;
                        worksheet.Cell(row, 3).Value = item.PelabuhanTujuan;
                        worksheet.Cell(row, 4).Value = item.NamaKapal;
                        worksheet.Cell(row, 5).Value = item.JumlahTiket;
                        worksheet.Cell(row, 6).Value = item.TotalPendapatan;
                        worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                        row++;
                    }

                    // Total row
                    var totalRow = row;
                    worksheet.Cell(totalRow, 1).Value = "TOTAL";
                    worksheet.Range(totalRow, 1, totalRow, 5).Merge();
                    worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(totalRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#00658D");
                    worksheet.Cell(totalRow, 1).Style.Font.FontColor = XLColor.White;
                    
                    var total = pendapatanList.Sum(p => p.TotalPendapatan);
                    worksheet.Cell(totalRow, 6).Value = total;
                    worksheet.Cell(totalRow, 6).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(totalRow, 6).Style.Font.Bold = true;
                    worksheet.Cell(totalRow, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD700");

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Add borders
                    var dataRange = worksheet.Range(4, 1, totalRow, 6);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    // Save
                    workbook.SaveAs(saveDialog.FileName);
                }

                MessageBox.Show($"Data berhasil di-export ke:\n{saveDialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Open file
                var result = MessageBox.Show("Buka file Excel sekarang?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export Excel] Error: {ex.Message}");
                MessageBox.Show($"Error export Excel:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

    /// <summary>
    /// View model untuk DataGrid dengan numbering
    /// </summary>
    public class PendapatanPerRuteKapalView
    {
        public int No { get; set; }
        public string PelabuhanAsal { get; set; } = string.Empty;
        public string PelabuhanTujuan { get; set; } = string.Empty;
        public string NamaKapal { get; set; } = string.Empty;
        public decimal TotalPendapatan { get; set; }
        public int JumlahTiket { get; set; }
        public string TotalPendapatanFormatted { get; set; } = string.Empty;
    }
}