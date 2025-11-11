using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TiketLaut.Services;
using System.Threading.Tasks;
using TiketLaut.Models;
using System.ComponentModel;
using TiketLaut.Views.Components;


namespace TiketLaut.Views
{
    public partial class AdminTiketPage : UserControl
    {
        private ObservableCollection<TiketViewModel> _allTikets;
        private ObservableCollection<TiketViewModel> _filteredTikets;
        private readonly TiketService _tiketService;
        private readonly JadwalService _jadwalService;
        private readonly PenggunaService _penggunaService;
        private readonly RiwayatService _riwayatService;

        // Pagination variables
        private List<Tiket> _allTiketsData = new List<Tiket>(); // Store all data from DB
        private int _currentPage = 1;
        private const int _pageSize = 30;
        private int _totalRecords = 0;

        public AdminTiketPage()
        {
            InitializeComponent();
            _tiketService = new TiketService();
            _jadwalService = new JadwalService();
            _penggunaService = new PenggunaService();
            _riwayatService = new RiwayatService();
            _allTikets = new ObservableCollection<TiketViewModel>();
            _filteredTikets = new ObservableCollection<TiketViewModel>();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                // Auto-update status tiket dan jadwal yang sudah selesai
                System.Diagnostics.Debug.WriteLine("[AdminTiketPage] Calling AutoUpdate...");
                try
                {
                    var updateCount = await _riwayatService.AutoUpdatePembayaranSelesaiAsync();
                    System.Diagnostics.Debug.WriteLine($"[AdminTiketPage] AutoUpdate completed: {updateCount} records updated");
                    
                    // Show result with CustomDialog
                    if (updateCount > 0)
                    {
                        var dialog = new CustomDialog(
                            "Auto-Update Berhasil",
                            $"Auto-update berhasil!\n{updateCount} record diupdate.",
                            CustomDialog.DialogType.Success
                        );
                        dialog.Owner = Window.GetWindow(this);
                        dialog.ShowDialog();
                    }
                    else
                    {
                        var dialog = new CustomDialog(
                            "Auto-Update",
                            "Auto-update jalan tapi tidak ada record yang diupdate.\nSemua status sudah benar.",
                            CustomDialog.DialogType.Info
                        );
                        dialog.Owner = Window.GetWindow(this);
                        dialog.ShowDialog();
                    }
                }
                catch (Exception exAuto)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdminTiketPage] AutoUpdate ERROR: {exAuto.Message}");
                    System.Diagnostics.Debug.WriteLine($"[AdminTiketPage] StackTrace: {exAuto.StackTrace}");
                    
                    var dialog = new CustomDialog(
                        "Auto-Update Error",
                        $"Error running auto-update:\n{exAuto.Message}",
                        CustomDialog.DialogType.Error
                    );
                    dialog.Owner = Window.GetWindow(this);
                    dialog.ShowDialog();
                }
                var tikets = await _tiketService.GetAllTiketsAsync();
                _allTiketsData = tikets.OrderByDescending(t => t.tiket_id).ToList();
                _totalRecords = _allTiketsData.Count;
                
                // Reset to page 1
                _currentPage = 1;
                LoadPageData();

