using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminJadwalPage : UserControl
    {
        private readonly JadwalService _jadwalService;
        private readonly PelabuhanService _pelabuhanService;
        private ObservableCollection<JadwalViewModel> _allJadwals = new ObservableCollection<JadwalViewModel>();
        private ObservableCollection<JadwalViewModel> _filteredJadwals = new ObservableCollection<JadwalViewModel>();

        public AdminJadwalPage()
        {
            InitializeComponent();
            _jadwalService = new JadwalService();
            _pelabuhanService = new PelabuhanService();
            LoadAllDataAsync();
        }

        private async void LoadAllDataAsync()
        {
            // Load data sequentially to avoid DbContext threading issues
            // Note: Methods already have Dispatcher.InvokeAsync for UI updates
            await LoadJadwalDataAsync();
            await LoadPelabuhanComboBoxesAsync();
            await LoadKapalComboBoxAsync();
        }

        private async Task LoadJadwalDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AdminJadwalPage] Loading jadwal data...");
                
                var jadwals = await _jadwalService.GetAllJadwalAsync();
                
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] Received {jadwals.Count} jadwals from service");
                
                await Dispatcher.InvokeAsync(() =>
                {
                    _allJadwals.Clear();
                    _filteredJadwals.Clear();
                    
                    foreach (var jadwal in jadwals)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] Processing jadwal ID {jadwal.jadwal_id}");
                        System.Diagnostics.Debug.WriteLine($"  - Asal: {jadwal.pelabuhan_asal?.nama_pelabuhan ?? "NULL"}");
                        System.Diagnostics.Debug.WriteLine($"  - Tujuan: {jadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "NULL"}");
                        System.Diagnostics.Debug.WriteLine($"  - Kapal: {jadwal.kapal?.nama_kapal ?? "NULL"}");
                        System.Diagnostics.Debug.WriteLine($"  - Waktu Berangkat: {jadwal.waktu_berangkat}");
                        System.Diagnostics.Debug.WriteLine($"  - Waktu Tiba: {jadwal.waktu_tiba}");
                        
                        var vm = new JadwalViewModel
                        {
                            jadwal_id = jadwal.jadwal_id,
                            pelabuhan_asal = jadwal.pelabuhan_asal!,
                            pelabuhan_tujuan = jadwal.pelabuhan_tujuan!,
                            kapal = jadwal.kapal!,
                            waktu_berangkat = jadwal.waktu_berangkat,
                            waktu_tiba = jadwal.waktu_tiba,
                            kelas_layanan = jadwal.kelas_layanan,
                            status = jadwal.status,
                            IsSelected = false
                        };
                        _allJadwals.Add(vm);
                        _filteredJadwals.Add(vm);
                    }
                    
                    dgJadwal.ItemsSource = _filteredJadwals;
                    System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] DataGrid bound with {_filteredJadwals.Count} items");
                });
                
                await Dispatcher.InvokeAsync(() =>
                {
                    if (_allJadwals.Count == 0)
                    {
                        MessageBox.Show(
                            "Tidak ada jadwal yang valid ditemukan.\n\n" +
                            "Kemungkinan:\n" +
                            "1. Database kosong\n" +
                            "2. Data jadwal memiliki timestamp NULL (48 data dengan NULL timestamp akan difilter)\n\n" +
                            "Silakan tambah jadwal baru atau perbaiki data di database.",
                            "Info",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] StackTrace: {ex.StackTrace}");
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error loading data: {ex.Message}\n\nCek Debug Output untuk detail lengkap.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task LoadPelabuhanComboBoxesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AdminJadwalPage] Loading pelabuhan for filters...");
                var pelabuhans = await _pelabuhanService.GetAllPelabuhanAsync();
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] Loaded {pelabuhans.Count} pelabuhans");
                
                // UI operations must run on UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    // Load Pelabuhan Asal
                    cmbPelabuhanAsal.Items.Clear();
                    cmbPelabuhanAsal.Items.Add(new ComboBoxItem { Content = "-- Semua Pelabuhan --", Tag = null });
                    foreach (var pelabuhan in pelabuhans)
                    {
                        cmbPelabuhanAsal.Items.Add(new ComboBoxItem 
                        { 
                            Content = pelabuhan.nama_pelabuhan,
                            Tag = pelabuhan.pelabuhan_id
                        });
                    }
                    cmbPelabuhanAsal.SelectedIndex = 0;
                    System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] Pelabuhan Asal loaded: {cmbPelabuhanAsal.Items.Count} items");

                    // Load Pelabuhan Tujuan
                    cmbPelabuhanTujuan.Items.Clear();
                    cmbPelabuhanTujuan.Items.Add(new ComboBoxItem { Content = "-- Semua Pelabuhan --", Tag = null });
                    foreach (var pelabuhan in pelabuhans)
                    {
                        cmbPelabuhanTujuan.Items.Add(new ComboBoxItem 
                        { 
                            Content = pelabuhan.nama_pelabuhan,
                            Tag = pelabuhan.pelabuhan_id
                        });
                    }
                    cmbPelabuhanTujuan.SelectedIndex = 0;
                    System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] Pelabuhan Tujuan loaded: {cmbPelabuhanTujuan.Items.Count} items");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] ERROR loading pelabuhan: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error loading pelabuhan filter: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadKapalComboBoxAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AdminJadwalPage] Loading kapal for filters...");
                var jadwals = await _jadwalService.GetAllJadwalAsync();
                var kapals = jadwals
                    .Where(j => j.kapal != null)
                    .Select(j => j.kapal)
                    .GroupBy(k => k!.kapal_id)
                    .Select(g => g.First())
                    .OrderBy(k => k!.nama_kapal)
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] Found {kapals.Count} unique kapals");

                // UI operations must run on UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    // Load Kapal
                    cmbKapal.Items.Clear();
                    cmbKapal.Items.Add(new ComboBoxItem { Content = "-- Semua Kapal --", Tag = null });
                    foreach (var kapal in kapals)
                    {
                        cmbKapal.Items.Add(new ComboBoxItem 
                        { 
                            Content = kapal!.nama_kapal,
                            Tag = kapal.kapal_id
                        });
                    }
                    cmbKapal.SelectedIndex = 0;
                    System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] Kapal loaded: {cmbKapal.Items.Count} items");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] ERROR loading kapal: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error loading kapal filter: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error loading pelabuhan: {ex.Message}");
            }
        }

        private async void BtnAddJadwal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AdminJadwalFormDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadJadwalDataAsync();
            }
        }

        private void BtnDetailJadwal_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int jadwalId)
            {
                // Open detail window
                var detailWindow = new AdminJadwalDetailWindow(jadwalId);
                detailWindow.ShowDialog();
            }
        }

        private async void BtnEditJadwal_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int jadwalId)
            {
                var jadwal = await _jadwalService.GetJadwalByIdAsync(jadwalId);
                if (jadwal != null)
                {
                    var dialog = new AdminJadwalFormDialog(jadwal);
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadJadwalDataAsync();
                    }
                }
            }
        }

        private async void BtnDeleteJadwal_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int jadwalId)
            {
                var result = MessageBox.Show(
                    "Apakah Anda yakin ingin menghapus jadwal ini?",
                    "Konfirmasi Hapus",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var (success, message) = await _jadwalService.DeleteJadwalAsync(jadwalId);
                    MessageBox.Show(message, success ? "Success" : "Error",
                        MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (success)
                    {
                        await LoadJadwalDataAsync();
                    }
                }
            }
        }

        private async void BtnBulkDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedJadwals = _filteredJadwals.Where(j => j.IsSelected).ToList();
            
            if (selectedJadwals.Count == 0)
            {
                MessageBox.Show("Pilih minimal satu jadwal untuk dihapus!", "Info", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Apakah Anda yakin ingin menghapus {selectedJadwals.Count} jadwal terpilih?",
                "Konfirmasi Hapus Massal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var jadwalIds = selectedJadwals.Select(j => j.jadwal_id).ToList();
                var bulkResult = await _jadwalService.BulkDeleteJadwalAsync(jadwalIds);
                
                MessageBox.Show(bulkResult.message, bulkResult.success ? "Success" : "Error",
                    MessageBoxButton.OK, bulkResult.success ? MessageBoxImage.Information : MessageBoxImage.Error);

                if (bulkResult.success)
                {
                    await LoadJadwalDataAsync();
                    chkSelectAll.IsChecked = false;
                }
            }
        }

        private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            var isChecked = chkSelectAll.IsChecked ?? false;
            foreach (var jadwal in _filteredJadwals)
            {
                jadwal.IsSelected = isChecked;
            }
            dgJadwal.Items.Refresh();
        }

        // Filter Methods
        private void ApplyFilters()
        {
            // Null check - return early if data not loaded yet or UI not initialized
            if (_allJadwals == null || _filteredJadwals == null || 
                txtSearch == null || cmbPelabuhanAsal == null || 
                cmbPelabuhanTujuan == null || cmbStatus == null || 
                cmbKapal == null || cmbKelasLayanan == null ||
                dpTanggalDari == null || dpTanggalSampai == null)
                return;

            var searchText = txtSearch.Text?.ToLower() ?? string.Empty;
            var selectedPelabuhanAsal = (cmbPelabuhanAsal.SelectedItem as ComboBoxItem)?.Tag as int?;
            var selectedPelabuhanTujuan = (cmbPelabuhanTujuan.SelectedItem as ComboBoxItem)?.Tag as int?;
            var selectedStatus = (cmbStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var selectedKapal = (cmbKapal.SelectedItem as ComboBoxItem)?.Tag as int?;
            var selectedKelasLayanan = (cmbKelasLayanan.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var tanggalDari = dpTanggalDari.SelectedDate;
            var tanggalSampai = dpTanggalSampai.SelectedDate;

            var filtered = _allJadwals.Where(j =>
            {
                // Search filter
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var matchSearch = 
                        j.pelabuhan_asal?.nama_pelabuhan?.ToLower().Contains(searchText) == true ||
                        j.pelabuhan_tujuan?.nama_pelabuhan?.ToLower().Contains(searchText) == true ||
                        j.kapal?.nama_kapal?.ToLower().Contains(searchText) == true ||
                        j.kelas_layanan?.ToLower().Contains(searchText) == true;
                    
                    if (!matchSearch) return false;
                }

                // Pelabuhan Asal filter
                if (selectedPelabuhanAsal.HasValue && j.pelabuhan_asal?.pelabuhan_id != selectedPelabuhanAsal.Value)
                    return false;

                // Pelabuhan Tujuan filter
                if (selectedPelabuhanTujuan.HasValue && j.pelabuhan_tujuan?.pelabuhan_id != selectedPelabuhanTujuan.Value)
                    return false;

                // Status filter
                if (!string.IsNullOrWhiteSpace(selectedStatus) && j.status != selectedStatus)
                    return false;

                // Kapal filter
                if (selectedKapal.HasValue && j.kapal?.kapal_id != selectedKapal.Value)
                    return false;

                // Kelas Layanan filter
                if (!string.IsNullOrWhiteSpace(selectedKelasLayanan) && j.kelas_layanan != selectedKelasLayanan)
                    return false;

                // Tanggal Dari filter
                if (tanggalDari.HasValue && j.waktu_berangkat.Date < tanggalDari.Value.Date)
                    return false;

                // Tanggal Sampai filter
                if (tanggalSampai.HasValue && j.waktu_berangkat.Date > tanggalSampai.Value.Date)
                    return false;

                return true;
            }).ToList();

            _filteredJadwals.Clear();
            foreach (var item in filtered)
            {
                _filteredJadwals.Add(item);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbPelabuhanAsal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPelabuhanAsal.SelectedItem != null)
                ApplyFilters();
        }

        private void CmbPelabuhanTujuan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPelabuhanTujuan.SelectedItem != null)
                ApplyFilters();
        }

        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbStatus.SelectedItem != null)
                ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (dpTanggalDari != null && dpTanggalSampai != null)
                ApplyFilters();
        }

        private void CmbKapal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbKapal.SelectedItem != null)
                ApplyFilters();
        }

        private void CmbKelasLayanan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbKelasLayanan.SelectedItem != null)
                ApplyFilters();
        }

        private void BtnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            // Reset all filters
            txtSearch.Text = string.Empty;
            cmbPelabuhanAsal.SelectedIndex = 0;
            cmbPelabuhanTujuan.SelectedIndex = 0;
            cmbStatus.SelectedIndex = 0;
            cmbKapal.SelectedIndex = 0;
            cmbKelasLayanan.SelectedIndex = 0;
            dpTanggalDari.SelectedDate = null;
            dpTanggalSampai.SelectedDate = null;
            
            // Reset to show all data
            _filteredJadwals.Clear();
            foreach (var item in _allJadwals)
            {
                _filteredJadwals.Add(item);
            }
        }

        // ViewModel class untuk binding dengan checkbox
        public class JadwalViewModel
        {
            public int jadwal_id { get; set; }
            public Pelabuhan pelabuhan_asal { get; set; } = null!;
            public Pelabuhan pelabuhan_tujuan { get; set; } = null!;
            public Kapal kapal { get; set; } = null!;
            public DateTime waktu_berangkat { get; set; }
            public DateTime waktu_tiba { get; set; }
            public string kelas_layanan { get; set; } = string.Empty;
            public string status { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
        }
    }
}
