using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using TiketLaut.Models;
using TiketLaut.Services;
using TiketLaut.Views;
using TiketLaut.Views.Components;
using TiketLaut.Helpers;

namespace TiketLaut.Views
{
    public partial class ScheduleWindow : Window
    {
        public ObservableCollection<ScheduleItem> ScheduleItems { get; set; } = new ObservableCollection<ScheduleItem>();
        private List<Jadwal>? _jadwals;
        private SearchCriteria? _searchCriteria;
        private readonly JadwalService _jadwalService;
        private Button? _selectedVehicleButton;
        
         // Constructor 1 (default)
        public ScheduleWindow()
        {
            InitializeComponent();
            _jadwalService = new JadwalService();
            
            // Enable zoom functionality
            ZoomHelper.EnableZoom(this);

            SetNavbarVisibility();

            // Check data pencarian ada di session ga
            if (SessionManager.LastSearchCriteria != null && SessionManager.LastSearchResults != null)
            {
                // pake data dari session
                _searchCriteria = SessionManager.LastSearchCriteria;
                _jadwals = SessionManager.LastSearchResults;

                // load dari session
                LoadFilterDropdownsAsync();

                // Load jadwal dari database 
                LoadScheduleFromDatabase();
            }
            else
            {
                // Load dropdown saja, user perlu cari manual
                LoadFilterDropdownsAsync();
                
                // Show info message
                CustomDialog.ShowInfo(
                    "Info",
                    "Silakan gunakan form pencarian untuk menemukan jadwal keberangkatan.");
            }
        }


        // Constructor baru dgn parameter dari DB
        public ScheduleWindow(List<Jadwal> jadwals, SearchCriteria searchCriteria)
        {
            InitializeComponent();
            _jadwalService = new JadwalService();
            _jadwals = jadwals;
            _searchCriteria = searchCriteria;

            SetNavbarVisibility();

            // Populate filter dropdown dengan data user
            LoadFilterDropdownsAsync();

            LoadScheduleFromDatabase();
        }


