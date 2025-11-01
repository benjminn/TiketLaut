using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TiketLaut.Services;
using TiketLaut.Models;

namespace TiketLaut.Views
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Window
    {
        private bool _isLoggedIn = false;
        private string _currentUser = "";
        private readonly JadwalService _jadwalService;

        // Constructor default (untuk pertama kali buka app)
        public HomePage()
        {
            InitializeComponent();
            _jadwalService = new JadwalService();
            SetNavbarVisibility();
            LoadDataAsync();
        }

        // Constructor dengan parameter (untuk setelah login/logout)
        public HomePage(bool isLoggedIn, string username = "") : this()
        {
            _isLoggedIn = isLoggedIn;
            _currentUser = username;

            SetNavbarVisibility();

            if (_isLoggedIn && !string.IsNullOrEmpty(_currentUser))
            {
                navbarPostLogin.SetUserInfo(_currentUser);
            }
        }

        /// <summary>
        /// Load data dari database saat window dibuka
        /// </summary>
        private async void LoadDataAsync()
        {
            try
            {
                // Show loading state
                btnCariJadwal.IsEnabled = false;
                btnCariJadwal.Content = "Memuat data...";

                // Load pelabuhan dari database
                var pelabuhans = await _jadwalService.GetAllPelabuhanAsync();

                if (pelabuhans.Any())
                {
                    // Populate ComboBox Pelabuhan Asal
                    cmbPelabuhanAsal.Items.Clear();
                    cmbPelabuhanAsal.Items.Add(new PelabuhanComboBoxItem
                    {
                        Id = 0,
                        DisplayText = "Pilih Pelabuhan Asal"
                    });

                    foreach (var pelabuhan in pelabuhans)
                    {
                        cmbPelabuhanAsal.Items.Add(new PelabuhanComboBoxItem
                        {
                            Id = pelabuhan.pelabuhan_id,
                            DisplayText = $"{pelabuhan.nama_pelabuhan} ({pelabuhan.kota})"
                        });
                    }
                    cmbPelabuhanAsal.SelectedIndex = 0;

                    // Populate ComboBox Pelabuhan Tujuan
                    cmbPelabuhanTujuan.Items.Clear();
                    cmbPelabuhanTujuan.Items.Add(new PelabuhanComboBoxItem
                    {
                        Id = 0,
                        DisplayText = "Pilih Pelabuhan Tujuan"
                    });

                    foreach (var pelabuhan in pelabuhans)
                    {
                        cmbPelabuhanTujuan.Items.Add(new PelabuhanComboBoxItem
                        {
                            Id = pelabuhan.pelabuhan_id,
                            DisplayText = $"{pelabuhan.nama_pelabuhan} ({pelabuhan.kota})"
                        });
                    }
                    cmbPelabuhanTujuan.SelectedIndex = 0;

                    // Populate Kelas Layanan
                    cmbKelasLayanan.Items.Clear();
                    cmbKelasLayanan.Items.Add(new ComboBoxItem { Content = "Pilih Kelas Layanan" });
                    cmbKelasLayanan.Items.Add(new ComboBoxItem { Content = "Reguler" });
                    cmbKelasLayanan.Items.Add(new ComboBoxItem { Content = "Eksekutif" });
                    cmbKelasLayanan.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show(
                        "Tidak dapat memuat data pelabuhan dari database.\n" +
                        "Pastikan koneksi database aktif.",
                        "Peringatan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Terjadi kesalahan saat memuat data:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Restore button state
                btnCariJadwal.IsEnabled = true;
                btnCariJadwal.Content = "Cari Jadwal";
            }
        }

        private void SetNavbarVisibility()
        {
            if (_isLoggedIn)
            {
                navbarPreLogin.Visibility = Visibility.Collapsed;
                navbarPostLogin.Visibility = Visibility.Visible;
            }
            else
            {
                navbarPreLogin.Visibility = Visibility.Visible;
                navbarPostLogin.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Apakah Anda yakin ingin logout?",
                "Konfirmasi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Clear session
                SessionManager.Logout();

                // Buka LoginWindow
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();

                // Tutup HomePage
                this.Close();
            }
        }

        private async void BtnCariJadwal_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validasi Pelabuhan Asal
                if (cmbPelabuhanAsal.SelectedIndex <= 0)
                {
                    MessageBox.Show("Silakan pilih Pelabuhan Asal!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi Pelabuhan Tujuan
                if (cmbPelabuhanTujuan.SelectedIndex <= 0)
                {
                    MessageBox.Show("Silakan pilih Pelabuhan Tujuan!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi Kelas Layanan
                if (cmbKelasLayanan.SelectedIndex <= 0)
                {
                    MessageBox.Show("Silakan pilih Kelas Layanan!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi Tanggal
                if (!dpTanggal?.SelectedDate.HasValue ?? true)
                {
                    MessageBox.Show("Silakan pilih Tanggal!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi Penumpang
                if (cmbPenumpang?.SelectedIndex <= 0)
                {
                    MessageBox.Show("Silakan pilih Jumlah Penumpang!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi Jenis Kendaraan
                if (cmbJenisKendaraan.SelectedIndex <= 0)
                {
                    MessageBox.Show("Silakan pilih Jenis Kendaraan!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show loading
                btnCariJadwal.IsEnabled = false;
                btnCariJadwal.Content = "Mencari jadwal...";

                // Ambil data dari form
                var pelabuhanAsal = (PelabuhanComboBoxItem)cmbPelabuhanAsal.SelectedItem;
                var pelabuhanTujuan = (PelabuhanComboBoxItem)cmbPelabuhanTujuan.SelectedItem;
                var kelasLayanan = ((ComboBoxItem)cmbKelasLayanan.SelectedItem).Content.ToString();
                var jenisKendaraanIndex = cmbJenisKendaraan.SelectedIndex - 1; // -1 karena index 0 adalah "Pilih"

                // Parse jumlah penumpang
                var penumpangText = ((ComboBoxItem)cmbPenumpang.SelectedItem).Content.ToString();
                int jumlahPenumpang = 1;
                if (penumpangText != null && penumpangText.Contains("Penumpang"))
                {
                    var parts = penumpangText.Split(' ');
                    if (parts.Length > 0)
                    {
                        int.TryParse(parts[0].Replace("+", ""), out jumlahPenumpang);
                    }
                }

                // Parse jam keberangkatan (optional)
                TimeOnly? jamKeberangkatan = null;
                if (cmbJam.SelectedIndex > 0)
                {
                    var jamText = ((ComboBoxItem)cmbJam.SelectedItem).Content.ToString();
                    if (TimeOnly.TryParse(jamText, out TimeOnly jam))
                    {
                        jamKeberangkatan = jam;
                    }
                }

                // Validasi pelabuhan asal dan tujuan tidak sama
                if (pelabuhanAsal.Id == pelabuhanTujuan.Id)
                {
                    MessageBox.Show(
                        "Pelabuhan asal dan tujuan tidak boleh sama!",
                        "Peringatan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Search jadwal dari database
                var jadwals = await _jadwalService.SearchJadwalAsync(
                    pelabuhanAsal.Id,
                    pelabuhanTujuan.Id,
                    kelasLayanan ?? "Reguler",
                    jamKeberangkatan,
                    jenisKendaraanIndex
                );

                if (jadwals == null || !jadwals.Any())
                {
                    MessageBox.Show(
                        "Tidak ditemukan jadwal yang sesuai dengan kriteria pencarian Anda.\n\n" +
                        "Silakan coba dengan kriteria lain atau pilih tanggal berbeda.",
                        "Jadwal Tidak Ditemukan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Buat search criteria untuk dikirim ke ScheduleWindow
                var searchCriteria = new SearchCriteria
                {
                    PelabuhanAsalId = pelabuhanAsal.Id,
                    PelabuhanTujuanId = pelabuhanTujuan.Id,
                    KelasLayanan = kelasLayanan ?? "Reguler",
                    TanggalKeberangkatan = dpTanggal.SelectedDate.Value,
                    JamKeberangkatan = jamKeberangkatan,
                    JumlahPenumpang = jumlahPenumpang,
                    JenisKendaraanId = jenisKendaraanIndex
                };

                // Buka ScheduleWindow dengan hasil pencarian
                var scheduleWindow = new ScheduleWindow(jadwals, searchCriteria);
                scheduleWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Terjadi kesalahan saat mencari jadwal:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // Restore button state
                btnCariJadwal.IsEnabled = true;
                btnCariJadwal.Content = "Cari Jadwal";
            }
        }

        private void cmbJenisKendaraan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: bisa tambahkan logika untuk update harga preview
        }

        private void navbarPreLogin_Loaded(object sender, RoutedEventArgs e)
        {
            // Event handler untuk navbar pre-login
        }
    }
}