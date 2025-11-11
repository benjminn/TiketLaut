using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TiketLaut.Services;
using TiketLaut.Helpers;
using TiketLaut.Views.Components;

namespace TiketLaut.Views
{
    public partial class AdminTiketDetailWindow : Window
    {
        private readonly int _tiketId;
        private Tiket? _tiket;
        private readonly TiketService _tiketService;

        public AdminTiketDetailWindow(int tiketId)
        {
            InitializeComponent();
            _tiketId = tiketId;
            _tiketService = new TiketService();
            ZoomHelper.EnableZoom(this);
            
            LoadTiketDetail();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Gunakan helper untuk mengatur ukuran responsif
            WindowSizeHelper.SetDetailWindowSize(this);
        }

        private async void LoadTiketDetail()
        {
            try
            {
                var tiketService = new TiketService();
                _tiket = await tiketService.GetTiketByIdAsync(_tiketId);

                if (_tiket != null)
                {
                    // Informasi Tiket
                    txtKodeTiket.Text = _tiket.kode_tiket ?? "-";
                    txtStatus.Text = _tiket.status_tiket ?? "-";
                    SetStatusColor(_tiket.status_tiket ?? "Unknown");
                    txtTanggalPemesanan.Text = _tiket.tanggal_pemesanan.ToString("dd MMMM yyyy HH:mm");
                    txtTotalHarga.Text = $"Rp {_tiket.total_harga:N0}";
                    var pembayaran = _tiket.Pembayarans?.OrderByDescending(p => p.tanggal_bayar).FirstOrDefault();
                    txtMetodePembayaran.Text = pembayaran?.metode_pembayaran ?? "Belum dibayar";

                    // Show/Hide Verifikasi Pembayaran button
                    if (_tiket.status_tiket == "Menunggu Pembayaran" || _tiket.status_tiket == "Booked")
                    {
                        btnVerifikasiPembayaran.Visibility = Visibility.Visible;
                    }

                    // Informasi Pembeli
                    if (_tiket.Pengguna != null)
                    {
                        txtNamaPembeli.Text = _tiket.Pengguna.nama ?? "-";
                        txtNIK.Text = _tiket.Pengguna.nomor_induk_kependudukan ?? "-";
                        txtEmail.Text = _tiket.Pengguna.email ?? "-";
                        // Note: Pengguna hanya punya NIK, belum ada field telepon terpisah
                        txtTelepon.Text = _tiket.Pengguna.nomor_induk_kependudukan ?? "-";
                    }

                    // Detail Perjalanan
                    txtJumlahPenumpang.Text = $"{_tiket.jumlah_penumpang} orang";

                    // Golongan Kendaraan dengan keterangan
                    if (Enum.TryParse<JenisKendaraan>(_tiket.jenis_kendaraan_enum, out var jenisKendaraanEnum))
                    {
                        var golongan = jenisKendaraanEnum.ToString().Replace("_", " ");
                        txtGolonganKendaraan.Text = golongan;
                        txtKeteranganGolongan.Text = GetKeteranganGolongan(jenisKendaraanEnum);
                    }
                    else
                    {
                        txtGolonganKendaraan.Text = _tiket.jenis_kendaraan_enum;
                        txtKeteranganGolongan.Text = "";
                    }

                    txtPlatNomor.Text = _tiket.plat_nomor ?? "-";

                    // Rute (load from jadwal)
                    if (_tiket.Jadwal != null)
                    {
                        var asalNama = _tiket.Jadwal.pelabuhan_asal?.nama_pelabuhan ?? "Unknown";
                        var tujuanNama = _tiket.Jadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "Unknown";
                        txtRute.Text = $"{asalNama} → {tujuanNama}";
                    }
                }
            }
            catch (Exception ex)
            {
                var dialog = new CustomDialog(
                    "Error",
                    $"Error loading tiket detail: {ex.Message}",
                    CustomDialog.DialogType.Error
                );
                dialog.ShowDialog();
            }
        }

