using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminJadwalPage : UserControl
    {
        private readonly JadwalService _jadwalService;
        private ObservableCollection<JadwalViewModel> _jadwals = new ObservableCollection<JadwalViewModel>();

        public AdminJadwalPage()
        {
            InitializeComponent();
            _jadwalService = new JadwalService();
            LoadJadwalData();
        }

        private async void LoadJadwalData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AdminJadwalPage] Loading jadwal data...");
                
                var jadwals = await _jadwalService.GetAllJadwalAsync();
                _jadwals.Clear();
                
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] Received {jadwals.Count} jadwals from service");
                
                foreach (var jadwal in jadwals)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] Processing jadwal ID {jadwal.jadwal_id}");
                    System.Diagnostics.Debug.WriteLine($"  - Asal: {jadwal.pelabuhan_asal?.nama_pelabuhan ?? "NULL"}");
                    System.Diagnostics.Debug.WriteLine($"  - Tujuan: {jadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "NULL"}");
                    System.Diagnostics.Debug.WriteLine($"  - Kapal: {jadwal.kapal?.nama_kapal ?? "NULL"}");
                    System.Diagnostics.Debug.WriteLine($"  - Waktu Berangkat: {jadwal.waktu_berangkat}");
                    System.Diagnostics.Debug.WriteLine($"  - Waktu Tiba: {jadwal.waktu_tiba}");
                    
                    _jadwals.Add(new JadwalViewModel
                    {
                        jadwal_id = jadwal.jadwal_id,
                        pelabuhan_asal = jadwal.pelabuhan_asal,
                        pelabuhan_tujuan = jadwal.pelabuhan_tujuan,
                        kapal = jadwal.kapal,
                        waktu_berangkat = jadwal.waktu_berangkat,
                        waktu_tiba = jadwal.waktu_tiba,
                        kelas_layanan = jadwal.kelas_layanan,
                        status = jadwal.status,
                        IsSelected = false
                    });
                }
                
                dgJadwal.ItemsSource = _jadwals;
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] DataGrid bound with {_jadwals.Count} items");
                
                if (_jadwals.Count == 0)
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AdminJadwalPage] StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Error loading data: {ex.Message}\n\nCek Debug Output untuk detail lengkap.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddJadwal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AdminJadwalFormDialog();
            if (dialog.ShowDialog() == true)
            {
                LoadJadwalData();
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
                        LoadJadwalData();
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
                        LoadJadwalData();
                    }
                }
            }
        }

        private async void BtnBulkDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedJadwals = _jadwals.Where(j => j.IsSelected).ToList();
            
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
                var (success, message, count) = await _jadwalService.BulkDeleteJadwalAsync(jadwalIds);
                
                MessageBox.Show(message, success ? "Success" : "Error",
                    MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);

                if (success)
                {
                    LoadJadwalData();
                    chkSelectAll.IsChecked = false;
                }
            }
        }

        private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            var isChecked = chkSelectAll.IsChecked ?? false;
            foreach (var jadwal in _jadwals)
            {
                jadwal.IsSelected = isChecked;
            }
            dgJadwal.Items.Refresh();
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
