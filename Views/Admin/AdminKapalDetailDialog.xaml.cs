using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TiketLaut.Services;
using TiketLaut.Helpers;

namespace TiketLaut.Views
{
    public partial class AdminKapalDetailDialog : Window
    {
        private readonly KapalService _kapalService;
        private readonly JadwalService _jadwalService;
        private readonly int _kapalId;

        public AdminKapalDetailDialog(int kapalId)
        {
            InitializeComponent();
            _kapalService = new KapalService();
            _jadwalService = new JadwalService();
            _kapalId = kapalId;
            ZoomHelper.EnableZoom(this);

            LoadKapalDetail();
        }

        private async void LoadKapalDetail()
        {
            try
            {
                // Load kapal info
                var kapal = await _kapalService.GetKapalByIdAsync(_kapalId);
                
                if (kapal == null)
                {
                    MessageBox.Show("Kapal tidak ditemukan!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Set kapal info
                txtTitle.Text = $"Detail Kapal: {kapal.nama_kapal}";
                txtNamaKapal.Text = kapal.nama_kapal;
                txtKapasitasPenumpang.Text = $"{kapal.kapasitas_penumpang_max} orang";
                txtKapasitasKendaraan.Text = $"{kapal.kapasitas_kendaraan_max} kendaraan";
                txtFasilitas.Text = kapal.fasilitas ?? "-";
                txtDeskripsi.Text = kapal.deskripsi ?? "-";

                // Load jadwal terkait
                var jadwals = await _jadwalService.GetJadwalByKapalIdAsync(_kapalId);
                
                if (jadwals != null && jadwals.Any())
                {
                    // Set statistics
                    txtTotalJadwal.Text = jadwals.Count.ToString();
                    txtJadwalAktif.Text = jadwals.Count(j => j.status == "Active").ToString();

                    // Transform data untuk binding
                    var jadwalItems = jadwals.Select(j => new JadwalDisplayItem
                    {
                        jadwal_id = j.jadwal_id,
                        pelabuhan_asal = j.pelabuhan_asal?.nama_pelabuhan ?? "Unknown",
                        pelabuhan_tujuan = j.pelabuhan_tujuan?.nama_pelabuhan ?? "Unknown",
                        jam_keberangkatan = j.waktu_berangkat.ToString(@"HH\:mm"),
                        jam_tiba = j.waktu_tiba.ToString(@"HH\:mm"),
                        kelas_layanan = j.kelas_layanan,
                        sisa_kapasitas = j.sisa_kapasitas_penumpang,
                        status = j.status,
                        StatusVisibility = j.status == "Active" ? Visibility.Visible : Visibility.Collapsed,
                        InactiveStatusVisibility = j.status != "Active" ? Visibility.Visible : Visibility.Collapsed
                    }).ToList();

                    icJadwalList.ItemsSource = jadwalItems;
                    txtNoJadwal.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtTotalJadwal.Text = "0";
                    txtJadwalAktif.Text = "0";
                    txtNoJadwal.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading kapal detail: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void JadwalCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E7F3F8"));
            }
        }

        private void JadwalCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA"));
            }
        }
    }

    // Helper class untuk display
    public class JadwalDisplayItem
    {
        public int jadwal_id { get; set; }
        public string pelabuhan_asal { get; set; } = string.Empty;
        public string pelabuhan_tujuan { get; set; } = string.Empty;
        public string jam_keberangkatan { get; set; } = string.Empty;
        public string jam_tiba { get; set; } = string.Empty;
        public string kelas_layanan { get; set; } = string.Empty;
        public int sisa_kapasitas { get; set; }
        public string status { get; set; } = string.Empty;
        public Visibility StatusVisibility { get; set; }
        public Visibility InactiveStatusVisibility { get; set; }
    }
}
