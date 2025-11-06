using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class AdminPelabuhanDetailDialog : Window
    {
        private readonly PelabuhanService _pelabuhanService;
        private readonly JadwalService _jadwalService;
        private readonly int _pelabuhanId;

        public AdminPelabuhanDetailDialog(int pelabuhanId)
        {
            InitializeComponent();
            _pelabuhanService = new PelabuhanService();
            _jadwalService = new JadwalService();
            _pelabuhanId = pelabuhanId;

            LoadPelabuhanDetail();
        }

        private async void LoadPelabuhanDetail()
        {
            try
            {
                // Load pelabuhan info
                var pelabuhan = await _pelabuhanService.GetPelabuhanByIdAsync(_pelabuhanId);
                if (pelabuhan != null)
                {
                    txtNamaPelabuhan.Text = pelabuhan.nama_pelabuhan;
                    txtKota.Text = pelabuhan.kota;
                    txtProvinsi.Text = pelabuhan.provinsi;
                    txtTimezone.Text = $"{pelabuhan.timezone} (UTC+{pelabuhan.TimezoneOffsetHours})";
                    txtFasilitas.Text = pelabuhan.fasilitas;
                    txtDeskripsi.Text = pelabuhan.deskripsi ?? "(Tidak ada deskripsi)";
                }

                // Load jadwal info
                var jadwals = await _jadwalService.GetJadwalByPelabuhanIdAsync(_pelabuhanId);

                if (jadwals.Count > 0)
                {
                    // Transform jadwal data untuk display
                    var jadwalDisplayItems = jadwals.Select(j => new JadwalDisplayItem
                    {
                        jadwal_id = j.jadwal_id,
                        pelabuhan_asal = j.pelabuhan_asal?.nama_pelabuhan ?? "Unknown",
                        pelabuhan_tujuan = j.pelabuhan_tujuan?.nama_pelabuhan ?? "Unknown",
                        jam_keberangkatan = j.waktu_berangkat.ToString(@"HH\:mm"),
                        jam_tiba = j.waktu_tiba.ToString(@"HH\:mm"),
                        nama_kapal = j.kapal?.nama_kapal ?? "Unknown",
                        kelas_layanan = j.kelas_layanan,
                        status = j.status,
                        tipe_rute = j.pelabuhan_asal_id == _pelabuhanId ? "Keberangkatan" : "Kedatangan",
                        StatusVisibility = j.status == "Active" ? Visibility.Visible : Visibility.Collapsed,
                        InactiveStatusVisibility = j.status != "Active" ? Visibility.Visible : Visibility.Collapsed
                    }).ToList();

                    jadwalList.ItemsSource = jadwalDisplayItems;

                    // Statistics
                    txtTotalJadwal.Text = jadwals.Count.ToString();
                    txtJadwalKeberangkatan.Text = jadwals.Count(j => j.pelabuhan_asal_id == _pelabuhanId).ToString();
                    txtJadwalKedatangan.Text = jadwals.Count(j => j.pelabuhan_tujuan_id == _pelabuhanId).ToString();
                }
                else
                {
                    txtNoJadwal.Visibility = Visibility.Visible;
                    txtTotalJadwal.Text = "0";
                    txtJadwalKeberangkatan.Text = "0";
                    txtJadwalKedatangan.Text = "0";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading pelabuhan detail: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void JadwalCard_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E7F3F8"));
            }
        }

        private void JadwalCard_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA"));
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Helper class for jadwal display
        private class JadwalDisplayItem
        {
            public int jadwal_id { get; set; }
            public string pelabuhan_asal { get; set; } = string.Empty;
            public string pelabuhan_tujuan { get; set; } = string.Empty;
            public string jam_keberangkatan { get; set; } = string.Empty;
            public string jam_tiba { get; set; } = string.Empty;
            public string nama_kapal { get; set; } = string.Empty;
            public string kelas_layanan { get; set; } = string.Empty;
            public string status { get; set; } = string.Empty;
            public string tipe_rute { get; set; } = string.Empty;
            public Visibility StatusVisibility { get; set; }
            public Visibility InactiveStatusVisibility { get; set; }
        }
    }
}