        private void SetStatusColor(string status)
        {
            switch (status)
            {
                case "Menunggu Pembayaran":
                case "Booked":
                    borderStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107")); // Yellow
                    break;
                case "Paid":
                case "Aktif":
                    borderStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745")); // Green
                    break;
                case "Cancelled":
                case "Gagal":
                    borderStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")); // Red
                    break;
                default:
                    borderStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C757D")); // Gray
                    break;
            }
        }

        private string GetKeteranganGolongan(JenisKendaraan jenisKendaraan)
        {
            return jenisKendaraan switch
            {
                JenisKendaraan.Jalan_Kaki => "(Pejalan Kaki)",
                JenisKendaraan.Golongan_I => "(Sepeda)",
                JenisKendaraan.Golongan_II => "(Motor <500cc)",
                JenisKendaraan.Golongan_III => "(Motor >500cc / Roda 3)",
                JenisKendaraan.Golongan_IV_A => "(Mobil Sedan/Minibus ≤5m)",
                JenisKendaraan.Golongan_IV_B => "(Mobil Bak ≤5m)",
                JenisKendaraan.Golongan_V_A => "(Bus 5-7m)",
                JenisKendaraan.Golongan_V_B => "(Truk 5-7m)",
                JenisKendaraan.Golongan_VI_A => "(Bus 7-10m)",
                JenisKendaraan.Golongan_VI_B => "(Truk 7-10m)",
                JenisKendaraan.Golongan_VII => "(Truk Tronton 10-12m)",
                JenisKendaraan.Golongan_VIII => "(Truk/Alat Berat 12-16m)",
                JenisKendaraan.Golongan_IX => "(Truk/Alat Berat >16m)",
                _ => ""
            };
        }

        private void BtnVerifikasiPembayaran_Click(object sender, RoutedEventArgs e)
        {
            if (_tiket == null) return;

            try
            {
                var pembayaran = _tiket.Pembayarans?.OrderByDescending(p => p.tanggal_bayar).FirstOrDefault();

                if (pembayaran == null)
                {
                    var dialog = new CustomDialog(
                        "Pembayaran Tidak Ditemukan",
                        $"Tiket: {_tiket.kode_tiket}\n\n" +
                        "Tidak ada data pembayaran untuk tiket ini.\n\n" +
                        "Silakan tambahkan pembayaran terlebih dahulu di menu 'Kelola Pembayaran'.",
                        CustomDialog.DialogType.Info
                    );
                    dialog.ShowDialog();
                }
                else
                {
                    // Show payment detail with confirmation
                    var confirmDialog = new CustomDialog(
                        "Detail Pembayaran",
                        $"Tiket: {_tiket.kode_tiket}\n" +
                        $"Metode Pembayaran: {pembayaran.metode_pembayaran}\n" +
                        $"Jumlah: Rp {pembayaran.jumlah_bayar:N0}\n" +
                        $"Status: {pembayaran.status_bayar}\n" +
                        $"Tanggal: {pembayaran.tanggal_bayar:dd MMMM yyyy HH:mm}\n\n" +
                        "Buka detail pembayaran di 'Kelola Pembayaran'?",
                        CustomDialog.DialogType.Info,
                        CustomDialog.DialogButtons.YesNo
                    );

                    if (confirmDialog.ShowDialog() == true)
                    {
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                var dialog = new CustomDialog(
                    "Error",
                    $"Error: {ex.Message}",
                    CustomDialog.DialogType.Error
                );
                dialog.ShowDialog();
            }
        }

        private void BtnDetailJadwal_Click(object sender, RoutedEventArgs e)
        {
            if (_tiket == null || _tiket.jadwal_id == 0)
            {
                var dialog = new CustomDialog(
                    "Error",
                    "Data jadwal tidak ditemukan untuk tiket ini.",
                    CustomDialog.DialogType.Error
                );
                dialog.ShowDialog();
                return;
            }

            var jadwalWindow = new AdminJadwalDetailWindow(_tiket.jadwal_id);
            jadwalWindow.Owner = this;
            jadwalWindow.ShowDialog();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
