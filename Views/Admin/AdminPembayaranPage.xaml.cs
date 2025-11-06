using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminPembayaranPage : UserControl
    {
        private readonly PembayaranService _pembayaranService;
        private ObservableCollection<Pembayaran> _allPembayaran = new ObservableCollection<Pembayaran>();
        private ObservableCollection<Pembayaran> _filteredPembayaran = new ObservableCollection<Pembayaran>();

        public AdminPembayaranPage()
        {
            InitializeComponent();
            _pembayaranService = new PembayaranService();
            LoadAllDataAsync();
        }

        private async void LoadAllDataAsync()
        {
            await LoadPembayaranDataAsync();
            await LoadFilterOptionsAsync();
        }

        private async Task LoadPembayaranDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AdminPembayaranPage] Loading pembayaran data...");
                
                var pembayaranList = await _pembayaranService.GetAllPembayaranAsync();
                
                await Dispatcher.InvokeAsync(() =>
                {
                    _allPembayaran.Clear();
                    _filteredPembayaran.Clear();
                    
                    foreach (var pembayaran in pembayaranList)
                    {
                        _allPembayaran.Add(pembayaran);
                        _filteredPembayaran.Add(pembayaran);
                    }
                    
                    dgPembayaran.ItemsSource = _filteredPembayaran;
                    UpdateSummary();
                    
                    System.Diagnostics.Debug.WriteLine($"[AdminPembayaranPage] Loaded {_allPembayaran.Count} pembayaran records");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminPembayaranPage] Error loading pembayaran: {ex.Message}");
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error loading pembayaran data: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task LoadFilterOptionsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AdminPembayaranPage] Loading filter options...");
                
                var metodeList = await _pembayaranService.GetUniqueMetodePembayaranAsync();
                var penggunaList = await _pembayaranService.GetUniquePenggunaAsync();
                
                await Dispatcher.InvokeAsync(() =>
                {
                    // Load Metode Pembayaran
                    cmbMetodePembayaran.Items.Clear();
                    cmbMetodePembayaran.Items.Add(new ComboBoxItem { Content = "-- Semua Metode --", Tag = null });
                    foreach (var metode in metodeList)
                    {
                        cmbMetodePembayaran.Items.Add(new ComboBoxItem { Content = metode, Tag = metode });
                    }
                    cmbMetodePembayaran.SelectedIndex = 0;
                    
                    // Load Pengguna
                    cmbPengguna.Items.Clear();
                    cmbPengguna.Items.Add(new ComboBoxItem { Content = "-- Semua Pengguna --", Tag = null });
                    foreach (var pengguna in penggunaList)
                    {
                        cmbPengguna.Items.Add(new ComboBoxItem { Content = pengguna, Tag = pengguna });
                    }
                    cmbPengguna.SelectedIndex = 0;
                    
                    System.Diagnostics.Debug.WriteLine($"[AdminPembayaranPage] Filter options loaded");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminPembayaranPage] Error loading filter options: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            if (_allPembayaran == null || _filteredPembayaran == null ||
                txtSearch == null || cmbStatus == null || cmbMetodePembayaran == null || 
                cmbPengguna == null || dpTanggalDari == null || dpTanggalSampai == null ||
                txtJumlahMin == null || txtJumlahMax == null)
            {
                return;
            }

            try
            {
                var searchText = txtSearch.Text?.ToLower() ?? "";
                var statusFilter = ((cmbStatus.SelectedItem as ComboBoxItem)?.Tag as string) ?? "";
                var metodeFilter = ((cmbMetodePembayaran.SelectedItem as ComboBoxItem)?.Tag as string) ?? "";
                var penggunaFilter = ((cmbPengguna.SelectedItem as ComboBoxItem)?.Tag as string) ?? "";
                var tanggalDari = dpTanggalDari.SelectedDate;
                var tanggalSampai = dpTanggalSampai.SelectedDate;
                
                decimal jumlahMin = 0;
                decimal jumlahMax = decimal.MaxValue;
                decimal.TryParse(txtJumlahMin.Text, out jumlahMin);
                if (!decimal.TryParse(txtJumlahMax.Text, out jumlahMax))
                {
                    jumlahMax = decimal.MaxValue;
                }

                var filtered = _allPembayaran.Where(p =>
                {
                    // Search filter (kode tiket, nama pengguna, metode)
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        bool matchSearch = 
                            (p.tiket?.kode_tiket?.ToLower().Contains(searchText) ?? false) ||
                            (p.tiket?.Pengguna?.nama?.ToLower().Contains(searchText) ?? false) ||
                            (p.metode_pembayaran?.ToLower().Contains(searchText) ?? false);
                        
                        if (!matchSearch) return false;
                    }

                    // Status filter
                    if (!string.IsNullOrEmpty(statusFilter) && p.status_bayar != statusFilter)
                    {
                        return false;
                    }

                    // Metode Pembayaran filter
                    if (!string.IsNullOrEmpty(metodeFilter) && p.metode_pembayaran != metodeFilter)
                    {
                        return false;
                    }

                    // Pengguna filter
                    if (!string.IsNullOrEmpty(penggunaFilter) && 
                        (p.tiket?.Pengguna?.nama != penggunaFilter))
                    {
                        return false;
                    }

                    // Tanggal filter
                    if (tanggalDari.HasValue && p.tanggal_bayar.Date < tanggalDari.Value.Date)
                    {
                        return false;
                    }
                    if (tanggalSampai.HasValue && p.tanggal_bayar.Date > tanggalSampai.Value.Date)
                    {
                        return false;
                    }

                    // Jumlah filter
                    if (p.jumlah_bayar < jumlahMin || p.jumlah_bayar > jumlahMax)
                    {
                        return false;
                    }

                    return true;
                }).ToList();

                _filteredPembayaran.Clear();
                foreach (var pembayaran in filtered)
                {
                    _filteredPembayaran.Add(pembayaran);
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminPembayaranPage] Error applying filters: {ex.Message}");
            }
        }

        private void UpdateSummary()
        {
            if (_filteredPembayaran == null) return;

            var total = _filteredPembayaran.Sum(p => p.jumlah_bayar);
            var sukses = _filteredPembayaran.Count(p => p.status_bayar == "Sukses");
            var menungguValidasi = _filteredPembayaran.Count(p => p.status_bayar == "Menunggu Validasi");
            var menungguPembayaran = _filteredPembayaran.Count(p => p.status_bayar == "Menunggu Pembayaran");
            var gagal = _filteredPembayaran.Count(p => p.status_bayar == "Gagal");

            txtSummary.Text = $"Menampilkan {_filteredPembayaran.Count} dari {_allPembayaran.Count} pembayaran " +
                             $"(Sukses: {sukses}, Validasi: {menungguValidasi}, Bayar: {menungguPembayaran}, Gagal: {gagal})";
            txtTotalPembayaran.Text = $"Total: Rp {total:N0}";
        }

        // Filter Event Handlers
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbMetodePembayaran_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbPengguna_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void TxtJumlah_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbStatus.SelectedIndex = 0;
            cmbMetodePembayaran.SelectedIndex = 0;
            cmbPengguna.SelectedIndex = 0;
            dpTanggalDari.SelectedDate = null;
            dpTanggalSampai.SelectedDate = null;
            txtJumlahMin.Text = "";
            txtJumlahMax.Text = "";
            
            ApplyFilters();
        }

        // Action Button Handlers
        private async void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int pembayaranId)
            {
                try
                {
                    var pembayaran = await _pembayaranService.GetPembayaranByIdAsync(pembayaranId);
                    if (pembayaran != null)
                    {
                        var detailWindow = new AdminPembayaranDetailWindow(pembayaran);
                        if (detailWindow.ShowDialog() == true)
                        {
                            // Refresh if any changes made
                            await LoadPembayaranDataAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading detail: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnValidasi_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int pembayaranId)
            {
                var result = MessageBox.Show(
                    "Validasi pembayaran ini?\n\n" +
                    "Tindakan ini akan:\n" +
                    "- Mengubah status pembayaran menjadi 'Sukses'\n" +
                    "- Mengaktifkan tiket terkait\n\n" +
                    "Lanjutkan?",
                    "Konfirmasi Validasi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var (success, message) = await _pembayaranService.ValidasiPembayaranAsync(pembayaranId);
                        
                        MessageBox.Show(message, success ? "Success" : "Error",
                            MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);

                        if (success)
                        {
                            await LoadPembayaranDataAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error validasi pembayaran: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void BtnTolak_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is int pembayaranId)
            {
                var result = MessageBox.Show(
                    "Tolak pembayaran ini?\n\n" +
                    "Tindakan ini akan:\n" +
                    "- Mengubah status pembayaran menjadi 'Gagal'\n" +
                    "- Membatalkan tiket terkait\n\n" +
                    "Lanjutkan?",
                    "Konfirmasi Tolak",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var (success, message) = await _pembayaranService.TolakPembayaranAsync(pembayaranId, "Ditolak oleh admin");
                        
                        MessageBox.Show(message, success ? "Success" : "Error",
                            MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);

                        if (success)
                        {
                            await LoadPembayaranDataAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error tolak pembayaran: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