                // Load schedules for filter
                var jadwals = await _jadwalService.GetAllAsync();
                cmbJadwal.Items.Add(new { Id = 0, Text = "-- Semua Jadwal --" });
                foreach (var jadwal in jadwals)
                {
                    cmbJadwal.Items.Add(new
                    {
                        Id = jadwal.jadwal_id,
                        Text = $"{jadwal.pelabuhan_asal?.nama_pelabuhan} → {jadwal.pelabuhan_tujuan?.nama_pelabuhan} ({jadwal.waktu_berangkat:dd/MM HH:mm})"
                    });
                }
                cmbJadwal.SelectedIndex = 0;
                cmbStatus.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error loading data", $"Error loading data: {ex.Message}");
            }
        }

        private void LoadPageData()
        {
            _allTikets.Clear();
            
            // Calculate pagination
            int skip = (_currentPage - 1) * _pageSize;
            var pagedData = _allTiketsData.Skip(skip).Take(_pageSize).ToList();
            
            foreach (var tiket in pagedData)
            {
                _allTikets.Add(new TiketViewModel { Tiket = tiket, IsSelected = false });
            }

            _filteredTikets = new ObservableCollection<TiketViewModel>(_allTikets);
            dgTiket.ItemsSource = _filteredTikets;
            
            UpdatePaginationUI();
        }

        private void UpdatePaginationUI()
        {
            int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
            int displayedStart = (_currentPage - 1) * _pageSize + 1;
            int displayedEnd = Math.Min(_currentPage * _pageSize, _totalRecords);
            
            txtPageNumber.Text = _currentPage.ToString();
            txtPaginationInfo.Text = $"Page {_currentPage} - Menampilkan {displayedStart}-{displayedEnd} dari {_totalRecords} tiket";
            txtTotalRecords.Text = $"Total: {_totalRecords} tiket";
            
            // Enable/Disable navigation buttons
            btnPrevPage.IsEnabled = _currentPage > 1;
            btnNextPage.IsEnabled = _currentPage < totalPages;
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadPageData();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                LoadPageData();
            }
        }
        private void ApplyFilters()
        {
            var filtered = _allTikets.AsEnumerable();

            // Filter by Kode Tiket
            if (!string.IsNullOrWhiteSpace(txtSearchKodeTiket.Text))
            {
                var search = txtSearchKodeTiket.Text.ToLower();
                filtered = filtered.Where(t => t.Tiket.kode_tiket?.ToLower().Contains(search) ?? false);
            }

            // Filter by Jadwal
            if (cmbJadwal.SelectedValue != null)
            {
                var selectedJadwalId = (int)((dynamic)cmbJadwal.SelectedItem).Id;
                if (selectedJadwalId > 0)
                {
                    filtered = filtered.Where(t => t.Tiket.jadwal_id == selectedJadwalId);
                }
            }

            // Filter by Status
            var selectedStatus = cmbStatus.SelectedItem as ComboBoxItem;
            if (selectedStatus != null && selectedStatus.Content.ToString() != "-- Semua Status --")
            {
                filtered = filtered.Where(t => t.Tiket.status_tiket == selectedStatus.Content.ToString());
            }

            // Filter by Tanggal
            if (dpTanggal.SelectedDate.HasValue)
            {
                var selectedDate = dpTanggal.SelectedDate.Value.Date;
                filtered = filtered.Where(t => t.Tiket.tanggal_pemesanan.Date == selectedDate);
            }

            _filteredTikets.Clear();
            foreach (var item in filtered)
            {
                _filteredTikets.Add(item);
            }

            UpdateDataGridVisibility();
        }

        private void UpdateDataGridVisibility()
        {
            if (_filteredTikets.Count == 0)
            {
                dgTiket.Visibility = Visibility.Collapsed;
                txtNoData.Visibility = Visibility.Visible;
            }
            else
            {
                dgTiket.Visibility = Visibility.Visible;
                txtNoData.Visibility = Visibility.Collapsed;
            }
        }


        private void TxtSearchKodeTiket_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void CmbJadwal_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void DpTanggal_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();


        private void BtnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            txtSearchKodeTiket.Text = "";
            cmbJadwal.SelectedIndex = 0;
            cmbStatus.SelectedIndex = 0;
            dpTanggal.SelectedDate = null;
            ApplyFilters();
        }

        private void DgTiket_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgTiket.SelectedItem is TiketViewModel viewModel)
            {
                ShowDetailWindow(viewModel.Tiket.tiket_id);
            }
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TiketViewModel viewModel)
            {
                ShowDetailWindow(viewModel.Tiket.tiket_id);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgTiket.SelectedItem is TiketViewModel viewModel)
            {
                var editDialog = new AdminTiketFormDialog(viewModel.Tiket.tiket_id);
                if (editDialog.ShowDialog() == true)
                {
                    LoadData();
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgTiket.SelectedItem is TiketViewModel viewModel)
            {
                var result = CustomDialog.ShowQuestion(
                    $"Hapus tiket {viewModel.Tiket.kode_tiket}?",
                    "Konfirmasi");

                if (result == true)
                {
                    try
                    {
                        await _tiketService.DeleteTiketAsync(viewModel.Tiket.tiket_id);
                        _allTikets.Remove(viewModel);
                        _filteredTikets.Remove(viewModel);
                        CustomDialog.ShowSuccess("Tiket berhasil dihapus", "Sukses");
                    }
                    catch (Exception ex)
                    {
                        CustomDialog.ShowError($"Gagal menghapus tiket: {ex.Message}", "Error");
                    }
                }
            }
        }

        public async Task DeleteTiketAsync(int tiketId)
        {
            try
            {
                await _tiketService.DeleteTiketAsync(tiketId);

                var tiketToRemove = _allTikets.FirstOrDefault(t => t.Tiket.tiket_id == tiketId);
                if (tiketToRemove != null)
                {
                    _allTikets.Remove(tiketToRemove);
                    _filteredTikets.Remove(tiketToRemove);
                }

                CustomDialog.ShowSuccess("Tiket berhasil dihapus.", "Success");
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError($"Error deleting tiket: {ex.Message}", "Error");
            }
        }

        private void ShowDetailWindow(int tiketId)
        {
            var detailWindow = new AdminTiketDetailWindow(tiketId);
            detailWindow.ShowDialog();
        }

        private void ShowEditDialog(int tiketId)
        {
            var editDialog = new AdminTiketFormDialog(tiketId);
            if (editDialog.ShowDialog() == true)
            {
                LoadData(); // Refresh data
            }
        }

        public static Color GetStatusColor(string status)

        {
            return status switch
            {
                "Menunggu Pembayaran" => (Color)ColorConverter.ConvertFromString("#FFA500"), // Orange
                "Aktif" => (Color)ColorConverter.ConvertFromString("#28A745"), // Green
                "Selesai" => (Color)ColorConverter.ConvertFromString("#17A2B8"), // Cyan/Info
                "Gagal" => (Color)ColorConverter.ConvertFromString("#DC3545"), // Red
                _ => (Color)ColorConverter.ConvertFromString("#6C757D") // Gray

            };
        }

    }

    // ViewModel untuk DataGrid
    public class TiketViewModel
    {
        public required Tiket Tiket { get; set; }
        public bool IsSelected { get; set; }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? status = value as string;
            if (string.IsNullOrEmpty(status))
            {
                // Jika status null atau kosong, gunakan warna default
                return new SolidColorBrush(AdminTiketPage.GetStatusColor(""));
            }

            // Memanggil method statis yang sudah Anda buat!
            Color color = AdminTiketPage.GetStatusColor(status);
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Tidak perlu diimplementasikan untuk case ini
            throw new NotImplementedException();
        }
    }
}
