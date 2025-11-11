using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TiketLaut.Services;
using TiketLaut.Models;
using TiketLaut.Views.Components;
using TiketLaut.Helpers;

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
        private Button? _selectedVehicleButtonHome; // Track selected vehicle button for highlight
        private List<Pelabuhan> _pelabuhans = new List<Pelabuhan>(); // Store pelabuhan data
        
        // Carousel background images
        private readonly List<string> _backgroundImages = new List<string>
        {
            "/Views/Assets/Images/bekgron.png",
            "/Views/Assets/Images/bg2.png",    // Ganti dengan nama file gambar Anda
            "/Views/Assets/Images/bg3.jpg",    // Ganti dengan nama file gambar Anda
            "/Views/Assets/Images/bg4.jpg"     // Tambahkan sebanyak yang diinginkan
        };
        private int _currentImageIndex = 0;
        private DispatcherTimer? _carouselTimer;
        private DispatcherTimer? _clockTimer;

        // Constructor default (untuk pertama kali buka app)
        public HomePage()
        {
            InitializeComponent();
            _jadwalService = new JadwalService();
            
            // Enable zoom functionality
            ZoomHelper.EnableZoom(this);
            
            // Set default penumpang value after controls are initialized
            txtPenumpang.Text = "1";
            
            SetNavbarVisibility();
            LoadDataAsync();
            
            // Initialize carousel and clock
            InitializeCarousel();
            InitializeClock();
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
                    // Store pelabuhan data
                    _pelabuhans = pelabuhans.ToList();

                    // Populate Pelabuhan Asal Popup
                    PopulatePelabuhanPopup(spPelabuhanAsalList, pelabuhans, true);

                    // Populate Pelabuhan Tujuan Popup
                    PopulatePelabuhanPopup(spPelabuhanTujuanList, pelabuhans, false);

                    // Set DatePicker default to today
                    dpTanggal.SelectedDate = DateTime.Today;
                }
                else
                {
                    CustomDialog.ShowWarning(
                        "Peringatan",
                        "Tidak dapat memuat data pelabuhan dari database.\nPastikan koneksi database aktif.");
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Terjadi kesalahan saat memuat data:\n\n{ex.Message}");
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
            var result = CustomDialog.ShowQuestion(
                "Konfirmasi",
                "Apakah Anda yakin ingin logout?",
                CustomDialog.DialogButtons.YesNo);

            if (result == true)
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
                if (!int.TryParse(txtPelabuhanAsal.Text, out int pelabuhanAsalId) || pelabuhanAsalId <= 0)
                {
                    CustomDialog.ShowWarning("Peringatan", "Silakan pilih Pelabuhan Asal!");
                    return;
                }

                // Validasi Pelabuhan Tujuan
                if (!int.TryParse(txtPelabuhanTujuan.Text, out int pelabuhanTujuanId) || pelabuhanTujuanId <= 0)
                {
                    CustomDialog.ShowWarning("Peringatan", "Silakan pilih Pelabuhan Tujuan!");
                    return;
                }

                // Validasi Kelas Layanan
                if (string.IsNullOrEmpty(txtKelasLayanan.Text))
                {
                    CustomDialog.ShowWarning("Peringatan", "Silakan pilih Kelas Layanan!");
                    return;
                }

                // Validasi Tanggal
                if (!dpTanggal?.SelectedDate.HasValue ?? true)
                {
                    CustomDialog.ShowWarning("Peringatan", "Silakan pilih Tanggal!");
                    return;
                }

                // Validasi Jenis Kendaraan (harus sebelum validasi penumpang)
                if (!int.TryParse(txtVehicle.Text, out int jenisKendaraanId) || jenisKendaraanId < 0)
                {
                    CustomDialog.ShowWarning("Peringatan", "Silakan pilih Jenis Kendaraan!");
                    return;
                }

                // Dapatkan index jenis kendaraan
                int jenisKendaraanIndex = jenisKendaraanId;
                
                // Dapatkan maksimal penumpang untuk jenis kendaraan yang dipilih
                int maksimalPenumpang = DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanIndex);

                // Validasi Penumpang
                if (!int.TryParse(txtPenumpang.Text, out int jumlahPenumpangInput) || jumlahPenumpangInput < 1)
                {
                    CustomDialog.ShowWarning("Peringatan", "Jumlah penumpang minimal adalah 1!");
                    return;
                }

                // Validasi penumpang tidak melebihi maksimal untuk jenis kendaraan
                if (jumlahPenumpangInput > maksimalPenumpang)
                {
                    string jenisKendaraanText = GetJenisKendaraanText(jenisKendaraanIndex);
                    CustomDialog.ShowWarning(
                        "Peringatan",
                        $"Jumlah penumpang untuk {jenisKendaraanText} maksimal {maksimalPenumpang} orang!");
                    return;
                }

                // Show loading
                btnCariJadwal.IsEnabled = false;
                btnCariJadwal.Content = "Mencari jadwal...";

                // Get pelabuhan objects from stored list
                var pelabuhanAsal = _pelabuhans.FirstOrDefault(p => p.pelabuhan_id == pelabuhanAsalId);
                var pelabuhanTujuan = _pelabuhans.FirstOrDefault(p => p.pelabuhan_id == pelabuhanTujuanId);
                
                // Double check null
                if (pelabuhanAsal == null || pelabuhanTujuan == null)
                {
                    CustomDialog.ShowError("Error", "Pelabuhan tidak valid!");
                    btnCariJadwal.IsEnabled = true;
                    btnCariJadwal.Content = "Cari Jadwal";
                    return;
                }

                // Create PelabuhanComboBoxItem objects for compatibility
                var pelabuhanAsalItem = new PelabuhanComboBoxItem
                {
                    Id = pelabuhanAsal.pelabuhan_id,
                    DisplayText = $"{pelabuhanAsal.nama_pelabuhan} ({pelabuhanAsal.kota})"
                };

                var pelabuhanTujuanItem = new PelabuhanComboBoxItem
                {
                    Id = pelabuhanTujuan.pelabuhan_id,
                    DisplayText = $"{pelabuhanTujuan.nama_pelabuhan} ({pelabuhanTujuan.kota})"
                };
                
                var kelasLayanan = txtKelasLayanan.Text;

                // Parse jumlah penumpang dari TextBox
                int jumlahPenumpang = int.Parse(txtPenumpang.Text);

                // Parse tanggal keberangkatan (from dpTanggal DatePicker)
