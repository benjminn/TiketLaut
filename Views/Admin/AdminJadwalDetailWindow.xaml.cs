using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TiketLaut.Services;
using TiketLaut.Helpers;
using TiketLaut.Views.Components;

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Gunakan helper untuk window besar (jadwal detail menampilkan banyak data)
            WindowSizeHelper.SetLargeWindowSize(this);
        }

        private async void LoadData()
        {
            try
            {
                // Load jadwal detail
                _currentJadwal = await _jadwalService.GetJadwalByIdAsync(_jadwalId);
                if (_currentJadwal == null)
                {
                    CustomDialog.ShowError("Error", "Jadwal tidak ditemukan!");
                    this.Close();
                    return;
                }

                DisplayJadwalInfo();
                await LoadTiketsAsync();
                await LoadSimilarSchedulesAsync();
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Error loading data: {ex.Message}");
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
                    // Status "Booked" atau "Menunggu Pembayaran"
                    filteredTikets = _allTikets.Where(t => 
                        t.status_tiket == "Booked" || 
                        t.status_tiket == "Menunggu Pembayaran");
                    break;
                case "Aktif":
                    // Status "Paid" atau "Aktif"
                    filteredTikets = _allTikets.Where(t => 
                        t.status_tiket == "Paid" || 
                        t.status_tiket == "Aktif");
                    break;
                case "Gagal":
                    // Status "Cancelled" atau "Gagal"
                    filteredTikets = _allTikets.Where(t => 
                        t.status_tiket == "Cancelled" || 
                        t.status_tiket == "Gagal");
                    break;
            }

            // Map to display items with KeteranganGolongan
            var displayItems = filteredTikets.Select(t =>
            {
                // Parse enum dari string
                var jenisKendaraanEnum = Enum.TryParse<JenisKendaraan>(t.jenis_kendaraan_enum, out var parsedEnum)
                    ? parsedEnum
                    : JenisKendaraan.Jalan_Kaki;

                return new TiketDisplayItem
                {
                    tiket_id = t.tiket_id,
                    kode_tiket = t.kode_tiket,
                    Pengguna = t.Pengguna,
                    jumlah_penumpang = t.jumlah_penumpang,
                    jenis_kendaraan_enum = jenisKendaraanEnum,
                    KeteranganGolongan = GetKeteranganGolongan(jenisKendaraanEnum),
                    plat_nomor = t.plat_nomor,
                    total_harga = t.total_harga,
                    tanggal_pemesanan = t.tanggal_pemesanan,
                    status_tiket = t.status_tiket
                };
            }).ToList();

            dgTikets.ItemsSource = displayItems;

            // Update summary
            txtTotalTiket.Text = filteredTikets.Count().ToString();
            var totalPendapatan = filteredTikets
                .Where(t => t.status_tiket == "Paid" || t.status_tiket == "Aktif")
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
                CustomDialog.ShowInfo(
                    "Info",
                    "Tidak ada jadwal serupa ditemukan untuk rute dan tanggal yang sama."
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

        private void BtnFilterAktif_Click(object sender, RoutedEventArgs e)
        {
            SetActiveFilter(btnFilterAktif);
            ApplyTiketFilter("Aktif");
        }

        private void BtnFilterGagal_Click(object sender, RoutedEventArgs e)
        {
            SetActiveFilter(btnFilterGagal);
            ApplyTiketFilter("Gagal");
        }

        private void SetActiveFilter(Button activeButton)
        {
            // Reset all buttons
            btnFilterAll.Tag = null;
            btnFilterPending.Tag = null;
            btnFilterAktif.Tag = null;
            btnFilterGagal.Tag = null;

            // Set active button
            activeButton.Tag = "Active";
        }

        private void DgTikets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgTikets.SelectedItem is TiketDisplayItem selectedItem)
            {
                ShowTiketDetail(selectedItem.tiket_id);
            }
        }

        private void ShowTiketDetail(int tiketId)
        {
            // Open new detail window instead of MessageBox
            var detailWindow = new AdminTiketDetailWindow(tiketId);
            detailWindow.ShowDialog();
        }

        private string GetKeteranganGolongan(JenisKendaraan jenisKendaraan)
        {
            return jenisKendaraan switch
            {
                JenisKendaraan.Jalan_Kaki => "Golongan 0 (Pejalan Kaki)",
                JenisKendaraan.Golongan_I => "Golongan I (Sepeda)",
                JenisKendaraan.Golongan_II => "Golongan II (Motor <500cc)",
                JenisKendaraan.Golongan_III => "Golongan III (Motor >500cc)",
                JenisKendaraan.Golongan_IV_A => "Golongan IV-A (Sedan/Minibus)",
                JenisKendaraan.Golongan_IV_B => "Golongan IV-B (Mobil Bak)",
                JenisKendaraan.Golongan_V_A => "Golongan V-A (Bus 5-7m)",
                JenisKendaraan.Golongan_V_B => "Golongan V-B (Truk 5-7m)",
                JenisKendaraan.Golongan_VI_A => "Golongan VI-A (Bus 7-10m)",
                JenisKendaraan.Golongan_VI_B => "Golongan VI-B (Truk 7-10m)",
                JenisKendaraan.Golongan_VII => "Golongan VII (Tronton 10-12m)",
                JenisKendaraan.Golongan_VIII => "Golongan VIII (Alat Berat 12-16m)",
                JenisKendaraan.Golongan_IX => "Golongan IX (Alat Berat >16m)",
                _ => "-"
            };
        }

        private void DgSimilarSchedules_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgSimilarSchedules.SelectedItem is Jadwal selectedJadwal)
            {
                // Open detail window for the selected jadwal as a dialog (modal)
                // This way the current window stays open in the background
                var detailWindow = new AdminJadwalDetailWindow(selectedJadwal.jadwal_id);
                detailWindow.Owner = this; // Set owner so it appears on top
                detailWindow.ShowDialog(); // Use ShowDialog instead of Show to make it modal
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnEditJadwal_Click(object sender, RoutedEventArgs e)
        {
            if (_currentJadwal == null) return;

            try
            {
                // Open edit dialog
                var formDialog = new AdminJadwalFormDialog(_currentJadwal);
                if (formDialog.ShowDialog() == true)
                {
                    // Reload data after successful edit
                    LoadData();
                    CustomDialog.ShowSuccess("Success", "Jadwal berhasil diupdate!");
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Error: {ex.Message}");
            }
        }

        private async void BtnDeleteJadwal_Click(object sender, RoutedEventArgs e)
        {
            if (_currentJadwal == null) return;

            var result = CustomDialog.ShowQuestion(
                "Konfirmasi Hapus",
                $"Apakah Anda yakin ingin menghapus jadwal ini?\n\n" +
                $"Rute: {_currentJadwal.pelabuhan_asal?.nama_pelabuhan} → {_currentJadwal.pelabuhan_tujuan?.nama_pelabuhan}\n" +
                $"Tanggal: {_currentJadwal.waktu_berangkat:dd MMM yyyy}\n" +
                $"Waktu: {_currentJadwal.waktu_berangkat:HH:mm}\n\n" +
                $"Jadwal yang dihapus tidak dapat dikembalikan!"
            );

            if (result == true)
            {
                try
                {
                    var deleteResult = await _jadwalService.DeleteJadwalAsync(_jadwalId);
                    
                    if (deleteResult.success)
                    {
                        CustomDialog.ShowSuccess("Success", "Jadwal berhasil dihapus!");
                        
                        // Close this window and notify parent to refresh
                        DialogResult = true;
                        this.Close();
                    }
                    else
                    {
                        CustomDialog.ShowError("Error", $"Gagal menghapus jadwal: {deleteResult.message}");
                    }
                }
                catch (Exception ex)
                {
                    CustomDialog.ShowError("Error", $"Error: {ex.Message}");
                }
            }
        }
    }

    // Helper class for displaying tiket with keterangan golongan
    public class TiketDisplayItem
    {
        public int tiket_id { get; set; }
        public string? kode_tiket { get; set; }
        public Pengguna? Pengguna { get; set; }
        public int jumlah_penumpang { get; set; }
        public JenisKendaraan jenis_kendaraan_enum { get; set; }
        public string? KeteranganGolongan { get; set; }
        public string? plat_nomor { get; set; }
        public decimal total_harga { get; set; }
        public DateTime? tanggal_pemesanan { get; set; }
        public string? status_tiket { get; set; }
    }
}
