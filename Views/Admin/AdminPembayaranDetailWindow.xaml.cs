using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TiketLaut.Services;
using TiketLaut.Helpers;

namespace TiketLaut.Views
{
    public partial class AdminPembayaranDetailWindow : Window
    {
        private Pembayaran _pembayaran;

        public AdminPembayaranDetailWindow(Pembayaran pembayaran)
        {
            InitializeComponent();
            _pembayaran = pembayaran;
            ZoomHelper.EnableZoom(this);
            
            LoadData();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Gunakan helper untuk mengatur ukuran responsif
            WindowSizeHelper.SetDetailWindowSize(this);
        }

        private void LoadData()
        {
            try
            {
                // Informasi Pembayaran
                txtPembayaranId.Text = _pembayaran.pembayaran_id.ToString();
                txtMetode.Text = _pembayaran.metode_pembayaran;
                txtJumlah.Text = $"Rp {_pembayaran.jumlah_bayar:N0}";
                txtTanggal.Text = _pembayaran.tanggal_bayar.ToString("dd MMMM yyyy HH:mm", new CultureInfo("id-ID"));
                txtStatus.Text = _pembayaran.status_bayar;
                SetStatusColor(_pembayaran.status_bayar);

                // Informasi Tiket
                if (_pembayaran.tiket != null)
                {
                    txtKodeTiket.Text = _pembayaran.tiket.kode_tiket;
                    txtJumlahPenumpang.Text = _pembayaran.tiket.jumlah_penumpang.ToString();
                    txtTotalHarga.Text = $"Rp {_pembayaran.tiket.total_harga:N0}";
                    txtTanggalPesan.Text = _pembayaran.tiket.tanggal_pemesanan.ToString("dd MMMM yyyy HH:mm", new CultureInfo("id-ID"));
                    txtStatusTiket.Text = _pembayaran.tiket.status_tiket;
                    txtJenisKendaraan.Text = _pembayaran.tiket.jenis_kendaraan_enum;
                    txtPlatNomor.Text = _pembayaran.tiket.plat_nomor ?? "-";

                    // Informasi Pengguna
                    if (_pembayaran.tiket.Pengguna != null)
                    {
                        txtNamaPengguna.Text = _pembayaran.tiket.Pengguna.nama;
                        txtEmail.Text = _pembayaran.tiket.Pengguna.email;
                        txtTelepon.Text = _pembayaran.tiket.Pengguna.nomor_induk_kependudukan ?? "-";
                        txtAlamat.Text = _pembayaran.tiket.Pengguna.alamat ?? "-";
                    }

                    // Informasi Jadwal
                    if (_pembayaran.tiket.Jadwal != null)
                    {
                        txtPelabuhanAsal.Text = _pembayaran.tiket.Jadwal.pelabuhan_asal?.nama_pelabuhan ?? "-";
                        txtPelabuhanTujuan.Text = _pembayaran.tiket.Jadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "-";
                        txtWaktuBerangkat.Text = _pembayaran.tiket.Jadwal.waktu_berangkat.ToString("dd MMM yyyy HH:mm", new CultureInfo("id-ID"));
                        txtWaktuTiba.Text = _pembayaran.tiket.Jadwal.waktu_tiba.ToString("dd MMM yyyy HH:mm", new CultureInfo("id-ID"));
                        txtKapal.Text = _pembayaran.tiket.Jadwal.kapal?.nama_kapal ?? "-";
                        txtKelasLayanan.Text = _pembayaran.tiket.Jadwal.kelas_layanan ?? "-";
                    }

                    // Daftar Penumpang
                    if (_pembayaran.tiket.RincianPenumpangs != null && _pembayaran.tiket.RincianPenumpangs.Count > 0)
                    {
                        dgPenumpang.ItemsSource = _pembayaran.tiket.RincianPenumpangs;
                        dgPenumpang.Visibility = Visibility.Visible;
                        borderEmptyState.Visibility = Visibility.Collapsed;
                        
                        System.Diagnostics.Debug.WriteLine($"[AdminPembayaranDetailWindow] Loaded {_pembayaran.tiket.RincianPenumpangs.Count} penumpang");
                    }
                    else
                    {
                        dgPenumpang.Visibility = Visibility.Collapsed;
                        borderEmptyState.Visibility = Visibility.Visible;
                        
                        System.Diagnostics.Debug.WriteLine("[AdminPembayaranDetailWindow] No penumpang data found");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AdminPembayaranDetailWindow] Tiket is null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminPembayaranDetailWindow] Error loading data: {ex.Message}");
                MessageBox.Show($"Error loading detail data: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetStatusColor(string status)
        {
            switch (status)
            {
                case "Approved":
                case "Sukses":
                    borderStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4EDDA"));
                    txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#155724"));
                    break;
                case "Menunggu Validasi":
                    borderStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3CD"));
                    txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#856404"));
                    break;
                case "Rejected":
                case "Gagal":
                    borderStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8D7DA"));
                    txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#721C24"));
                    break;
                default:
                    borderStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E3E5"));
                    txtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#383D41"));
                    break;
            }
        }

        private void BtnLihatTiket_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_pembayaran.tiket != null)
                {
                    var tiketDetailWindow = new AdminTiketDetailWindow(_pembayaran.tiket.tiket_id);
                    tiketDetailWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening tiket detail: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_pembayaran == null)
                {
                    MessageBox.Show("Data pembayaran tidak tersedia.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Tampilkan dialog untuk edit status pembayaran
                var editDialog = new AdminPembayaranEditDialog(_pembayaran);
                if (editDialog.ShowDialog() == true)
                {
                    // Refresh data setelah edit
                    var pembayaranService = new PembayaranService();
                    var updatedPembayaran = await pembayaranService.GetPembayaranByIdAsync(_pembayaran.pembayaran_id);
                    
                    if (updatedPembayaran != null)
                    {
                        _pembayaran = updatedPembayaran;
                        LoadData(); // Reload UI
                        
                        MessageBox.Show("Data pembayaran berhasil diperbarui!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        DialogResult = true; // Notify parent to refresh
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminPembayaranDetailWindow] Error editing pembayaran: {ex.Message}");
                MessageBox.Show($"Error editing pembayaran: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    // Helper Converter for Row Numbers
    public class RowNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Controls.DataGridRow row)
            {
                return (row.GetIndex() + 1).ToString();
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