        /// <summary>
        /// Set navbar visibility based on user login status
        /// </summary>
        private void SetNavbarVisibility()
        {
            if (SessionManager.IsLoggedIn && SessionManager.CurrentUser != null)
            {
                // User sudah login pake NavbarPostLogin
                navbarPreLogin.Visibility = Visibility.Collapsed;
                navbarPostLogin.Visibility = Visibility.Visible;
                navbarPostLogin.SetUserInfo(SessionManager.CurrentUser.nama);
            }
            else
            {
                // User belum login pake NavbarPreLogin
                navbarPreLogin.Visibility = Visibility.Visible;
                navbarPostLogin.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Load dan populate semua dropdown filter dengan data lengkap dari database
        /// </summary>
        private async void LoadFilterDropdownsAsync()
        {
            try
            {
                // Load semua pelabuhan untuk dropdown
                var pelabuhans = await _jadwalService.GetAllPelabuhanAsync();

                if (pelabuhans.Any())
                {
                    // Populate Pelabuhan Asal dengan semua pilihan
                    cmbFilterFrom.Items.Clear();
                    cmbFilterFrom.Items.Add(new PelabuhanComboBoxItem
                    {
                        Id = 0,
                        DisplayText = "Pilih Pelabuhan Asal"
                    });

                    foreach (var pelabuhan in pelabuhans)
                    {
                        cmbFilterFrom.Items.Add(new PelabuhanComboBoxItem
                        {
                            Id = pelabuhan.pelabuhan_id,
                            DisplayText = $"{pelabuhan.nama_pelabuhan} ({pelabuhan.kota})"
                        });
                    }

                    // Set selected berdasarkan search criteria
                    if (_searchCriteria != null)
                    {
                        var selectedAsal = cmbFilterFrom.Items.Cast<PelabuhanComboBoxItem>()
                            .FirstOrDefault(item => item.Id == _searchCriteria.PelabuhanAsalId);
                        if (selectedAsal != null)
                        {
                            cmbFilterFrom.SelectedItem = selectedAsal;
                        }
                        else
                        {
                            cmbFilterFrom.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        cmbFilterFrom.SelectedIndex = 0;
                    }

                    // Populate Pelabuhan Tujuan dengan semua pilihan
                    cmbFilterTo.Items.Clear();
                    cmbFilterTo.Items.Add(new PelabuhanComboBoxItem
                    {
                        Id = 0,
                        DisplayText = "Pilih Pelabuhan Tujuan"
                    });

                    foreach (var pelabuhan in pelabuhans)
                    {
                        cmbFilterTo.Items.Add(new PelabuhanComboBoxItem
                        {
                            Id = pelabuhan.pelabuhan_id,
                            DisplayText = $"{pelabuhan.nama_pelabuhan} ({pelabuhan.kota})"
                        });
                    }

                    // Set selected berdasarkan search criteria
                    if (_searchCriteria != null)
                    {
                        var selectedTujuan = cmbFilterTo.Items.Cast<PelabuhanComboBoxItem>()
                            .FirstOrDefault(item => item.Id == _searchCriteria.PelabuhanTujuanId);
                        if (selectedTujuan != null)
                        {
                            cmbFilterTo.SelectedItem = selectedTujuan;
                        }
                        else
                        {
                            cmbFilterTo.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        cmbFilterTo.SelectedIndex = 0;
                    }
                }

                // Populate Tanggal
                PopulateDateFilter();

                // Populate Jam
                PopulateTimeFilter();

                // Populate Jenis Kendaraan
                PopulateVehicleFilter();

                // Populate Jumlah Penumpang
                PopulatePassengerFilter();
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Terjadi kesalahan saat memuat data filter:\n\n{ex.Message}");
            }
        }

        /// <summary>
        /// Set tanggal filter menggunakan DatePicker
        /// </summary>
        private void PopulateDateFilter()
        {
            // Di ScheduleWindow, user bisa pilih tanggal apa saja (termasuk tanggal lampau)
            // untuk filtering jadwal yang sudah ada di database
            // Jadi TIDAK set DisplayDateStart (biarkan null = unlimited)
            
            // Set selected berdasarkan search criteria
            if (_searchCriteria != null)
            {
                dpFilterDate.SelectedDate = _searchCriteria.TanggalKeberangkatan.Date;
            }
            else
            {
                dpFilterDate.SelectedDate = DateTime.Today;
            }
        }

        /// <summary>
        /// Populate dropdown jam (00:00 - 23:00 seperti HomePage)
        /// </summary>
        private void PopulateTimeFilter()
        {
            // Jam sudah di-define di XAML (00:00 - 23:00)
            // Set selected berdasarkan search criteria jika ada
            if (_searchCriteria != null && _searchCriteria.JamKeberangkatan.HasValue)
            {
                var jamKeberangkatan = _searchCriteria.JamKeberangkatan.Value;
                var jamText = $"{jamKeberangkatan:00}:00";
                
                // Cari item yang sesuai
                for (int i = 0; i < cmbFilterTime.Items.Count; i++)
                {
                    if (cmbFilterTime.Items[i] is ComboBoxItem item && 
                        item.Content?.ToString() == jamText)
                    {
                        cmbFilterTime.SelectedIndex = i;
                        return;
                    }
                }
            }
            
            // Default ke "Pilih Jam"
            cmbFilterTime.SelectedIndex = 0;
        }

        /// <summary>
        /// Populate filter jenis kendaraan dengan data dari search criteria
        /// </summary>
        private void PopulateVehicleFilter()
        {
            // Set selected berdasarkan search criteria
            if (_searchCriteria != null)
            {
                int jenisId = _searchCriteria.JenisKendaraanId;
                string vehicleText = jenisId switch
                {
                    0 => "Pejalan Kaki",
                    1 => "Sepeda",
                    2 => "Sepeda Motor (<500cc)",
                    3 => "Sepeda Motor (>500cc)",
                    4 => "Mobil Penumpang",
                    5 => "Truk Pickup",
                    6 => "Bus Sedang",
                    7 => "Truk Sedang",
                    8 => "Bus Besar",
                    9 => "Truk Besar",
                    10 => "Truk Tronton",
                    11 => "Truk Tronton (<16 meter)",
                    12 => "Truk Tronton (>16 meter)",
                    _ => "Sepeda Motor (>500cc)"
                };
                
                UpdateVehicleDisplay(jenisId, vehicleText);
                txtFilterVehicle.Text = jenisId.ToString();
            }
            else
            {
                UpdateVehicleDisplay(3, "Sepeda Motor (>500cc)");
                txtFilterVehicle.Text = "3";
            }
        }

        /// <summary>
        /// Set initial value untuk filter penumpang dengan tombol +/-
        /// </summary>
        private void PopulatePassengerFilter()
        {
            // Set initial value berdasarkan search criteria
            int initialValue = 1;
            if (_searchCriteria != null && _searchCriteria.JumlahPenumpang > 0)
            {
                initialValue = _searchCriteria.JumlahPenumpang;
            }

            txtFilterPenumpang.Text = initialValue.ToString();
            UpdateFilterPenumpangDisplay(initialValue);
        }

        /// <summary>
        /// Update display text untuk penumpang ("X Penumpang")
        /// </summary>
        private void UpdateFilterPenumpangDisplay(int count)
        {
            if (txtFilterPenumpangDisplay == null)
                return;

            // Update menggunakan Inlines karena TextBlock menggunakan Run elements
            txtFilterPenumpangDisplay.Inlines.Clear();
            txtFilterPenumpangDisplay.Inlines.Add(new Run(count.ToString()));
            txtFilterPenumpangDisplay.Inlines.Add(new Run(" "));
            txtFilterPenumpangDisplay.Inlines.Add(new Run("Penumpang"));
        }

        /// <summary>
        /// Mendapatkan maksimal penumpang berdasarkan kendaraan yang dipilih di filter
        /// </summary>
        private int GetMaksimalPenumpangFromFilterKendaraan()
        {
            if (txtFilterVehicle == null || string.IsNullOrEmpty(txtFilterVehicle.Text))
                return 100; // Default maksimal jika belum pilih kendaraan

            // txtFilterVehicle sekarang menyimpan ID (0-12)
            if (int.TryParse(txtFilterVehicle.Text, out int jenisKendaraanId))
            {
                return DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanId);
            }
            
            return 100;
        }

        /// <summary>
        /// OBSOLETE: Method lama cmbFilterVehicle_SelectionChanged (removed, sekarang menggunakan popup)
        /// </summary>

        /// <summary>
        /// Event handler untuk tombol plus penumpang
        /// </summary>
        private void BtnFilterPlusPenumpang_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtFilterPenumpang.Text, out int current))
            {
                // Dapatkan maksimal penumpang berdasarkan kendaraan yang dipilih
                int maksimalPenumpang = GetMaksimalPenumpangFromFilterKendaraan();
                
                if (current < maksimalPenumpang)
                {
                    current++;
                    txtFilterPenumpang.Text = current.ToString();
                    txtPopupPenumpang.Text = current.ToString();
                    UpdateFilterPenumpangDisplay(current);
                    
                    // Update button states in popup
                    UpdatePopupButtonStates(current);
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
        /// Event handler untuk tombol minus penumpang
        /// </summary>
        private void BtnFilterMinusPenumpang_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtFilterPenumpang.Text, out int current))
            {
                if (current > 1)
                {
                    current--;
                    txtFilterPenumpang.Text = current.ToString();
                    txtPopupPenumpang.Text = current.ToString();
                    UpdateFilterPenumpangDisplay(current);
                }
            }
        }