#pragma warning disable CS8602
                DateTime? tanggalKeberangkatan = dpTanggal.SelectedDate;
#pragma warning restore CS8602

                // Parse jam keberangkatan (optional)
                int? jamKeberangkatan = null;
                if (!string.IsNullOrEmpty(txtJam.Text))
                {
                    if (int.TryParse(txtJam.Text, out int jam))
                    {
                        jamKeberangkatan = jam;
                    }
                }

                // Validasi pelabuhan asal dan tujuan tidak sama
                if (pelabuhanAsalItem.Id == pelabuhanTujuanItem.Id)
                {
                    CustomDialog.ShowWarning(
                        "Peringatan",
                        "Pelabuhan asal dan tujuan tidak boleh sama!");
                    return;
                }

                // Search jadwal dari database
                var jadwals = await _jadwalService.SearchJadwalAsync(
                    pelabuhanAsalItem.Id,
                    pelabuhanTujuanItem.Id,
                    kelasLayanan ?? "Reguler",
                    tanggalKeberangkatan,
                    jenisKendaraanIndex,
                    jamKeberangkatan
                );

                if (jadwals == null || !jadwals.Any())
                {
                    CustomDialog.ShowInfo(
                        "Jadwal Tidak Ditemukan",
                        "Tidak ditemukan jadwal yang sesuai dengan kriteria pencarian Anda.\n\nSilakan coba dengan kriteria lain atau pilih tanggal berbeda.");
                    return;
                }

                // Buat search criteria untuk dikirim ke ScheduleWindow
                var searchCriteria = new SearchCriteria
                {
                    PelabuhanAsalId = pelabuhanAsalItem!.Id,
                    PelabuhanTujuanId = pelabuhanTujuanItem!.Id,
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
                    CustomDialog.ShowError("Error", $"Terjadi kesalahan saat mencari jadwal:\n\n{ex.Message}");
                }
            }
            finally
            {
                // Restore button state
                btnCariJadwal.IsEnabled = true;
                btnCariJadwal.Content = "Cari Jadwal";
            }
        }

        /// <summary>
        /// OBSOLETE: Old ComboBox handler (not used anymore)
        /// </summary>
        private void cmbJenisKendaraan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No longer used - replaced by popup button system
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
                    CustomDialog.ShowInfo(
                        "Batas Maksimal",
                        $"Jumlah penumpang maksimal untuk jenis kendaraan ini adalah {maksimalPenumpang} orang.");
                }
            }
        }

        /// <summary>
        /// Mendapatkan maksimal penumpang berdasarkan kendaraan yang dipilih
        /// </summary>
        private int GetMaksimalPenumpangFromKendaraan()
        {
            if (txtVehicle == null || !int.TryParse(txtVehicle.Text, out int jenisKendaraanId) || jenisKendaraanId < 0)
                return 10; // Default maksimal jika belum pilih kendaraan

            int jenisKendaraanIndex = jenisKendaraanId;
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
        /// Event handler untuk swap/tukar pelabuhan asal dan tujuan
        /// </summary>
        private void BtnSwapPelabuhan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Simpan value yang dipilih saat ini
                string tempAsalId = txtPelabuhanAsal.Text;
                string tempAsalDisplay = string.Join("", txtPelabuhanAsalDisplay.Inlines.OfType<Run>().Select(r => r.Text));
                
                string tempTujuanId = txtPelabuhanTujuan.Text;
                string tempTujuanDisplay = string.Join("", txtPelabuhanTujuanDisplay.Inlines.OfType<Run>().Select(r => r.Text));

                // Tukar values
                txtPelabuhanAsal.Text = tempTujuanId;
                txtPelabuhanAsalDisplay.Inlines.Clear();
                if (!string.IsNullOrEmpty(tempTujuanDisplay) && tempTujuanDisplay != "Pilih Pelabuhan Asal")
                {
                    txtPelabuhanAsalDisplay.Inlines.Add(new Run(tempTujuanDisplay) { Foreground = Brushes.Black });
                }
                else
                {
                    txtPelabuhanAsalDisplay.Inlines.Add(new Run("Pilih Pelabuhan Asal") { Foreground = (Brush)new BrushConverter().ConvertFrom("#666666")! });
                }

                txtPelabuhanTujuan.Text = tempAsalId;
                txtPelabuhanTujuanDisplay.Inlines.Clear();
                if (!string.IsNullOrEmpty(tempAsalDisplay) && tempAsalDisplay != "Pilih Pelabuhan Tujuan")
                {
                    txtPelabuhanTujuanDisplay.Inlines.Add(new Run(tempAsalDisplay) { Foreground = Brushes.Black });
                }
                else
                {
                    txtPelabuhanTujuanDisplay.Inlines.Add(new Run("Pilih Pelabuhan Tujuan") { Foreground = (Brush)new BrushConverter().ConvertFrom("#666666")! });
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Terjadi kesalahan saat menukar pelabuhan: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler ketika button tanggal diklik
        /// </summary>
        private void BtnTanggal_Click(object sender, RoutedEventArgs e)
        {
            if (dpTanggal != null)
            {
                // Pastikan DatePicker visible sebentar untuk bisa membuka calendar
                dpTanggal.Visibility = Visibility.Visible;
                dpTanggal.IsDropDownOpen = true;
                // Akan di-collapsed lagi setelah calendar tertutup di event CalendarClosed
            }
        }

        /// <summary>
        /// Event handler saat tanggal dipilih
        /// </summary>
        private void DpTanggal_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpTanggal.SelectedDate.HasValue && txtTanggalDisplay != null)
            {
                DateTime selectedDate = dpTanggal.SelectedDate.Value;
                
                // Format: "1/11/2025" (tanpa nama hari)
                string formattedDate = $"{selectedDate.Day}/{selectedDate.Month}/{selectedDate.Year}";
                txtTanggalDisplay.Text = formattedDate;
            }

            if (IsLoaded) // Hanya jalankan jika window sudah fully loaded
                LoadAvailableJamAsync();
        }

        /// <summary>
        /// Event handler ketika calendar dibuka
        /// </summary>
        private void DpTanggal_CalendarOpened(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Event handler ketika calendar ditutup
        /// </summary>
        private void DpTanggal_CalendarClosed(object sender, RoutedEventArgs e)
        {
            
            // Sembunyikan DatePicker lagi setelah calendar ditutup
            if (dpTanggal != null)
            {
                dpTanggal.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// OBSOLETE: Event handler untuk ComboBox (diganti dengan Popup)
        /// </summary>
        private void CmbJenisKendaraan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No longer used - replaced by popup button system
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
                if (spJamList == null)
                    return;

                // Clear existing items except the header
                var itemsToRemove = spJamList.Children.OfType<Button>().ToList();
                foreach (var item in itemsToRemove)
                {
                    spJamList.Children.Remove(item);
                }

                // SELALU tampilkan semua jam 00:00 - 23:00
                // Tidak peduli ada jadwal atau tidak, user bisa pilih jam apapun
                // Backend akan menampilkan jadwal yang >= jam yang dipilih
                for (int i = 0; i < 24; i++)
                {
                    var button = new Button
                    {
                        Tag = i.ToString(),
                        BorderThickness = new Thickness(0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Padding = new Thickness(10, 8, 10, 8),
                        Margin = new Thickness(0, 2, 0, 2),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Center
                    };

                    // Set button style
                    var style = new Style(typeof(Button));
                    style.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.Transparent));
                    
                    var template = new ControlTemplate(typeof(Button));
                    var factory = new FrameworkElementFactory(typeof(Border));
                    factory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                    factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
                    factory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                    
                    var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                    factory.AppendChild(contentPresenter);
                    template.VisualTree = factory;
                    style.Setters.Add(new Setter(Button.TemplateProperty, template));
                    
                    // Hover trigger
                    var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
                    trigger.Setters.Add(new Setter(Button.BackgroundProperty, (Brush)(new BrushConverter().ConvertFrom("#74c0f3ff") ?? Brushes.Transparent)));
                    style.Triggers.Add(trigger);
                    
                    button.Style = style;

                    // Button content
                    var textBlock = new TextBlock
                    {
                        Text = $"{i:D2}:00",
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.Black,
                        TextAlignment = TextAlignment.Center
                    };
                    
                    button.Content = textBlock;
                    button.Click += BtnJamOption_Click;

                    spJamList.Children.Add(button);
                }
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Event handler untuk button toggle popup kendaraan
        /// </summary>
        private void BtnVehicle_Click(object sender, RoutedEventArgs e)
        {
            if (popupVehicleHome != null)
            {
                popupVehicleHome.IsOpen = !popupVehicleHome.IsOpen;
            }
        }

        /// <summary>
        /// Event handler ketika popup kendaraan dibuka - maintain highlight state
        /// </summary>
        private void PopupVehicleHome_Opened(object sender, EventArgs e)
        {
            // Maintain selected button highlight when popup reopens
            if (_selectedVehicleButtonHome != null)
            {
                var blueBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#b7def8ff")); // Soft blue
                _selectedVehicleButtonHome.SetValue(Button.BackgroundProperty, blueBrush);
            }
        }

        /// <summary>
        /// Event handler untuk memilih opsi kendaraan
        /// </summary>
        private void BtnVehicleOptionHome_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagValue)
            {
                // Parse tag format: "ID|Name"
                var parts = tagValue.Split('|');
                if (parts.Length == 2 && int.TryParse(parts[0], out int jenisKendaraanId))
                {
                    string vehicleName = parts[1];
                    
                    // Reset previous selected button - clear local value to allow style to work
                    if (_selectedVehicleButtonHome != null)
                    {
                        _selectedVehicleButtonHome.ClearValue(Button.BackgroundProperty);
                    }
                    
                    // Set local value on current button - use soft blue color
                    var blueBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#b7def8ff")); // Soft blue - same as hover
                    button.SetValue(Button.BackgroundProperty, blueBrush);
                    
                    // Save current selected button
                    _selectedVehicleButtonHome = button;
                    
                    // Update display text and hidden value
                    UpdateVehicleDisplay(jenisKendaraanId, vehicleName);
                    txtVehicle.Text = jenisKendaraanId.ToString();

                    // Get max passengers for this vehicle
                    int maksimalPenumpang = DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanId);
                    
                    // Check current passenger count
                    if (int.TryParse(txtPenumpang.Text, out int currentPenumpang))
                    {
                        // If current passengers exceed max, adjust to max
                        if (currentPenumpang > maksimalPenumpang)
                        {
                            int newValue = maksimalPenumpang;
                            txtPenumpang.Text = newValue.ToString();
                            txtPenumpangDisplay.Text = $"{newValue}    Penumpang";
                            
                            // Show info message
                            CustomDialog.ShowInfo(
                                "Info",
                                $"Jumlah penumpang disesuaikan menjadi {newValue} (maksimal untuk kendaraan ini).");
                        }
                    }

                    // Close popup
                    if (popupVehicleHome != null)
                    {
                        popupVehicleHome.IsOpen = false;
                    }
                }
            }
        }
        /// <summary>
        /// Helper method to find Border element inside button template
        /// </summary>
        private Border? FindChildBorder(DependencyObject parent)
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Border border)
                {
                    return border;
                }
                
                // Recursively search in children
                var result = FindChildBorder(child);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }

        private void UpdateVehicleDisplay(int id, string vehicleName)
        {
            // Clear existing text
            txtVehicleDisplay.Inlines.Clear();

            // Create color for golongan
            var golonganBrush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#00B4B5"));

            // Map ID to golongan
            Dictionary<int, string> golonganMap = new Dictionary<int, string>
            {
                { 0, "" },  // Pejalan Kaki - no golongan
                { 1, " (Golongan I)" },
                { 2, " (Golongan II)" },
                { 3, " (Golongan III)" },
                { 4, " (Golongan IVA)" },
                { 5, " (Golongan IVB)" },
                { 6, " (Golongan VA)" },
                { 7, " (Golongan VB)" },
                { 8, " (Golongan VIA)" },
                { 9, " (Golongan VIB)" },
                { 10, " (Golongan VII)" },
                { 11, " (Golongan VIII)" },
                { 12, " (Golongan IX)" }
            };

            // Create runs with different colors
            var nameRun = new Run(vehicleName)
            {
                Foreground = Brushes.Black
            };

            txtVehicleDisplay.Inlines.Add(nameRun);

            // Add golongan if exists
            if (golonganMap.ContainsKey(id) && !string.IsNullOrEmpty(golonganMap[id]))
            {
                var golonganRun = new Run(golonganMap[id])
                {
                    Foreground = golonganBrush
                };
                txtVehicleDisplay.Inlines.Add(golonganRun);
            }
        }

        // ========================================
        // PELABUHAN POPUP FUNCTIONS
        // ========================================

        private void PopulatePelabuhanPopup(StackPanel container, IEnumerable<Pelabuhan> pelabuhans, bool isAsal)
        {
            // Clear existing items except the header
            var itemsToRemove = container.Children.OfType<Button>().ToList();
            foreach (var item in itemsToRemove)
            {
                container.Children.Remove(item);
            }

            foreach (var pelabuhan in pelabuhans)
            {
                var button = new Button
                {
                    Tag = $"{pelabuhan.pelabuhan_id}|{pelabuhan.nama_pelabuhan}|{pelabuhan.kota}",
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 2, 0, 2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };

                // Set button style
                var style = new Style(typeof(Button));
                style.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.Transparent));
                
                var template = new ControlTemplate(typeof(Button));
                var factory = new FrameworkElementFactory(typeof(Border));
                factory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
                factory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
                
                var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                factory.AppendChild(contentPresenter);
                template.VisualTree = factory;
                style.Setters.Add(new Setter(Button.TemplateProperty, template));
                
                // Hover trigger
                var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
                trigger.Setters.Add(new Setter(Button.BackgroundProperty, (Brush)(new BrushConverter().ConvertFrom("#74c0f3ff") ?? Brushes.Transparent)));
                style.Triggers.Add(trigger);
                
                button.Style = style;

                // Button content
                var stackPanel = new StackPanel();
                
                var titleBlock = new TextBlock
                {
                    Text = pelabuhan.nama_pelabuhan,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black
                };
                
                var subtitleBlock = new TextBlock
                {
                    Text = $"Kota: {pelabuhan.kota}",
                    FontSize = 11,
                    Foreground = (Brush)(new BrushConverter().ConvertFrom("#64748B") ?? Brushes.Gray),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                
                stackPanel.Children.Add(titleBlock);
                stackPanel.Children.Add(subtitleBlock);
                button.Content = stackPanel;

                // Add event handler
                if (isAsal)
                {
                    button.Click += BtnPelabuhanAsalOption_Click;
                }
                else
                {
                    button.Click += BtnPelabuhanTujuanOption_Click;
                }

                container.Children.Add(button);
            }
        }

        private void BtnPelabuhanAsal_Click(object sender, RoutedEventArgs e)
        {
            popupPelabuhanAsal.IsOpen = !popupPelabuhanAsal.IsOpen;
        }

        private void BtnPelabuhanAsalOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                var parts = tag.Split('|');
                if (parts.Length == 3)
                {
                    var id = parts[0];
                    var nama = parts[1];
                    var kota = parts[2];

                    txtPelabuhanAsal.Text = id;
                    txtPelabuhanAsalDisplay.Inlines.Clear();
                    txtPelabuhanAsalDisplay.Inlines.Add(new Run($"{nama} ({kota})") { Foreground = Brushes.Black });
                    
                    popupPelabuhanAsal.IsOpen = false;

                    // Load available jam setelah pelabuhan dipilih
                    if (IsLoaded)
                        LoadAvailableJamAsync();
                }
            }
        }

        private void BtnPelabuhanTujuan_Click(object sender, RoutedEventArgs e)
        {
            popupPelabuhanTujuan.IsOpen = !popupPelabuhanTujuan.IsOpen;
        }

        private void BtnPelabuhanTujuanOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                var parts = tag.Split('|');
                if (parts.Length == 3)
                {
                    var id = parts[0];
                    var nama = parts[1];
                    var kota = parts[2];

                    txtPelabuhanTujuan.Text = id;
                    txtPelabuhanTujuanDisplay.Inlines.Clear();
                    txtPelabuhanTujuanDisplay.Inlines.Add(new Run($"{nama} ({kota})") { Foreground = Brushes.Black });
                    
                    popupPelabuhanTujuan.IsOpen = false;

                    // Load available jam setelah pelabuhan dipilih
                    if (IsLoaded)
                        LoadAvailableJamAsync();
                }
            }
        }

        // ========================================
        // KELAS LAYANAN POPUP FUNCTIONS
        // ========================================

        private void BtnKelasLayanan_Click(object sender, RoutedEventArgs e)
        {
            popupKelasLayanan.IsOpen = !popupKelasLayanan.IsOpen;
        }

        private void BtnKelasOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string kelas)
            {
                txtKelasLayanan.Text = kelas;
                txtKelasLayananDisplay.Inlines.Clear();
                txtKelasLayananDisplay.Inlines.Add(new Run(kelas) { Foreground = Brushes.Black });
                
                popupKelasLayanan.IsOpen = false;

                // Load available jam setelah kelas layanan dipilih
                if (IsLoaded)
                    LoadAvailableJamAsync();
            }
        }

        // ========================================
        // JAM POPUP FUNCTIONS
        // ========================================

        private void BtnJam_Click(object sender, RoutedEventArgs e)
        {
            popupJam.IsOpen = !popupJam.IsOpen;
        }

        private void BtnJamOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string jam)
            {
                txtJam.Text = jam;
                txtJamDisplay.Inlines.Clear();
                txtJamDisplay.Inlines.Add(new Run($"{int.Parse(jam):D2}:00") { Foreground = Brushes.Black });
                
                popupJam.IsOpen = false;
            }
        }
        
        // ========================================
        // BACKGROUND CAROUSEL FUNCTIONS
        // ========================================
        
        private void InitializeCarousel()
        {
            _carouselTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5) // Ganti gambar setiap 5 detik
            };
            _carouselTimer.Tick += CarouselTimer_Tick;
            _carouselTimer.Start();
        }
        
        private void CarouselTimer_Tick(object? sender, EventArgs e)
        {
            // Set next image index
            _currentImageIndex = (_currentImageIndex + 1) % _backgroundImages.Count;
            
            // Set next image source
            nextImage.Source = new BitmapImage(new Uri(_backgroundImages[_currentImageIndex], UriKind.Relative));
            
            // Get window width for animation
            double windowWidth = this.ActualWidth;
            if (windowWidth <= 0) windowWidth = 1920; // Fallback width
            
            // Position next image off-screen to the right
            var nextTransform = (TranslateTransform)nextImage.RenderTransform;
            nextTransform.X = windowWidth;
            
            // Animate current image sliding out to the left
            var currentTransform = (TranslateTransform)currentImage.RenderTransform;
            var slideOutAnim = new DoubleAnimation
            {
                From = 0,
                To = -windowWidth,
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            // Animate next image sliding in from the right
            var slideInAnim = new DoubleAnimation
            {
                From = windowWidth,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.8),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            slideInAnim.Completed += (s, args) =>
            {
                // After animation completes, swap the images
                currentImage.Source = nextImage.Source;
                currentTransform.X = 0;
                nextTransform.X = windowWidth;
            };
            
            // Start animations
            currentTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnim);
            nextTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnim);
        }
        
        // ========================================
        // DATE TIME DISPLAY FUNCTIONS
        // ========================================
        
        private void InitializeClock()
        {
            UpdateDateTime();
            
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Update setiap 1 detik
            };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
        }
        
        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateDateTime();
        }
        
        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            
            // Format: Hari, DD MMMM YYYY
            var culture = new System.Globalization.CultureInfo("id-ID");
            txtCurrentDate.Text = now.ToString("dddd, dd MMMM yyyy", culture);
            
            // Format: HH:mm:ss WIB
            txtCurrentTime.Text = now.ToString("HH:mm:ss") + " WIB";
        }
        
        // ========================================
        // HELP BUTTON FUNCTIONS
        // ========================================
        
        private void HelpButton_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowHelpDialog();
        }
        
        private void HelpButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Hover effect: scale up slightly and change shadow
            if (sender is Border border)
            {
                var scaleTransform = new ScaleTransform(1.05, 1.05);
                border.RenderTransform = scaleTransform;
                border.RenderTransformOrigin = new Point(0.5, 0.5);
                
                // Animate scale
                var scaleAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 1.05,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                
                // Change shadow to be more prominent
                var shadow = new DropShadowEffect
                {
                    BlurRadius = 25,
                    ShadowDepth = 8,
                    Opacity = 0.3,
                    Color = Colors.Black
                };
                border.Effect = shadow;
            }
        }
        
        private void HelpButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Reset to normal state
            if (sender is Border border)
            {
                var scaleTransform = border.RenderTransform as ScaleTransform;
                if (scaleTransform != null)
                {
                    var scaleAnimation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                    };
                    
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                }
                
                // Reset shadow to original
                var shadow = (DropShadowEffect)Resources["CardShadow"];
                border.Effect = shadow;
            }
        }
        
        private void ShowHelpDialog()
        {
            var dialog = new Window
            {
                Width = 550,
                Height = 550,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.Transparent,
                FontFamily = new FontFamily("Plus Jakarta Sans"),
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true
            };
            
            // Outer border for shadow effect
            var shadowBorder = new Border
            {
                Margin = new Thickness(30), // Space for shadow
                Effect = new DropShadowEffect
                {
                    BlurRadius = 30,
                    ShadowDepth = 0,
                    Opacity = 0.3,
                    Color = Colors.Black
                }
            };
            
            // Inner border with rounded corners
            var mainBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(30)
            };
            
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Header
            var headerBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D")),
                Padding = new Thickness(25, 20, 25, 20),
                CornerRadius = new CornerRadius(30, 30, 0, 0)
            };
            
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            var headerStack = new StackPanel();
            
            var headerTitle = new TextBlock
            {
                Text = "📞 Hubungi Kami",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            
            var headerSubtitle = new TextBlock
            {
                Text = "Kami siap membantu Anda 24/7",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                Margin = new Thickness(0, 5, 0, 0)
            };
            
            headerStack.Children.Add(headerTitle);
            headerStack.Children.Add(headerSubtitle);
            Grid.SetColumn(headerStack, 0);
            
            // Close button in header
            var closeHeaderButton = new Button
            {
                Content = "✕",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Width = 40,
                Height = 40,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(0)
            };
            
            closeHeaderButton.Click += (s, e) => dialog.Close();
            
            // Style for close button
            var closeButtonTemplate = new ControlTemplate(typeof(Button));
            var closeBorder = new FrameworkElementFactory(typeof(Border));
            closeBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            closeBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(20));
            closeBorder.SetValue(Border.WidthProperty, 40.0);
            closeBorder.SetValue(Border.HeightProperty, 40.0);
            
            var closeContentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            closeContentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            closeContentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            
            closeBorder.AppendChild(closeContentPresenter);
            closeButtonTemplate.VisualTree = closeBorder;
            closeHeaderButton.Template = closeButtonTemplate;
            
            var closeButtonStyle = new Style(typeof(Button));
            var closeHoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            closeHoverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromArgb(50, 255, 255, 255))));
            closeButtonStyle.Triggers.Add(closeHoverTrigger);
            closeHeaderButton.Style = closeButtonStyle;
            
            Grid.SetColumn(closeHeaderButton, 1);
            
            headerGrid.Children.Add(headerStack);
            headerGrid.Children.Add(closeHeaderButton);
            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            
            // Content
            var contentScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(25, 20, 25, 20)
            };
            
            var contentStack = new StackPanel();
            
            // Customer Service
            var csTitle = new TextBlock
            {
                Text = "🎧 Customer Service",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#011C4B")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            contentStack.Children.Add(csTitle);
            
            AddContactInfo(contentStack, "☎️", "Telepon", "+62 812-3456-7890");
            AddContactInfo(contentStack, "📱", "WhatsApp", "+62 812-3456-7890");
            AddContactInfo(contentStack, "📧", "Email", "support@tiketlaut.com");
            AddContactInfo(contentStack, "🏢", "Email Bisnis", "business@tiketlaut.com");
            
            // Office
            var officeTitle = new TextBlock
            {
                Text = "🏢 Kantor Pusat",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#011C4B")),
                Margin = new Thickness(0, 20, 0, 10)
            };
            contentStack.Children.Add(officeTitle);
            
            var addressText = new TextBlock
            {
                Text = "Jl. Pelabuhan Raya No. 123\nKota Pelabuhan, Indonesia 12345",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#495057")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            };
            contentStack.Children.Add(addressText);
            
            var operatingHours = new TextBlock
            {
                Text = "⏰ Senin - Minggu: 08:00 - 20:00 WIB",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C757D")),
                FontStyle = FontStyles.Italic
            };
            contentStack.Children.Add(operatingHours);
            
            contentScroll.Content = contentStack;
            Grid.SetRow(contentScroll, 1);
            
            // Footer Button
            var footerBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA")),
                Padding = new Thickness(25, 15, 25, 15),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DEE2E6")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                CornerRadius = new CornerRadius(0, 0, 30, 30)
            };
            
            var closeButton = new Button
            {
                Content = "Tutup",
                Padding = new Thickness(30, 10, 30, 10),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00658D")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            closeButton.Click += (s, e) => dialog.Close();
            
            var buttonTemplate = new ControlTemplate(typeof(Button));
            var buttonBorder = new FrameworkElementFactory(typeof(Border));
            buttonBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            buttonBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            buttonBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
            
            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            
            buttonBorder.AppendChild(contentPresenter);
            buttonTemplate.VisualTree = buttonBorder;
            closeButton.Template = buttonTemplate;
            
            var buttonStyle = new Style(typeof(Button));
            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#267DAE"))));
            buttonStyle.Triggers.Add(hoverTrigger);
            closeButton.Style = buttonStyle;
            
            footerBorder.Child = closeButton;
            Grid.SetRow(footerBorder, 2);
            
            mainGrid.Children.Add(headerBorder);
            mainGrid.Children.Add(contentScroll);
            mainGrid.Children.Add(footerBorder);
            
            // Set mainGrid as child of mainBorder
            mainBorder.Child = mainGrid;
            
            // Set mainBorder as child of shadowBorder
            shadowBorder.Child = mainBorder;
            
            dialog.Content = shadowBorder;
            dialog.ShowDialog();
        }
        
        private void AddContactInfo(StackPanel parent, string icon, string label, string value)
        {
            var itemStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            
            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var infoStack = new StackPanel();
            
            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6C757D")),
                FontWeight = FontWeights.Medium
            };
            
            var valueText = new TextBlock
            {
                Text = value,
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#011C4B")),
                FontWeight = FontWeights.SemiBold
            };
            
            infoStack.Children.Add(labelText);
            infoStack.Children.Add(valueText);
            
            itemStack.Children.Add(iconText);
            itemStack.Children.Add(infoStack);
            
            parent.Children.Add(itemStack);
        }
    }
}