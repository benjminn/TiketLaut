using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;
using TiketLaut.Models;

namespace TiketLaut.Views
{
    public partial class AdminTiketFormDialog : Window
    {
        private Tiket? _tiket;
        private readonly int _tiketId;
        private readonly bool _isEdit;
        private readonly DetailKendaraanService _kendaraanService;
        private List<DetailKendaraan> _daftarKendaraan = new List<DetailKendaraan>();


        public AdminTiketFormDialog(int tiketId)
        {
            InitializeComponent();
            _tiketId = tiketId;
            _isEdit = tiketId > 0;
            _kendaraanService = new DetailKendaraanService();
            LoadTiketData();
        }

        private async void LoadTiketData()
        {
            try
            {
                var tiketService = new TiketService();
                _tiket = await tiketService.GetTiketByIdAsync(_tiketId);

                if (_tiket != null)
                {
                    txtKodeTiket.Text = _tiket.kode_tiket;
                    txtTotalHarga.Text = $"Rp {_tiket.total_harga:N0}";
                    txtTanggalPemesanan.Text = _tiket.tanggal_pemesanan.ToString("dd MMMM yyyy HH:mm");
                    txtPlatNomor.Text = _tiket.plat_nomor ?? "";

                    foreach (ComboBoxItem item in cmbStatus.Items)
                    {
                        if (item.Content.ToString() == _tiket.status_tiket)
                        {
                            cmbStatus.SelectedItem = item;
                            break;
                        }
                    }
                    txtKodeTiket.IsReadOnly = true;
                    txtTotalHarga.IsReadOnly = true;
                    txtTanggalPemesanan.IsReadOnly = true;

                    LoadKendaraanData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tiket data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async void LoadKendaraanData()
        {
            if (_tiket == null || _tiket.Jadwal == null)
            {
                MessageBox.Show("Gagal memuat data kendaraan: Data jadwal tidak ditemukan pada tiket ini.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int grupKendaraanId = _tiket.Jadwal.grup_kendaraan_id;
                _daftarKendaraan = await _kendaraanService.GetByGrupAsync(grupKendaraanId);

                cmbJenisKendaraan.ItemsSource = _daftarKendaraan;
                cmbJenisKendaraan.DisplayMemberPath = "deskripsi";

                SetSelectedKendaraan();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading kendaraan data: {ex.Message}", "Error");
            }
        }


        private void SetSelectedKendaraan()
        {
            if (_tiket != null && _daftarKendaraan.Any())
            {
                var selected = _daftarKendaraan.FirstOrDefault(k =>
                    k.deskripsi == _tiket.jenis_kendaraan_enum);

                if (selected != null)
                {
                    cmbJenisKendaraan.SelectedItem = selected;


                    string deskripsiLower = selected.deskripsi.ToLower();

                    if (deskripsiLower.Contains("pejalan kaki") || deskripsiLower == "sepeda")
                    {
                        txtPlatNomor.IsEnabled = false;
                    }
                }
            }
        }


        private void CmbJenisKendaraan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbJenisKendaraan.SelectedItem is DetailKendaraan kendaraanTerpilih && _tiket != null)
            {
                txtTotalHarga.Text = $"Rp {kendaraanTerpilih.harga_kendaraan:N0}";


                string deskripsiLower = kendaraanTerpilih.deskripsi.ToLower();

                if (deskripsiLower.Contains("pejalan kaki") || deskripsiLower == "sepeda")
                {
                    txtPlatNomor.Text = "";
                    txtPlatNomor.IsEnabled = false;
                }
                // --- AKHIR PERBAIKAN ---
                else
                {
                    txtPlatNomor.IsEnabled = true;
                }
            }
        }


        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_tiket == null) return;

            if (cmbJenisKendaraan.SelectedItem == null)
            {
                MessageBox.Show("Silakan pilih jenis kendaraan.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                DetailKendaraan kendaraanTerpilih = (DetailKendaraan)cmbJenisKendaraan.SelectedItem;

                _tiket.status_tiket = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Menunggu Pembayaran";
                _tiket.jenis_kendaraan_enum = kendaraanTerpilih.deskripsi;
                _tiket.total_harga = kendaraanTerpilih.harga_kendaraan;

                string deskripsiLower = kendaraanTerpilih.deskripsi.ToLower();

                // Bandingkan dengan string huruf kecil
                if (deskripsiLower.Contains("pejalan kaki") || deskripsiLower == "sepeda")
                {
                    _tiket.plat_nomor = null;
                }
                else
                {
                    // Jika BUKAN, ini pasti kendaraan dan plat nomor WAJIB
                    if (string.IsNullOrWhiteSpace(txtPlatNomor.Text))
                    {
                        MessageBox.Show("Plat nomor wajib diisi untuk kendaraan.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        return; // Hentikan penyimpanan
                    }
                    _tiket.plat_nomor = txtPlatNomor.Text.Trim();
                }

                var tiketService = new TiketService();
                await tiketService.UpdateTiketAsync(_tiket);

                MessageBox.Show("Tiket berhasil diperbarui.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving tiket: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}