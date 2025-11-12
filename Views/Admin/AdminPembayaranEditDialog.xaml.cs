using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;
using TiketLaut.Helpers;

namespace TiketLaut.Views
{
    public partial class AdminPembayaranEditDialog : Window
    {
        private readonly Pembayaran _pembayaran;
        private readonly PembayaranService _pembayaranService;

        public AdminPembayaranEditDialog(Pembayaran pembayaran)
        {
            InitializeComponent();
            _pembayaran = pembayaran;
            _pembayaranService = new PembayaranService();
            
            LoadData();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Gunakan helper untuk mengatur ukuran responsif (dialog sedang)
            WindowSizeHelper.SetFormDialogSize(this);
        }

        private void LoadData()
        {
            try
            {
                // Load read-only information
                txtKodeTiket.Text = _pembayaran.tiket?.kode_tiket ?? "-";
                txtNamaPengguna.Text = _pembayaran.tiket?.Pengguna?.nama ?? "-";
                txtTanggalBayar.Text = _pembayaran.tanggal_bayar.ToString("dd MMMM yyyy HH:mm");
                txtJumlahBayar.Text = $"Rp {_pembayaran.jumlah_bayar:N0}";

                // Set editable fields
                // Status
                var statusItem = cmbStatus.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Tag?.ToString() == _pembayaran.status_bayar);
                cmbStatus.SelectedItem = statusItem ?? cmbStatus.Items[0];

                // Metode Pembayaran
                var metodeItem = cmbMetode.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Tag?.ToString() == _pembayaran.metode_pembayaran);
                cmbMetode.SelectedItem = metodeItem ?? cmbMetode.Items[0];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminPembayaranEditDialog] Error loading data: {ex.Message}");
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSimpan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                {
                    MessageBox.Show("Status pembayaran harus dipilih!", "Validasi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Konfirmasi
                var result = MessageBox.Show(
                    "Simpan perubahan data pembayaran?\n\n" +
                    $"Status: {(cmbStatus.SelectedItem as ComboBoxItem)?.Content}\n" +
                    $"Metode: {(cmbMetode.SelectedItem as ComboBoxItem)?.Content}",
                    "Konfirmasi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Disable tombol untuk mencegah double-click
                btnSimpan.IsEnabled = false;
                var newStatus = (cmbStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var newMetode = (cmbMetode.SelectedItem as ComboBoxItem)?.Tag?.ToString();

                // Cek perubahan status
                bool statusChanged = newStatus != _pembayaran.status_bayar;
                string oldStatus = _pembayaran.status_bayar;
                _pembayaran.status_bayar = newStatus ?? _pembayaran.status_bayar;
                _pembayaran.metode_pembayaran = newMetode ?? _pembayaran.metode_pembayaran;

                // Simpan perubahan
                var (success, message) = await _pembayaranService.UpdatePembayaranAsync(_pembayaran);

                if (success)
                {
                    // Jika status berubah ke "Sukses", validasi pembayaran
                    if (statusChanged && newStatus == "Sukses" && oldStatus != "Sukses")
                    {
                        await _pembayaranService.ValidasiPembayaranAsync(_pembayaran.pembayaran_id);
                        try
                        {
                            // Ambil data lengkap yang baru saja divalidasi
                            // Kita perlu data 'Tiket' yang lengkap
                            var tiketService = new TiketService();
                            var tiket = await tiketService.GetTiketByIdAsync(_pembayaran.tiket_id); // Asumsi GetTiketByIdAsync() mengambil relasi

                            if (tiket?.Jadwal?.pelabuhan_asal != null)
                            {
                                var notifService = new NotifikasiService();
                                await notifService.SendPembayaranBerhasilNotificationAsync(
                                    penggunaId: tiket.pengguna_id,
                                    tiketKode: tiket.kode_tiket,
                                    ruteAsal: tiket.Jadwal.pelabuhan_asal.nama_pelabuhan,
                                    ruteTujuan: tiket.Jadwal.pelabuhan_tujuan.nama_pelabuhan,
                                    jadwalId: tiket.jadwal_id,
                                    tiketId: tiket.tiket_id
                                );
                                System.Diagnostics.Debug.WriteLine($"[AdminPembayaranEditDialog] Notifikasi 'Pembayaran Berhasil' terkirim ke {tiket.pengguna_id}");
                            }
                        }
                        catch (Exception exNotif)
                        {
                            System.Diagnostics.Debug.WriteLine($"[AdminPembayaranEditDialog] GAGAL kirim notifikasi: {exNotif.Message}");
                        }
                    }
                    // Jika status berubah ke "Gagal", tolak pembayaran
                    else if (statusChanged && newStatus == "Gagal" && oldStatus != "Gagal")
                    {
                        await _pembayaranService.TolakPembayaranAsync(_pembayaran.pembayaran_id, "Ditolak oleh admin");
                    }

                    MessageBox.Show("Data pembayaran berhasil diperbarui!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(message, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    btnSimpan.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminPembayaranEditDialog] Error saving: {ex.Message}");
                MessageBox.Show($"Error menyimpan data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnSimpan.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