        /// <summary>
        /// Event handler ketika button tanggal diklik
        /// </summary>
        private void BtnFilterDate_Click(object sender, RoutedEventArgs e)
        {
            if (dpFilterDate != null)
            {
                // Pastikan DatePicker visible sebentar untuk bisa membuka calendar
                dpFilterDate.Visibility = Visibility.Visible;
                dpFilterDate.IsDropDownOpen = true;
                // Akan di-collapsed lagi setelah calendar tertutup di event CalendarClosed
            }
        }

        /// <summary>
        /// Event handler ketika TextBlock tanggal diklik (deprecated, diganti dengan BtnFilterDate_Click)
        /// </summary>
        private void TxtFilterDateDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (dpFilterDate != null)
            {
                dpFilterDate.IsDropDownOpen = true;
            }
        }

        /// <summary>
        /// Event handler ketika tanggal dipilih dari DatePicker
        /// </summary>
        private void DpFilterDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpFilterDate.SelectedDate.HasValue && txtFilterDateDisplay != null)
            {
                DateTime selectedDate = dpFilterDate.SelectedDate.Value;
                
                // Format: "Sab, 1 November 2025"
                string[] namaBulan = { "Januari", "Februari", "Maret", "April", "Mei", "Juni", 
                                      "Juli", "Agustus", "September", "Oktober", "November", "Desember" };
                string[] namaHari = { "Min", "Sen", "Sel", "Rab", "Kam", "Jum", "Sab" };
                
                string formattedDate = $"{namaHari[(int)selectedDate.DayOfWeek]}, {selectedDate.Day} {namaBulan[selectedDate.Month - 1]} {selectedDate.Year}";
                txtFilterDateDisplay.Text = formattedDate;
            }
        }

        /// <summary>
        /// Event handler ketika calendar dibuka
        /// </summary>
        private void DpFilterDate_CalendarOpened(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Event handler ketika calendar ditutup
        /// </summary>
        private void DpFilterDate_CalendarClosed(object sender, RoutedEventArgs e)
        {
            
            // Sembunyikan DatePicker lagi setelah calendar ditutup
            if (dpFilterDate != null)
            {
                dpFilterDate.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Event handler untuk button toggle popup penumpang
        /// </summary>
        private void BtnFilterPenumpang_Click(object sender, RoutedEventArgs e)
        {
            if (popupPenumpang != null)
            {
                // Sync nilai dari hidden textbox ke popup textbox
                if (int.TryParse(txtFilterPenumpang.Text, out int current))
                {
                    txtPopupPenumpang.Text = current.ToString();
                    UpdatePopupButtonStates(current);
                }
                
                popupPenumpang.IsOpen = !popupPenumpang.IsOpen;
            }
        }

        /// <summary>
        /// Validasi input hanya angka untuk TextBox penumpang
        /// </summary>
        private void TxtPopupPenumpang_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Hanya terima angka
            e.Handled = !int.TryParse(e.Text, out _);
        }

        /// <summary>
        /// Event handler ketika text penumpang berubah (manual input)
        /// </summary>
        private void TxtPopupPenumpang_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPopupPenumpang == null || txtFilterPenumpang == null)
                return;

            // Jika kosong, biarkan kosong (user sedang menghapus untuk ketik angka baru)
            if (string.IsNullOrWhiteSpace(txtPopupPenumpang.Text))
            {
                // Tidak auto-set ke 1, biarkan user mengetik angka baru
                return;
            }

            if (int.TryParse(txtPopupPenumpang.Text, out int value))
            {
                // Validasi tidak boleh 0
                if (value == 0)
                {
                    // Hapus angka 0, biarkan kosong
                    txtPopupPenumpang.Text = "";
                    txtPopupPenumpang.SelectionStart = 0;
                    return;
                }

                // Validasi minimal 1
                if (value < 1)
                {
                    value = 1;
                    txtPopupPenumpang.Text = "1";
                    txtPopupPenumpang.SelectionStart = 1;
                }

                // Validasi maksimal sesuai kendaraan
                int maksimalPenumpang = GetMaksimalPenumpangFromFilterKendaraan();
                if (value > maksimalPenumpang)
                {
                    value = maksimalPenumpang;
                    txtPopupPenumpang.Text = maksimalPenumpang.ToString();
                    txtPopupPenumpang.SelectionStart = maksimalPenumpang.ToString().Length;
                }

                // Sync ke hidden textbox dan update display
                txtFilterPenumpang.Text = value.ToString();
                UpdateFilterPenumpangDisplay(value);
                UpdatePopupButtonStates(value);
            }
        }

        /// <summary>
        /// Update state enabled/disabled untuk button +/- di popup
        /// </summary>
        private void UpdatePopupButtonStates(int current)
        {
            int maksimalPenumpang = GetMaksimalPenumpangFromFilterKendaraan();
            
            if (btnPopupMinusPenumpang != null)
                btnPopupMinusPenumpang.IsEnabled = current > 1;
            
            if (btnPopupPlusPenumpang != null)
                btnPopupPlusPenumpang.IsEnabled = current < maksimalPenumpang;
        }

        /// <summary>
        /// Event handler ketika popup penumpang ditutup
        /// </summary>
        private void PopupPenumpang_Closed(object sender, EventArgs e)
        {
            // Saat popup ditutup, jika TextBox kosong atau invalid, restore ke nilai terakhir yang valid
            if (txtPopupPenumpang != null && txtFilterPenumpang != null)
            {
                if (string.IsNullOrWhiteSpace(txtPopupPenumpang.Text))
                {
                    // Restore dari hidden textbox
                    if (int.TryParse(txtFilterPenumpang.Text, out int lastValue) && lastValue >= 1)
                    {
                        txtPopupPenumpang.Text = lastValue.ToString();
                    }
                    else
                    {
                        // Fallback ke 1
                        txtPopupPenumpang.Text = "1";
                        txtFilterPenumpang.Text = "1";
                        UpdateFilterPenumpangDisplay(1);
                    }
                }
            }
        }

        /// <summary>
        /// Event handler untuk button toggle popup kendaraan
        /// </summary>
        private void BtnFilterVehicle_Click(object sender, RoutedEventArgs e)
        {
            if (popupVehicle != null)
            {
                popupVehicle.IsOpen = !popupVehicle.IsOpen;
            }
        }

        /// <summary>
        /// Event handler ketika popup kendaraan dibuka - maintain highlight state
        /// </summary>
        private void PopupVehicle_Opened(object sender, EventArgs e)
        {
            // Maintain selected button highlight when popup reopens
            if (_selectedVehicleButton != null)
            {
                var blueBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#b7def8ff")); // Soft blue
                _selectedVehicleButton.SetValue(Button.BackgroundProperty, blueBrush);
            }
        }

        /// <summary>
        /// Event handler untuk memilih opsi kendaraan
        /// </summary>
        private void BtnVehicleOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagValue)
            {
                // Parse tag format: "ID|Name"
                var parts = tagValue.Split('|');
                if (parts.Length == 2 && int.TryParse(parts[0], out int jenisKendaraanId))
                {
                    string vehicleName = parts[1];
                    
                    // Reset previous selected button - clear local value to allow style to work
                    if (_selectedVehicleButton != null)
                    {
                        _selectedVehicleButton.ClearValue(Button.BackgroundProperty);
                    }
                    
                    // Set local value on current button - use soft blue color
                    var blueBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#b7def8ff")); // Soft blue - same as hover
                    button.SetValue(Button.BackgroundProperty, blueBrush);
                    
                    // Save current selected button
                    _selectedVehicleButton = button;
                    
                    // Update display text with colored format
                    UpdateVehicleDisplay(jenisKendaraanId, vehicleName);
                    txtFilterVehicle.Text = jenisKendaraanId.ToString();

                    // Get max passengers for this vehicle
                    int maksimalPenumpang = DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanId);
                    
                    // Check current passenger count
                    if (int.TryParse(txtFilterPenumpang.Text, out int currentPenumpang))
                    {
                        // If current passengers exceed max, adjust to max
                        if (currentPenumpang > maksimalPenumpang)
                        {
                            int newValue = maksimalPenumpang;
                            txtFilterPenumpang.Text = newValue.ToString();
                            UpdateFilterPenumpangDisplay(newValue);
                            
                            CustomDialog.ShowInfo(
                                "Info",
                                $"Jumlah penumpang disesuaikan menjadi {newValue} (maksimal untuk kendaraan ini).");
                        }
                    }

                    // Close popup
                    if (popupVehicle != null)
                    {
                        popupVehicle.IsOpen = false;
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

        /// <summary>
        /// Update vehicle display dengan format warna (nama hitam, golongan cyan)
        /// </summary>
        private void UpdateVehicleDisplay(int jenisKendaraanId, string vehicleName)
        {
            txtFilterVehicleDisplay.Inlines.Clear();
            
            // Dictionary untuk mapping golongan
            var golonganMap = new Dictionary<int, string>
            {
                { 0, "" }, // Pejalan Kaki - no golongan
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
            
            // Add vehicle name in black
            var nameRun = new Run(vehicleName) { Foreground = Brushes.Black };
            txtFilterVehicleDisplay.Inlines.Add(nameRun);
            
            // Add golongan in cyan if exists
            if (golonganMap.ContainsKey(jenisKendaraanId) && !string.IsNullOrEmpty(golonganMap[jenisKendaraanId]))
            {
                var golonganRun = new Run(golonganMap[jenisKendaraanId])
                {
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00B4B5"))
                };
                txtFilterVehicleDisplay.Inlines.Add(golonganRun);
            }
        }

        /// <summary>
        /// OBSOLETE: Method lama untuk dropdown penumpang (removed)
        /// </summary>

        /// <summary>
        /// Helper untuk convert jenis_kendaraan_id ke text
        /// </summary>
        private string GetJenisKendaraanText(int jenisKendaraanId)
        {
            return jenisKendaraanId switch
            {
                0 => "Pejalan kaki tanpa kendaraan",
                1 => "Sepeda",
                2 => "Sepeda Motor (<500cc)",
                3 => "Sepeda Motor (>500cc) (Golongan III)",
                4 => "Mobil jeep, sedan, minibus",
                5 => "Mobil barang bak muatan",
                6 => "Mobil bus penumpang (5-7 meter)",
                7 => "Mobil barang (truk/tangki) ukuran sedang",
                8 => "Mobil bus penumpang (7-10 meter)",
                9 => "Mobil barang (truk/tangki) sedang",
                10 => "Mobil tronton, tangki, penarik + gandengan (10-12 meter)",
                11 => "Mobil tronton, tangki, alat berat (12-16 meter)",
                12 => "Mobil tronton, tangki, alat berat (>16 meter)",
                _ => "Kendaraan tidak diketahui"
            };
        }

        /// <summary>
        /// Load schedule data dari database (real data)
        /// </summary>
        private void LoadScheduleFromDatabase()
        {
            if (_jadwals == null || !_jadwals.Any())
            {
                CustomDialog.ShowInfo("Info", "Tidak ada data jadwal yang tersedia.");
                return;
            }

            ScheduleItems.Clear();

            foreach (var jadwal in _jadwals)
            {
                try
                {
                    // ? FIX TIMEZONE: Convert UTC to pelabuhan timezone
                    // Database menyimpan dalam UTC, convert ke timezone pelabuhan
                    var offsetAsalHours = jadwal.pelabuhan_asal?.TimezoneOffsetHours ?? 7;  // Default WIB
                    var offsetTujuanHours = jadwal.pelabuhan_tujuan?.TimezoneOffsetHours ?? 7;
                    
                    var waktuBerangkatLocal = jadwal.waktu_berangkat.AddHours(offsetAsalHours);
                    var waktuTibaLocal = jadwal.waktu_tiba.AddHours(offsetTujuanHours);

                    // Format tanggal keberangkatan (gunakan timezone pelabuhan asal)
                    var tanggal = waktuBerangkatLocal.Date;
                    var boardingDate = tanggal.ToString("dddd, dd MMMM yyyy",
                        new System.Globalization.CultureInfo("id-ID"));

                    // Hitung durasi perjalanan (ACTUAL duration, bukan display time difference)
                    var duration = jadwal.waktu_tiba - jadwal.waktu_berangkat;
                    var durationText = $"{(int)duration.TotalHours} jam {duration.Minutes} menit";

                    // Format waktu check-in (15 menit sebelum berangkat) - gunakan timezone pelabuhan asal
                    var checkInTime = waktuBerangkatLocal.AddMinutes(-15);
                    var warningText = $"Masuk pelabuhan (check-in) sebelum {checkInTime:HH:mm}";

                    // Cari harga kendaraan yang sesuai dari GrupKendaraan
                    var detailKendaraan = jadwal.GrupKendaraan?.DetailKendaraans?
                        .FirstOrDefault(d => d.jenis_kendaraan == _searchCriteria?.JenisKendaraanId);
                    
                    // Validasi jenis kendaraan tersedia
                    if (detailKendaraan == null)
                    {
                        // Jadwal ini tidak support jenis kendaraan yang dicari
                        continue;
                    }

                    decimal harga = detailKendaraan.harga_kendaraan;

                    // LOGIC BARU: Hitung total harga berdasarkan jenis kendaraan
                    int jumlahPenumpang = _searchCriteria?.JumlahPenumpang ?? 1;
                    int jenisKendaraanId = _searchCriteria?.JenisKendaraanId ?? 0;
                    decimal totalHarga;

                    if (jenisKendaraanId == 0) // Pejalan kaki
                    {
                        // Kalikan dengan jumlah penumpang
                        totalHarga = harga * jumlahPenumpang;
                    }
                    else // Menggunakan kendaraan
                    {
                        // Tidak dikali dengan jumlah penumpang
                        totalHarga = harga;
                    }

                    var priceText = $"IDR {totalHarga:N0}";

                    // FIX: Pastikan nama pelabuhan ter-load dengan benar
                    string departurePort = jadwal.pelabuhan_asal?.nama_pelabuhan ?? "N/A";
                    string arrivalPort = jadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "N/A";
                    var scheduleItem = new ScheduleItem
                    {
                        FerryType = jadwal.kelas_layanan ?? "Reguler",
                        BoardingDate = boardingDate,
                        WarningText = warningText,
                        DepartureTime = $"{waktuBerangkatLocal:HH:mm}",  // Tanpa timezone
                        DeparturePort = departurePort,
                        ArrivalTime = $"{waktuTibaLocal:HH:mm}",  // Tanpa timezone
                        ArrivalPort = arrivalPort,
                        Duration = durationText,
                        Capacity = $"Kapasitas Tersedia ({jadwal.sisa_kapasitas_penumpang})",
                        Price = priceText,
                        PortName = $"{departurePort} ({jadwal.pelabuhan_asal?.kota ?? "N/A"})",
                        ShipName = jadwal.kapal?.nama_kapal ?? "N/A",
                        PortFacilities = ParseFacilities(jadwal.pelabuhan_asal?.fasilitas),
                        ShipFacilities = ParseFacilities(jadwal.kapal?.fasilitas),
                        JadwalId = jadwal.jadwal_id
                    };

                    ScheduleItems.Add(scheduleItem);
                }
                catch (Exception ex)
                {
                    // Skip jadwal yang error
                    continue;
                }
            }

            icScheduleList.ItemsSource = ScheduleItems;
        }

        /// <summary>
        /// Parse fasilitas dari string (comma-separated) ke List
        /// </summary>
        private List<string> ParseFacilities(string? fasilitasString)
        {
            if (string.IsNullOrEmpty(fasilitasString))
                return new List<string> { "Informasi fasilitas tidak tersedia" };

            return fasilitasString
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();
        }



        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            // Kembali ke HomePage dengan state login
            bool isLoggedIn = SessionManager.IsLoggedIn;
            string username = SessionManager.CurrentUser?.nama ?? "";

            var homePage = new HomePage(isLoggedIn: isLoggedIn, username: username);
            homePage.Left = this.Left;
            homePage.Top = this.Top;
            homePage.Width = this.Width;
            homePage.Height = this.Height;
            homePage.WindowState = this.WindowState;
            homePage.Show();
            this.Close();
        }

        /// <summary>
        /// Implementasi pencarian dengan filter yang dipilih (simplified version)
        /// </summary>
        private async void BtnCari_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validasi Pelabuhan Asal
                if (cmbFilterFrom.SelectedIndex < 0 ||
                    !(cmbFilterFrom.SelectedItem is PelabuhanComboBoxItem pelabuhanAsal) ||
                    pelabuhanAsal.Id == 0)
                {
                    CustomDialog.ShowWarning("Peringatan", "Silakan pilih Pelabuhan Asal!");
                    return;
                }

                // Validasi Pelabuhan Tujuan
                if (cmbFilterTo.SelectedIndex < 0 ||
                    !(cmbFilterTo.SelectedItem is PelabuhanComboBoxItem pelabuhanTujuan) ||
                    pelabuhanTujuan.Id == 0)
                {
                    CustomDialog.ShowWarning("Peringatan", "Silakan pilih Pelabuhan Tujuan!");
                    return;
                }

                // Validasi Tanggal (DatePicker)
                if (!dpFilterDate.SelectedDate.HasValue)
                {
                    CustomDialog.ShowWarning("Peringatan", "Silakan pilih Tanggal!");
                    return;
                }

                // Validasi Jenis Kendaraan (harus sebelum validasi penumpang)
                if (txtFilterVehicle == null || string.IsNullOrEmpty(txtFilterVehicle.Text) ||
                    !int.TryParse(txtFilterVehicle.Text, out int jenisKendaraanId))
                {
                    CustomDialog.ShowWarning("Peringatan", "Silakan pilih Jenis Kendaraan!");
                    return;
                }
                
                // Dapatkan maksimal penumpang untuk jenis kendaraan yang dipilih
                int maksimalPenumpang = DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanId);

                // Validasi Penumpang
                if (!int.TryParse(txtFilterPenumpang.Text, out int jumlahPenumpang) || jumlahPenumpang < 1)
                {
                    CustomDialog.ShowWarning("Peringatan", "Jumlah penumpang minimal adalah 1!");
                    return;
                }

                // Validasi penumpang tidak melebihi maksimal untuk jenis kendaraan
                if (jumlahPenumpang > maksimalPenumpang)
                {
                    var specs = DetailKendaraan.GetSpecificationByJenis((JenisKendaraan)jenisKendaraanId);
                    CustomDialog.ShowWarning(
                        "Peringatan",
                        $"Jumlah penumpang untuk {specs.Deskripsi} maksimal {maksimalPenumpang} orang!");
                    return;
                }

                // Ambil data dari form
                var tanggalKeberangkatan = dpFilterDate.SelectedDate.Value;

                // Ambil jam keberangkatan dari ComboBox (jika dipilih)
                int? jamKeberangkatan = null;
                if (cmbFilterTime.SelectedIndex > 0 && cmbFilterTime.SelectedItem is ComboBoxItem selectedTimeItem)
                {
                    var timeText = selectedTimeItem.Content?.ToString();
                    if (!string.IsNullOrEmpty(timeText) && timeText.Contains(":"))
                    {
                        var timeParts = timeText.Split(':');
                        if (int.TryParse(timeParts[0], out int hour))
                        {
                            jamKeberangkatan = hour;
                        }
                    }
                }

                // Use tanggal keberangkatan as DateTime (no separate time filter)
                DateTime? tanggalKeberangkatanFilter = tanggalKeberangkatan;

                // Validasi pelabuhan asal dan tujuan tidak sama
                if (pelabuhanAsal.Id == pelabuhanTujuan.Id)
                {
                    CustomDialog.ShowWarning(
                        "Peringatan",
                        "Pelabuhan asal dan tujuan tidak boleh sama!");
                    return;
                }

                // Default kelas layanan (karena tidak ada ComboBox untuk ini di UI)
                string kelasLayanan = _searchCriteria?.KelasLayanan ?? "Reguler";

                // Search jadwal dari database dengan parameter jam
                var jadwals = await _jadwalService.SearchJadwalAsync(
                    pelabuhanAsal.Id,
                    pelabuhanTujuan.Id,
                    kelasLayanan,
                    tanggalKeberangkatanFilter,
                    jenisKendaraanId,
                    jamKeberangkatan  // Kirim jam keberangkatan
                );

                if (jadwals == null || !jadwals.Any())
                {
                    CustomDialog.ShowInfo(
                        "Jadwal Tidak Ditemukan",
                        "Tidak ditemukan jadwal yang sesuai dengan kriteria pencarian Anda.\n\nSilakan coba dengan kriteria lain atau pilih tanggal berbeda.");
                    return;
                }

                // Update search criteria dan jadwals
                _searchCriteria = new SearchCriteria
                {
                    PelabuhanAsalId = pelabuhanAsal.Id,
                    PelabuhanTujuanId = pelabuhanTujuan.Id,
                    KelasLayanan = kelasLayanan,
                    TanggalKeberangkatan = tanggalKeberangkatan,
                    JamKeberangkatan = jamKeberangkatan, // Simpan jam juga
                    JumlahPenumpang = jumlahPenumpang,
                    JenisKendaraanId = jenisKendaraanId
                };

                _jadwals = jadwals;

                // SAVE TO SESSION - PENTING!
                SessionManager.SaveSearchSession(_searchCriteria, _jadwals);

                // Reload schedule dengan data baru
                LoadScheduleFromDatabase();

                CustomDialog.ShowSuccess(
                    "Hasil Pencarian",
                    $"Ditemukan {jadwals.Count} jadwal yang sesuai dengan kriteria pencarian.");
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Terjadi kesalahan saat mencari jadwal:\n\n{ex.Message}");
            }
        }

        /// <summary>
        /// Session management untuk pilih tiket
        /// </summary>
        private void BtnPilihTiket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ScheduleItem schedule)
            {
                // CEK SESSION LOGIN
                if (!TiketLaut.Services.SessionManager.IsLoggedIn ||
                    TiketLaut.Services.SessionManager.CurrentUser == null)
                {
                    // User belum login - tampilkan warning dengan opsi login
                    var result = CustomDialog.ShowWarning(
                        "Masuk diperlukan",
                        "Silakan masuk terlebih dahulu untuk melanjutkan pemesanan tiket.\nIngin masuk sekarang?",
                        CustomDialog.DialogButtons.YesNo);

                    if (result == true)
                    {
                        // ? FIX: Pastikan menggunakan LoginSource.ScheduleWindow
                        try
                        {
                            var loginWindow = new LoginWindow(LoginSource.ScheduleWindow); // ? PENTING: Gunakan ScheduleWindow bukan HomePage!

                            // Preserve window size and position for login window
                            loginWindow.Left = this.Left;
                            loginWindow.Top = this.Top;
                            loginWindow.Width = this.Width;
                            loginWindow.Height = this.Height;
                            loginWindow.WindowState = this.WindowState;

                            loginWindow.Show();
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            CustomDialog.ShowError("Error", $"Terjadi kesalahan saat membuka halaman login:\n\n{ex.Message}");
                        }
                    }
                    // Jika user pilih "No", tetap di halaman schedule (tidak ada aksi)
                    return; // Exit method - tidak lanjut ke booking
                }

                // ? USER SUDAH LOGIN - Lanjut ke booking
                try
                {
                    // Debug log untuk memastikan _searchCriteria ada
                    if (_searchCriteria != null)
                    {
                    }
                    else
                    {
                    }

                    // Buat instance BookingDetailWindow
                    var bookingDetailWindow = new BookingDetailWindow(isFromSchedule: true);

                    // Set data schedule yang dipilih
                    bookingDetailWindow.SetScheduleData(schedule);

                    // PENTING: Set search criteria juga
                    if (_searchCriteria != null)
                    {
                        bookingDetailWindow.SetSearchCriteria(_searchCriteria);
                    }

                    // Preserve window size and position
                    bookingDetailWindow.Left = this.Left;
                    bookingDetailWindow.Top = this.Top;
                    bookingDetailWindow.Width = this.Width;
                    bookingDetailWindow.Height = this.Height;
                    bookingDetailWindow.WindowState = this.WindowState;

                    // Show new window and close current
                    bookingDetailWindow.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    CustomDialog.ShowError("Error", $"Terjadi kesalahan saat membuka halaman pemesanan:\n\n{ex.Message}");
                }
            }
        }

        private void BtnToggleDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Find the parent Border (ticket card)
                var parent = FindParent<Border>(button);
                if (parent != null)
                {
                    // Find the facilities panel
                    var facilitiesPanel = FindChild<StackPanel>(parent, "pnlFacilities");
                    var dropdownImage = FindChild<Image>(button, "imgDropdown");

                    if (facilitiesPanel != null)
                    {
                        // Toggle visibility
                        if (facilitiesPanel.Visibility == Visibility.Collapsed)
                        {
                            facilitiesPanel.Visibility = Visibility.Visible;
                            // Rotate icon 180 degrees (pointing up)
                            if (dropdownImage != null)
                            {
                                var rotateTransform = new System.Windows.Media.RotateTransform(180);
                                dropdownImage.RenderTransform = rotateTransform;
                                dropdownImage.RenderTransformOrigin = new Point(0.5, 0.5);
                            }
                        }
                        else
                        {
                            facilitiesPanel.Visibility = Visibility.Collapsed;
                            // Reset rotation (pointing down)
                            if (dropdownImage != null)
                            {
                                dropdownImage.RenderTransform = null;
                            }
                        }
                    }
                }
            }
        }

        // Helper method to find parent element
        private T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = System.Windows.Media.VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is T))
            {
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }

        // Helper method to find child element by name
        private T? FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
        {
            if (parent == null) return null;

            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && (string.IsNullOrEmpty(childName) || typedChild.Name == childName))
                {
                    return typedChild;
                }

                var foundChild = FindChild<T>(child, childName);
                if (foundChild != null) return foundChild;
            }

            return null;
        }
    }

    /// <summary>
    /// Helper class untuk ComboBox Pelabuhan
    /// </summary>
    public class PelabuhanComboBoxItem
    {
        public int Id { get; set; }
        public string DisplayText { get; set; } = string.Empty;

        public override string ToString()
        {
            return DisplayText;
        }
    }

    // Model untuk Schedule Item
    public class ScheduleItem : INotifyPropertyChanged
    {
        private string _ferryType = string.Empty;
        private string _boardingDate = string.Empty;
        private string _warningText = string.Empty;
        private string _departureTime = string.Empty;
        private string _departurePort = string.Empty;
        private string _arrivalTime = string.Empty;
        private string _arrivalPort = string.Empty;
        private string _duration = string.Empty;
        private string _capacity = string.Empty;
        private string _price = string.Empty;
        private string _portName = string.Empty;
        private string _shipName = string.Empty;
        private List<string> _portFacilities = new List<string>();
        private List<string> _shipFacilities = new List<string>();
        private int _jadwalId = 0;

        public string FerryType
        {
            get => _ferryType;
            set { _ferryType = value; OnPropertyChanged(nameof(FerryType)); }
        }

        public string BoardingDate
        {
            get => _boardingDate;
            set { _boardingDate = value; OnPropertyChanged(nameof(BoardingDate)); }
        }

        public string WarningText
        {
            get => _warningText;
            set { _warningText = value; OnPropertyChanged(nameof(WarningText)); }
        }

        public string DepartureTime
        {
            get => _departureTime;
            set { _departureTime = value; OnPropertyChanged(nameof(DepartureTime)); }
        }

        public string DeparturePort
        {
            get => _departurePort;
            set
            {
                _departurePort = value;
                OnPropertyChanged(nameof(DeparturePort));
            }
        }

        public string ArrivalTime
        {
            get => _arrivalTime;
            set { _arrivalTime = value; OnPropertyChanged(nameof(ArrivalTime)); }
        }

        public string ArrivalPort
        {
            get => _arrivalPort;
            set
            {
                _arrivalPort = value;
                OnPropertyChanged(nameof(ArrivalPort));
            }
        }

        public string Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(nameof(Duration)); }
        }

        public string Capacity
        {
            get => _capacity;
            set { _capacity = value; OnPropertyChanged(nameof(Capacity)); }
        }

        public string Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        public string PortName
        {
            get => _portName;
            set { _portName = value; OnPropertyChanged(nameof(PortName)); }
        }

        public string ShipName
        {
            get => _shipName;
            set { _shipName = value; OnPropertyChanged(nameof(ShipName)); }
        }

        public List<string> PortFacilities
        {
            get => _portFacilities;
            set { _portFacilities = value; OnPropertyChanged(nameof(PortFacilities)); }
        }

        public List<string> ShipFacilities
        {
            get => _shipFacilities;
            set { _shipFacilities = value; OnPropertyChanged(nameof(ShipFacilities)); }
        }

        public int JadwalId
        {
            get => _jadwalId;
            set { _jadwalId = value; OnPropertyChanged(nameof(JadwalId)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

