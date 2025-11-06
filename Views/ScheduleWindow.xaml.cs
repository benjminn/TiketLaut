using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TiketLaut.Models;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class ScheduleWindow : Window
    {
        public ObservableCollection<ScheduleItem> ScheduleItems { get; set; } = new ObservableCollection<ScheduleItem>();
        private List<Jadwal>? _jadwals;
        private SearchCriteria? _searchCriteria;
        private readonly JadwalService _jadwalService;

        // Constructor default (backward compatibility)
        // Constructor default (backward compatibility) - UPDATED
        public ScheduleWindow()
        {
            InitializeComponent();
            _jadwalService = new JadwalService();

            // Set user info di navbar - FIXED to use SessionManager
            if (SessionManager.IsLoggedIn && SessionManager.CurrentUser != null)
            {
                navbarPostLogin.SetUserInfo(SessionManager.CurrentUser.nama);
            }
            else
            {
                navbarPostLogin.SetUserInfo("Guest User");
            }

            // CHECK: Apakah ada data pencarian tersimpan di session?
            if (SessionManager.LastSearchCriteria != null && SessionManager.LastSearchResults != null)
            {
                System.Diagnostics.Debug.WriteLine("[ScheduleWindow] Loading from saved search session");

                // Gunakan data dari session
                _searchCriteria = SessionManager.LastSearchCriteria;
                _jadwals = SessionManager.LastSearchResults;

                // Load dropdown dengan data dari session
                LoadFilterDropdownsAsync();

                // Load jadwal dari database (bukan sample data)
                LoadScheduleFromDatabase();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[ScheduleWindow] No saved search session");

                // Load dropdown saja, user perlu cari manual
                LoadFilterDropdownsAsync();
                
                // Show info message
                MessageBox.Show(
                    "Silakan gunakan form pencarian untuk menemukan jadwal keberangkatan.",
                    "Info",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }


        // Constructor baru dengan parameter dari database
        public ScheduleWindow(List<Jadwal> jadwals, SearchCriteria searchCriteria)
        {
            InitializeComponent();
            _jadwalService = new JadwalService();
            _jadwals = jadwals;
            _searchCriteria = searchCriteria;

            // Populate filter dropdown dengan data user
            LoadFilterDropdownsAsync();

            LoadScheduleFromDatabase();

            // Set user info di navbar
            if (TiketLaut.Services.SessionManager.IsLoggedIn &&
                TiketLaut.Services.SessionManager.CurrentUser != null)
            {
                navbarPostLogin.SetUserInfo(TiketLaut.Services.SessionManager.CurrentUser.nama);
            }
            else
            {
                navbarPostLogin.SetUserInfo("Guest User");
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
                MessageBox.Show(
                    $"Terjadi kesalahan saat memuat data filter:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ScheduleWindow] Error loading filter dropdowns: {ex.Message}");
            }
        }

        /// <summary>
        /// Set tanggal filter menggunakan DatePicker
        /// </summary>
        private void PopulateDateFilter()
        {
            // Set DisplayDateStart ke hari ini (tidak bisa pilih tanggal lampau)
            dpFilterDate.DisplayDateStart = DateTime.Today;

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
        /// Populate dropdown jenis kendaraan
        /// </summary>
        private void PopulateVehicleFilter()
        {
            cmbFilterVehicle.Items.Clear();
            cmbFilterVehicle.Items.Add(new ComboBoxItem { Content = "Pilih Jenis Kendaraan", Tag = -1 }); // Tag -1 untuk placeholder

            // Add vehicle types sesuai dengan Enums
            var vehicleTypes = new[]
            {
                "Pejalan kaki tanpa kendaraan",           // Index 0 = JenisKendaraan 0
                "Sepeda",                                  // Index 1 = JenisKendaraan 1
                "Sepeda Motor (<500cc)",                   // Index 2 = JenisKendaraan 2
                "Sepeda Motor (>500cc) (Golongan III)",   // Index 3 = JenisKendaraan 3
                "Mobil jeep, sedan, minibus",              // Index 4 = JenisKendaraan 4
                "Mobil barang bak muatan",                 // Index 5 = JenisKendaraan 5
                "Mobil bus penumpang (5-7 meter)",         // Index 6 = JenisKendaraan 6
                "Mobil barang (truk/tangki) ukuran sedang", // Index 7 = JenisKendaraan 7
                "Mobil bus penumpang (7-10 meter)",        // Index 8 = JenisKendaraan 8
                "Mobil barang (truk/tangki) sedang",       // Index 9 = JenisKendaraan 9
                "Mobil tronton, tangki, penarik + gandengan (10-12 meter)", // Index 10 = JenisKendaraan 10
                "Mobil tronton, tangki, alat berat (12-16 meter)",          // Index 11 = JenisKendaraan 11
                "Mobil tronton, tangki, alat berat (>16 meter)"             // Index 12 = JenisKendaraan 12
            };

            for (int i = 0; i < vehicleTypes.Length; i++)
            {
                cmbFilterVehicle.Items.Add(new ComboBoxItem
                {
                    Content = vehicleTypes[i],
                    Tag = i  // Tag = JenisKendaraanId (0-12)
                });
            }

            // Set selected berdasarkan search criteria
            if (_searchCriteria != null)
            {
                var selectedVehicleIndex = _searchCriteria.JenisKendaraanId + 1; // +1 karena index 0 adalah "Pilih"
                if (selectedVehicleIndex > 0 && selectedVehicleIndex < cmbFilterVehicle.Items.Count)
                {
                    cmbFilterVehicle.SelectedIndex = selectedVehicleIndex;
                }
                else
                {
                    cmbFilterVehicle.SelectedIndex = 0;
                }
            }
            else
            {
                cmbFilterVehicle.SelectedIndex = 0;
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
            if (txtFilterPenumpangDisplay == null || btnFilterMinusPenumpang == null || btnFilterPlusPenumpang == null)
                return;

            txtFilterPenumpangDisplay.Text = $"{count} Penumpang";
            
            // Dapatkan maksimal penumpang berdasarkan kendaraan yang dipilih
            int maksimalPenumpang = GetMaksimalPenumpangFromFilterKendaraan();
            
            // Update button states
            btnFilterMinusPenumpang.IsEnabled = count > 1;
            btnFilterPlusPenumpang.IsEnabled = count < maksimalPenumpang;
            
            System.Diagnostics.Debug.WriteLine($"[ScheduleWindow] UpdateFilterPenumpangDisplay: count={count}, max={maksimalPenumpang}, plus_enabled={count < maksimalPenumpang}");
        }

        /// <summary>
        /// Mendapatkan maksimal penumpang berdasarkan kendaraan yang dipilih di filter
        /// </summary>
        private int GetMaksimalPenumpangFromFilterKendaraan()
        {
            if (cmbFilterVehicle == null || cmbFilterVehicle.SelectedIndex <= 0)
                return 100; // Default maksimal jika belum pilih kendaraan (seperti pejalan kaki)

            // cmbFilterVehicle memiliki "Pilih Jenis Kendaraan" di index 0
            // Jadi jenisKendaraanIndex = SelectedIndex - 1
            int jenisKendaraanIndex = cmbFilterVehicle.SelectedIndex - 1;
            return DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanIndex);
        }

        /// <summary>
        /// Event handler saat jenis kendaraan di filter berubah
        /// </summary>
        private void cmbFilterVehicle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || cmbFilterVehicle == null || txtFilterPenumpang == null)
                return;

            int selectedIndex = cmbFilterVehicle.SelectedIndex;
            
            if (selectedIndex <= 0)
                return;

            // Kurangi 1 karena index 0 adalah "Pilih Jenis Kendaraan"
            int jenisKendaraanIndex = selectedIndex - 1;
            
            // Dapatkan maksimal penumpang untuk kendaraan ini
            int maksimalPenumpang = DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanIndex);
            
            System.Diagnostics.Debug.WriteLine($"[ScheduleWindow] Kendaraan filter dipilih, SelectedIndex: {selectedIndex}, JenisKendaraanIndex: {jenisKendaraanIndex}, Maks penumpang: {maksimalPenumpang}");
            
            // Ambil nilai penumpang saat ini
            if (int.TryParse(txtFilterPenumpang.Text, out int currentPenumpang))
            {
                int newValue = currentPenumpang;
                
                // Jika jumlah penumpang saat ini melebihi maksimal, set ke maksimal
                if (currentPenumpang > maksimalPenumpang)
                {
                    newValue = maksimalPenumpang;
                    txtFilterPenumpang.Text = newValue.ToString();
                }
                
                // PENTING: Selalu update display untuk refresh button states
                UpdateFilterPenumpangDisplay(newValue);
            }
            else
            {
                // Jika tidak valid, set ke 1
                txtFilterPenumpang.Text = "1";
                UpdateFilterPenumpangDisplay(1);
            }
        }

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
                    UpdateFilterPenumpangDisplay(current);
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
                    UpdateFilterPenumpangDisplay(current);
                }
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
                MessageBox.Show("Tidak ada data jadwal yang tersedia.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
                        System.Diagnostics.Debug.WriteLine($"[LoadSchedule] Skip jadwal {jadwal.jadwal_id} - jenis kendaraan tidak tersedia dalam grup");
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
                        System.Diagnostics.Debug.WriteLine($"[LoadSchedule] Pejalan kaki - Harga: {harga} x {jumlahPenumpang} = {totalHarga}");
                    }
                    else // Menggunakan kendaraan
                    {
                        // Tidak dikali dengan jumlah penumpang
                        totalHarga = harga;
                        System.Diagnostics.Debug.WriteLine($"[LoadSchedule] Kendaraan - Harga: {harga} (tidak dikali penumpang)");
                    }

                    var priceText = $"IDR {totalHarga:N0}";

                    // FIX: Pastikan nama pelabuhan ter-load dengan benar
                    string departurePort = jadwal.pelabuhan_asal?.nama_pelabuhan ?? "N/A";
                    string arrivalPort = jadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "N/A";

                    System.Diagnostics.Debug.WriteLine($"[LoadSchedule] Jadwal {jadwal.jadwal_id}:");
                    System.Diagnostics.Debug.WriteLine($"  departurePort: {departurePort}");
                    System.Diagnostics.Debug.WriteLine($"  arrivalPort: {arrivalPort}");

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

                    System.Diagnostics.Debug.WriteLine($"[LoadSchedule] Added item:");
                    System.Diagnostics.Debug.WriteLine($"  DeparturePort: {scheduleItem.DeparturePort}");
                    System.Diagnostics.Debug.WriteLine($"  ArrivalPort: {scheduleItem.ArrivalPort}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ScheduleWindow] Error processing jadwal {jadwal.jadwal_id}: {ex.Message}");
                    // Skip jadwal yang error
                    continue;
                }
            }

            icScheduleList.ItemsSource = ScheduleItems;

            System.Diagnostics.Debug.WriteLine($"[ScheduleWindow] Total loaded: {ScheduleItems.Count} schedules");
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
                    MessageBox.Show("Silakan pilih Pelabuhan Asal!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi Pelabuhan Tujuan
                if (cmbFilterTo.SelectedIndex < 0 ||
                    !(cmbFilterTo.SelectedItem is PelabuhanComboBoxItem pelabuhanTujuan) ||
                    pelabuhanTujuan.Id == 0)
                {
                    MessageBox.Show("Silakan pilih Pelabuhan Tujuan!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi Tanggal (DatePicker)
                if (!dpFilterDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Silakan pilih Tanggal!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi Jenis Kendaraan (harus sebelum validasi penumpang)
                if (cmbFilterVehicle.SelectedIndex < 0 ||
                    !(cmbFilterVehicle.SelectedItem is ComboBoxItem selectedVehicleItem) ||
                    selectedVehicleItem.Tag == null)
                {
                    MessageBox.Show("Silakan pilih Jenis Kendaraan!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Dapatkan index jenis kendaraan
                int jenisKendaraanId = (int)selectedVehicleItem.Tag;
                
                // Dapatkan maksimal penumpang untuk jenis kendaraan yang dipilih
                int maksimalPenumpang = DetailKendaraan.GetMaksimalPenumpangByIndex(jenisKendaraanId);

                // Validasi Penumpang
                if (!int.TryParse(txtFilterPenumpang.Text, out int jumlahPenumpang) || jumlahPenumpang < 1)
                {
                    MessageBox.Show("Jumlah penumpang minimal adalah 1!", "Peringatan",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validasi penumpang tidak melebihi maksimal untuk jenis kendaraan
                if (jumlahPenumpang > maksimalPenumpang)
                {
                    var specs = DetailKendaraan.GetSpecificationByJenis((JenisKendaraan)jenisKendaraanId);
                    MessageBox.Show(
                        $"Jumlah penumpang untuk {specs.Deskripsi} maksimal {maksimalPenumpang} orang!",
                        "Peringatan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
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
                    MessageBox.Show(
                        "Pelabuhan asal dan tujuan tidak boleh sama!",
                        "Peringatan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
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
                    MessageBox.Show(
                        "Tidak ditemukan jadwal yang sesuai dengan kriteria pencarian Anda.\n\n" +
                        "Silakan coba dengan kriteria lain atau pilih tanggal berbeda.",
                        "Jadwal Tidak Ditemukan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
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

                MessageBox.Show(
                    $"Ditemukan {jadwals.Count} jadwal yang sesuai dengan kriteria pencarian.",
                    "Hasil Pencarian",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Terjadi kesalahan saat mencari jadwal:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ScheduleWindow] Error searching schedules: {ex.Message}");
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
                    MessageBoxResult result = MessageBox.Show(
                        "Silakan login terlebih dahulu untuk melanjutkan pemesanan tiket.\n\n" +
                        "Ingin login sekarang?",
                        "Login Diperlukan",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        // ? BENAR - User ingin login, buka LoginWindow
                        try
                        {
                            var loginWindow = new LoginWindow();

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
                            MessageBox.Show(
                                $"Terjadi kesalahan saat membuka halaman login:\n{ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
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
                        System.Diagnostics.Debug.WriteLine($"[ScheduleWindow] SearchCriteria found:");
                        System.Diagnostics.Debug.WriteLine($"  JenisKendaraanId: {_searchCriteria.JenisKendaraanId}");
                        System.Diagnostics.Debug.WriteLine($"  JumlahPenumpang: {_searchCriteria.JumlahPenumpang}");
                        System.Diagnostics.Debug.WriteLine($"  PelabuhanAsalId: {_searchCriteria.PelabuhanAsalId}");
                        System.Diagnostics.Debug.WriteLine($"  PelabuhanTujuanId: {_searchCriteria.PelabuhanTujuanId}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[ScheduleWindow] WARNING: _searchCriteria is null!");
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

                    System.Diagnostics.Debug.WriteLine($"[ScheduleWindow] User {TiketLaut.Services.SessionManager.CurrentUser?.nama} proceeding to booking for schedule {schedule.JadwalId}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Terjadi kesalahan saat membuka halaman pemesanan:\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    System.Diagnostics.Debug.WriteLine($"[ScheduleWindow] Error opening booking window: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[ScheduleItem] DeparturePort set to: {value}");
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
                System.Diagnostics.Debug.WriteLine($"[ScheduleItem] ArrivalPort set to: {value}");
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

