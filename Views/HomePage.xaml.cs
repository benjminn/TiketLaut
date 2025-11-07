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
            
            // Set default penumpang value after controls are initialized
            txtPenumpang.Text = "1";
            
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
                    cmbKelasLayanan.Items.Add(new ComboBoxItem { Content = "Ekspress" }); // ? TYPO SESUAI DATABASE
                    cmbKelasLayanan.SelectedIndex = 0;

                    // Set DatePicker default to today
                    dpTanggal.SelectedDate = DateTime.Today;
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

                // Validasi Jenis Kendaraan (harus sebelum validasi penumpang)
                if (cmbJenisKendaraan.SelectedIndex <= 0)
                {
                    MessageBox.Show("Silakan pilih Jenis Kendaraan!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Dapatkan index jenis kendaraan
                int jenisKendaraanIndex = cmbJenisKendaraan.SelectedIndex - 1; // -1 karena index 0 adalah "Pilih"
                
                // Dapatkan maksimal penumpang untuk jenis kendaraan yang dipilih
                int maksimalPenumpang = DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanIndex);

                // Validasi Penumpang
                if (!int.TryParse(txtPenumpang.Text, out int jumlahPenumpangInput) || jumlahPenumpangInput < 1)
                {
                    MessageBox.Show("Jumlah penumpang minimal adalah 1!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi penumpang tidak melebihi maksimal untuk jenis kendaraan
                if (jumlahPenumpangInput > maksimalPenumpang)
                {
                    string jenisKendaraanText = GetJenisKendaraanText(jenisKendaraanIndex);
                    MessageBox.Show(
                        $"Jumlah penumpang untuk {jenisKendaraanText} maksimal {maksimalPenumpang} orang!",
                        "Peringatan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Show loading
                btnCariJadwal.IsEnabled = false;
                btnCariJadwal.Content = "Mencari jadwal...";

                // Ambil data dari form
                var pelabuhanAsal = (PelabuhanComboBoxItem?)cmbPelabuhanAsal.SelectedItem;
                var pelabuhanTujuan = (PelabuhanComboBoxItem?)cmbPelabuhanTujuan.SelectedItem;
                
                // Double check null (untuk menghilangkan warning)
                if (pelabuhanAsal == null || pelabuhanTujuan == null)
                {
                    MessageBox.Show("Pelabuhan tidak valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnCariJadwal.IsEnabled = true;
                    btnCariJadwal.Content = "Cari Jadwal";
                    return;
                }
                
                var kelasLayananItem = cmbKelasLayanan.SelectedItem as ComboBoxItem;
                var kelasLayanan = kelasLayananItem?.Content?.ToString();

                // Parse jumlah penumpang dari TextBox
                int jumlahPenumpang = int.Parse(txtPenumpang.Text);

                // Parse tanggal keberangkatan (from dpTanggal DatePicker)
#pragma warning disable CS8602
                DateTime? tanggalKeberangkatan = dpTanggal.SelectedDate;
#pragma warning restore CS8602

                // Parse jam keberangkatan (optional)
                int? jamKeberangkatan = null;
                if (cmbJam.SelectedIndex > 0) // Index 0 adalah "Pilih Jam"
                {
                    var selectedJamItem = cmbJam.SelectedItem as ComboBoxItem;
                    if (selectedJamItem?.Tag != null)
                    {
                        jamKeberangkatan = (int)selectedJamItem.Tag;
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
                    tanggalKeberangkatan,
                    jenisKendaraanIndex,
                    jamKeberangkatan
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
                    PelabuhanAsalId = pelabuhanAsal!.Id,
                    PelabuhanTujuanId = pelabuhanTujuan!.Id,
                    KelasLayanan = kelasLayanan ?? "Reguler",
                    TanggalKeberangkatan = tanggalKeberangkatan ?? DateTime.Today,
                    JamKeberangkatan = jamKeberangkatan,
                    JumlahPenumpang = jumlahPenumpang,
                    JenisKendaraanId = jenisKendaraanIndex
                };

                // SAVE TO SESSION - PENTING!
                SessionManager.SaveSearchSession(searchCriteria, jadwals);

                // Buka ScheduleWindow dengan hasil pencarian
                var scheduleWindow = new ScheduleWindow(jadwals, searchCriteria);
                scheduleWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                {
                    MessageBox.Show(
                        $"Terjadi kesalahan saat mencari jadwal:\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
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

        // ========== EVENT HANDLERS UNTUK PENUMPANG INPUT ==========

        /// <summary>
        /// Button minus untuk mengurangi jumlah penumpang
        /// </summary>
        private void BtnMinusPenumpang_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtPenumpang.Text, out int currentValue))
            {
                if (currentValue > 1)
                {
                    currentValue--;
                    txtPenumpang.Text = currentValue.ToString();
                    
                    // Sync ke popup textbox jika ada
                    if (txtPopupPenumpangHome != null)
                        txtPopupPenumpangHome.Text = currentValue.ToString();
                    
                    UpdatePenumpangDisplay(currentValue);
                    UpdatePopupButtonStatesHome(currentValue);
                }
            }
        }

        /// <summary>
        /// Button untuk toggle popup penumpang
        /// </summary>
        private void BtnPenumpang_Click(object sender, RoutedEventArgs e)
        {
            if (popupPenumpangHome != null)
            {
                // Sync nilai dari hidden textbox ke popup textbox
                if (int.TryParse(txtPenumpang.Text, out int current))
                {
                    txtPopupPenumpangHome.Text = current.ToString();
                    UpdatePopupButtonStatesHome(current);
                }
                
                popupPenumpangHome.IsOpen = !popupPenumpangHome.IsOpen;
            }
        }

        /// <summary>
        /// Button plus untuk menambah jumlah penumpang
        /// </summary>
        private void BtnPlusPenumpang_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtPenumpang.Text, out int currentValue))
            {
                // Dapatkan maksimal penumpang berdasarkan kendaraan yang dipilih
                int maksimalPenumpang = GetMaksimalPenumpangFromKendaraan();
                
                if (currentValue < maksimalPenumpang)
                {
                    currentValue++;
                    txtPenumpang.Text = currentValue.ToString();
                    txtPopupPenumpangHome.Text = currentValue.ToString();
                    UpdatePenumpangDisplay(currentValue);
                    UpdatePopupButtonStatesHome(currentValue);
                }
                else
                {
                    // Tampilkan pesan jika sudah maksimal
                    MessageBox.Show(
                        $"Jumlah penumpang maksimal untuk jenis kendaraan ini adalah {maksimalPenumpang} orang.",
                        "Batas Maksimal",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// Mendapatkan maksimal penumpang berdasarkan kendaraan yang dipilih
        /// </summary>
        private int GetMaksimalPenumpangFromKendaraan()
        {
            if (cmbJenisKendaraan == null || cmbJenisKendaraan.SelectedIndex <= 0)
                return 10; // Default maksimal jika belum pilih kendaraan

            int jenisKendaraanIndex = cmbJenisKendaraan.SelectedIndex - 1;
            return DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanIndex);
        }

        /// <summary>
        /// Update tampilan text penumpang
        /// </summary>
        private void UpdatePenumpangDisplay(int count)
        {
            if (txtPenumpangDisplay != null)
            {
                // Update menggunakan Inlines karena TextBlock menggunakan Run elements
                txtPenumpangDisplay.Inlines.Clear();
                txtPenumpangDisplay.Inlines.Add(new System.Windows.Documents.Run(count.ToString()));
                txtPenumpangDisplay.Inlines.Add(new System.Windows.Documents.Run(" "));
                txtPenumpangDisplay.Inlines.Add(new System.Windows.Documents.Run("Penumpang"));
            }
        }

        /// <summary>
        /// Validasi input hanya angka untuk TextBox penumpang popup
        /// </summary>
        private void TxtPopupPenumpangHome_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Hanya terima angka
            e.Handled = !int.TryParse(e.Text, out _);
        }

        /// <summary>
        /// Event handler ketika text penumpang popup berubah (manual input)
        /// </summary>
        private void TxtPopupPenumpangHome_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPopupPenumpangHome == null || txtPenumpang == null)
                return;

            // Jika kosong, biarkan kosong (user sedang menghapus untuk ketik angka baru)
            if (string.IsNullOrWhiteSpace(txtPopupPenumpangHome.Text))
            {
                return;
            }

            if (int.TryParse(txtPopupPenumpangHome.Text, out int value))
            {
                // Validasi tidak boleh 0
                if (value == 0)
                {
                    txtPopupPenumpangHome.Text = "";
                    txtPopupPenumpangHome.SelectionStart = 0;
                    return;
                }

                // Validasi minimal 1
                if (value < 1)
                {
                    value = 1;
                    txtPopupPenumpangHome.Text = "1";
                    txtPopupPenumpangHome.SelectionStart = 1;
                }

                // Validasi maksimal sesuai kendaraan
                int maksimalPenumpang = GetMaksimalPenumpangFromKendaraan();
                if (value > maksimalPenumpang)
                {
                    value = maksimalPenumpang;
                    txtPopupPenumpangHome.Text = maksimalPenumpang.ToString();
                    txtPopupPenumpangHome.SelectionStart = maksimalPenumpang.ToString().Length;
                }

                // Sync ke hidden textbox dan update display
                txtPenumpang.Text = value.ToString();
                UpdatePenumpangDisplay(value);
                UpdatePopupButtonStatesHome(value);
            }
        }

        /// <summary>
        /// Update state enabled/disabled untuk button +/- di popup
        /// </summary>
        private void UpdatePopupButtonStatesHome(int current)
        {
            int maksimalPenumpang = GetMaksimalPenumpangFromKendaraan();
            
            if (btnPopupMinusPenumpangHome != null)
                btnPopupMinusPenumpangHome.IsEnabled = current > 1;
            
            if (btnPopupPlusPenumpangHome != null)
                btnPopupPlusPenumpangHome.IsEnabled = current < maksimalPenumpang;
        }

        /// <summary>
        /// Event handler ketika popup penumpang ditutup
        /// </summary>
        private void PopupPenumpangHome_Closed(object sender, EventArgs e)
        {
            // Saat popup ditutup, jika TextBox kosong atau invalid, restore ke nilai terakhir yang valid
            if (txtPopupPenumpangHome != null && txtPenumpang != null)
            {
                if (string.IsNullOrWhiteSpace(txtPopupPenumpangHome.Text))
                {
                    // Restore dari hidden textbox
                    if (int.TryParse(txtPenumpang.Text, out int lastValue) && lastValue >= 1)
                    {
                        txtPopupPenumpangHome.Text = lastValue.ToString();
                    }
                    else
                    {
                        // Fallback ke 1
                        txtPopupPenumpangHome.Text = "1";
                        txtPenumpang.Text = "1";
                        UpdatePenumpangDisplay(1);
                    }
                }
            }
        }

        /// <summary>
        /// Button minus di dalam popup untuk mengurangi jumlah penumpang
        /// </summary>
        private void BtnPopupMinusPenumpangHome_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtPopupPenumpangHome.Text, out int currentValue))
            {
                if (currentValue > 1)
                {
                    currentValue--;
                    txtPopupPenumpangHome.Text = currentValue.ToString();
                    txtPenumpang.Text = currentValue.ToString();
                    UpdatePenumpangDisplay(currentValue);
                    UpdatePopupButtonStatesHome(currentValue);
                }
            }
        }

        /// <summary>
        /// Button plus di dalam popup untuk menambah jumlah penumpang
        /// </summary>
        private void BtnPopupPlusPenumpangHome_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtPopupPenumpangHome.Text, out int currentValue))
            {
                int maksimalPenumpang = GetMaksimalPenumpangFromKendaraan();
                
                if (currentValue < maksimalPenumpang)
                {
                    currentValue++;
                    txtPopupPenumpangHome.Text = currentValue.ToString();
                    txtPenumpang.Text = currentValue.ToString();
                    UpdatePenumpangDisplay(currentValue);
                    UpdatePopupButtonStatesHome(currentValue);
                }
            }
        }

        /// <summary>
        /// Validasi input hanya angka di TextBox penumpang
        /// </summary>
        private void TxtPenumpang_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Hanya izinkan angka
            e.Handled = !IsTextNumeric(e.Text);
        }

        /// <summary>
        /// Handle perubahan text untuk update status button +/-
        /// </summary>
        private void TxtPenumpang_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Null check untuk semua controls
            if (txtPenumpang == null)
                return;

            // Prevent infinite loop during initialization
            if (string.IsNullOrEmpty(txtPenumpang.Text))
                return;

            if (int.TryParse(txtPenumpang.Text, out int value))
            {
                // Update display text
                UpdatePenumpangDisplay(value);
                
                // Update popup button states jika popup sedang terbuka
                UpdatePopupButtonStatesHome(value);
            }
            else
            {
                // Jika input tidak valid, set ke 1 (dengan check untuk prevent recursion)
                if (txtPenumpang.Text != "1")
                    txtPenumpang.Text = "1";
            }
        }

        /// <summary>
        /// Helper method untuk cek apakah text adalah numeric
        /// </summary>
        private static bool IsTextNumeric(string text)
        {
            return int.TryParse(text, out _);
        }

        // ========== EVENT HANDLERS UNTUK JAM INPUT ==========

        /// <summary>
        /// Event handler saat tanggal dipilih
        /// </summary>
        private void DpTanggal_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) // Hanya jalankan jika window sudah fully loaded
                LoadAvailableJamAsync();
        }

        /// <summary>
        /// Event handler saat pelabuhan asal dipilih
        /// </summary>
        private void CmbPelabuhanAsal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) // Hanya jalankan jika window sudah fully loaded
                LoadAvailableJamAsync();
        }

        /// <summary>
        /// Event handler saat pelabuhan tujuan dipilih
        /// </summary>
        private void CmbPelabuhanTujuan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) // Hanya jalankan jika window sudah fully loaded
                LoadAvailableJamAsync();
        }

        /// <summary>
        /// Event handler saat kelas layanan dipilih
        /// </summary>
        private void CmbKelasLayanan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) // Hanya jalankan jika window sudah fully loaded
                LoadAvailableJamAsync();
        }

        /// <summary>
        /// Event handler saat jenis kendaraan dipilih - mengatur maksimal penumpang
        /// </summary>
        private void CmbJenisKendaraan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || cmbJenisKendaraan == null || txtPenumpang == null)
                return;

            int selectedIndex = cmbJenisKendaraan.SelectedIndex;
            
            // Index 0 adalah "Pilih Jenis Kendaraan", skip
            if (selectedIndex <= 0)
                return;

            // Get jenis kendaraan (index - 1 karena index 0 adalah placeholder)
            int jenisKendaraanIndex = selectedIndex - 1;
            
            // Dapatkan maksimal penumpang untuk kendaraan ini
            int maksimalPenumpang = DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanIndex);
            
            // Ambil nilai penumpang saat ini
            if (int.TryParse(txtPenumpang.Text, out int currentPenumpang))
            {
                // Jika jumlah penumpang saat ini melebihi maksimal, set ke maksimal
                if (currentPenumpang > maksimalPenumpang)
                {
                    txtPenumpang.Text = maksimalPenumpang.ToString();
                    UpdatePenumpangDisplay(maksimalPenumpang);
                }
            }
            else
            {
                // Jika tidak valid, set ke 1
                txtPenumpang.Text = "1";
                UpdatePenumpangDisplay(1);
            }

            // Update button states dengan maksimal baru
            UpdatePenumpangButtonStates(maksimalPenumpang);
            
            // Tampilkan notifikasi kepada user
            string jenisKendaraanText = GetJenisKendaraanText(jenisKendaraanIndex);
            System.Diagnostics.Debug.WriteLine($"[HomePage] Jenis kendaraan dipilih: {jenisKendaraanText}, Maks penumpang: {maksimalPenumpang}");
        }

        /// <summary>
        /// Update status button +/- berdasarkan maksimal penumpang
        /// </summary>
        private void UpdatePenumpangButtonStates(int maksimalPenumpang)
        {
            if (txtPenumpang == null)
                return;

            if (int.TryParse(txtPenumpang.Text, out int value))
            {
                // Update popup button states jika tersedia
                UpdatePopupButtonStatesHome(value);
            }
        }

        /// <summary>
        /// Get text deskripsi jenis kendaraan
        /// </summary>
        private string GetJenisKendaraanText(int index)
        {
            var jenis = (JenisKendaraan)index;
            var specs = DetailKendaraan.GetSpecificationByJenis(jenis);
            return specs.Deskripsi;
        }

        /// <summary>
        /// Load jam yang tersedia dari database berdasarkan kriteria yang dipilih
        /// </summary>
        private void LoadAvailableJamAsync()
        {
            try
            {
                // Null check untuk semua controls
                if (cmbJam == null)
                    return;

                // SELALU tampilkan semua jam 00:00 - 23:00
                // Tidak peduli ada jadwal atau tidak, user bisa pilih jam apapun
                // Backend akan menampilkan jadwal yang >= jam yang dipilih
                cmbJam.Items.Clear();
                cmbJam.Items.Add(new ComboBoxItem { Content = "Pilih Jam", Tag = null });
                
                for (int i = 0; i < 24; i++)
                {
                    cmbJam.Items.Add(new ComboBoxItem 
                    { 
                        Content = $"{i:D2}:00",
                        Tag = i 
                    });
                }

                cmbJam.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading jam: {ex.Message}");
                // Fallback ke semua jam jika error
                cmbJam.Items.Clear();
                cmbJam.Items.Add(new ComboBoxItem { Content = "Pilih Jam" });
                for (int i = 0; i < 24; i++)
                {
                    cmbJam.Items.Add(new ComboBoxItem { Content = $"{i:D2}:00", Tag = i });
                }
                cmbJam.SelectedIndex = 0;
            }
        }
    }
}