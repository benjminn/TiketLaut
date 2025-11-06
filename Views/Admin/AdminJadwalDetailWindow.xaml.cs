using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminJadwalDetailWindow : Window
    {
        private readonly JadwalService _jadwalService;
        private readonly int _jadwalId;
        private Jadwal? _currentJadwal;
        private List<Tiket> _allTikets = new List<Tiket>();
        private List<Jadwal> _similarSchedules = new List<Jadwal>();

        public AdminJadwalDetailWindow(int jadwalId)
        {
            InitializeComponent();
            _jadwalService = new JadwalService();
            _jadwalId = jadwalId;
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                // Load jadwal detail
                _currentJadwal = await _jadwalService.GetJadwalByIdAsync(_jadwalId);
                if (_currentJadwal == null)
                {
                    MessageBox.Show("Jadwal tidak ditemukan!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                DisplayJadwalInfo();
                await LoadTiketsAsync();
                await LoadSimilarSchedulesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayJadwalInfo()
        {
            if (_currentJadwal == null) return;

            // Convert UTC to local time
            var waktuBerangkatLocal = _currentJadwal.waktu_berangkat.Kind == DateTimeKind.Utc 
                ? _currentJadwal.waktu_berangkat.ToLocalTime() 
                : _currentJadwal.waktu_berangkat;
            var waktuTibaLocal = _currentJadwal.waktu_tiba.Kind == DateTimeKind.Utc 
                ? _currentJadwal.waktu_tiba.ToLocalTime() 
                : _currentJadwal.waktu_tiba;

            // Calculate duration
            var duration = _currentJadwal.waktu_tiba - _currentJadwal.waktu_berangkat;

            // Display data
            txtJadwalId.Text = _currentJadwal.jadwal_id.ToString();
            txtRute.Text = $"{_currentJadwal.pelabuhan_asal?.nama_pelabuhan ?? "-"} → {_currentJadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "-"}";
            
            txtPelabuhanAsal.Text = _currentJadwal.pelabuhan_asal?.nama_pelabuhan ?? "-";
            txtLokasiAsal.Text = $"{_currentJadwal.pelabuhan_asal?.kota ?? "-"}, {_currentJadwal.pelabuhan_asal?.provinsi ?? "-"}";
            
            txtPelabuhanTujuan.Text = _currentJadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "-";
            txtLokasiTujuan.Text = $"{_currentJadwal.pelabuhan_tujuan?.kota ?? "-"}, {_currentJadwal.pelabuhan_tujuan?.provinsi ?? "-"}";
            
            txtTanggal.Text = waktuBerangkatLocal.ToString("dddd, dd MMMM yyyy");
            txtWaktuBerangkat.Text = waktuBerangkatLocal.ToString("HH:mm");
            txtWaktuTiba.Text = waktuTibaLocal.ToString("HH:mm");
            txtDurasi.Text = $"{(int)duration.TotalHours}j {duration.Minutes}m";
            
            txtKapal.Text = _currentJadwal.kapal?.nama_kapal ?? "-";
            txtKelas.Text = _currentJadwal.kelas_layanan;
            txtKapasitasPenumpang.Text = $"{_currentJadwal.kapal?.kapasitas_penumpang_max ?? 0} orang";
            txtKapasitasKendaraan.Text = $"{_currentJadwal.kapal?.kapasitas_kendaraan_max ?? 0} unit";
            
            txtSisaPenumpang.Text = $"{_currentJadwal.sisa_kapasitas_penumpang} orang";
            txtSisaKendaraan.Text = $"{_currentJadwal.sisa_kapasitas_kendaraan} unit";
            
            // Status badge
            txtStatus.Text = _currentJadwal.status;
            if (_currentJadwal.status == "Active")
            {
                borderStatus.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218)); // #D4EDDA
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36)); // #155724
            }
            else
            {
                borderStatus.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218)); // #F8D7DA
                txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36)); // #721C24
            }
        }

        private async System.Threading.Tasks.Task LoadTiketsAsync()
        {
            if (_currentJadwal == null) return;

            // Load all tickets for this jadwal
            _allTikets = await _jadwalService.GetTiketsByJadwalIdAsync(_jadwalId);
            
            // Apply filter (default: all)
            ApplyTiketFilter("All");
        }

        private void ApplyTiketFilter(string filter)
        {
            IEnumerable<Tiket> filteredTikets = _allTikets;

            switch (filter)
            {
                case "Pending":
                    filteredTikets = _allTikets.Where(t => t.status_tiket == "Booked");
                    break;
                case "Paid":
                    filteredTikets = _allTikets.Where(t => t.status_tiket == "Paid");
                    break;
                case "Cancelled":
                    filteredTikets = _allTikets.Where(t => t.status_tiket == "Cancelled");
                    break;
            }

            dgTikets.ItemsSource = filteredTikets.ToList();

            // Update summary
            txtTotalTiket.Text = filteredTikets.Count().ToString();
            var totalPendapatan = filteredTikets
                .Where(t => t.status_tiket == "Paid")
                .Sum(t => t.total_harga);
            txtTotalPendapatan.Text = $"Rp {totalPendapatan:N0}";
        }

        private async System.Threading.Tasks.Task LoadSimilarSchedulesAsync()
        {
            if (_currentJadwal == null) return;

            // Get jadwals with same route and date
            var allJadwals = await _jadwalService.GetAllJadwalAsync();
            
            var waktuBerangkatLocal = _currentJadwal.waktu_berangkat.Kind == DateTimeKind.Utc 
                ? _currentJadwal.waktu_berangkat.ToLocalTime() 
                : _currentJadwal.waktu_berangkat;

            _similarSchedules = allJadwals.Where(j => 
                j.jadwal_id != _jadwalId && // Exclude current jadwal
                j.pelabuhan_asal_id == _currentJadwal.pelabuhan_asal_id &&
                j.pelabuhan_tujuan_id == _currentJadwal.pelabuhan_tujuan_id &&
                (j.waktu_berangkat.Kind == DateTimeKind.Utc ? j.waktu_berangkat.ToLocalTime() : j.waktu_berangkat).Date == waktuBerangkatLocal.Date
            ).OrderBy(j => j.waktu_berangkat).ToList();

            dgSimilarSchedules.ItemsSource = _similarSchedules;

            if (_similarSchedules.Count == 0)
            {
                MessageBox.Show(
                    "Tidak ada jadwal serupa ditemukan untuk rute dan tanggal yang sama.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        // Filter button click handlers
        private void BtnFilterAll_Click(object sender, RoutedEventArgs e)
        {
            SetActiveFilter(btnFilterAll);
            ApplyTiketFilter("All");
        }

        private void BtnFilterPending_Click(object sender, RoutedEventArgs e)
        {
            SetActiveFilter(btnFilterPending);
            ApplyTiketFilter("Pending");
        }

        private void BtnFilterPaid_Click(object sender, RoutedEventArgs e)
        {
            SetActiveFilter(btnFilterPaid);
            ApplyTiketFilter("Paid");
        }

        private void BtnFilterCancelled_Click(object sender, RoutedEventArgs e)
        {
            SetActiveFilter(btnFilterCancelled);
            ApplyTiketFilter("Cancelled");
        }

        private void SetActiveFilter(Button activeButton)
        {
            // Reset all buttons
            btnFilterAll.Tag = null;
            btnFilterPending.Tag = null;
            btnFilterPaid.Tag = null;
            btnFilterCancelled.Tag = null;

            // Set active button
            activeButton.Tag = "Active";
        }

        private void DgSimilarSchedules_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgSimilarSchedules.SelectedItem is Jadwal selectedJadwal)
            {
                // Open detail window for the selected jadwal
                var detailWindow = new AdminJadwalDetailWindow(selectedJadwal.jadwal_id);
                detailWindow.Show();
                this.Close();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
