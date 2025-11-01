using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TiketLaut.Models;

namespace TiketLaut.Views
{
    public partial class ScheduleWindow : Window
    {
        public ObservableCollection<ScheduleItem> ScheduleItems { get; set; } = new ObservableCollection<ScheduleItem>();
        private List<Jadwal>? _jadwals;
        private SearchCriteria? _searchCriteria;

        // Constructor default (backward compatibility)
        public ScheduleWindow()
        {
            InitializeComponent();
            LoadScheduleData();
            
            // Set user info di navbar
            navbarPostLogin.SetUserInfo("Admin User");
        }

        // ? Constructor baru dengan parameter dari database
        public ScheduleWindow(List<Jadwal> jadwals, SearchCriteria searchCriteria)
        {
            InitializeComponent();
            _jadwals = jadwals;
            _searchCriteria = searchCriteria;
            
            // ? Populate filter dropdown dengan data user
            PopulateFilterDropdowns();
            
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
        /// ? Populate dropdown filter dengan data search criteria dari HomePage
        /// </summary>
        private async void PopulateFilterDropdowns()
        {
            if (_searchCriteria == null || _jadwals == null || !_jadwals.Any())
                return;

            try
            {
                var jadwalService = new TiketLaut.Services.JadwalService();
                
                // Load semua pelabuhan untuk dropdown
                var pelabuhans = await jadwalService.GetAllPelabuhanAsync();

                // ? Populate Pelabuhan Asal
                cmbFilterFrom.Items.Clear();
                var pelabuhanAsal = pelabuhans.FirstOrDefault(p => p.pelabuhan_id == _searchCriteria.PelabuhanAsalId);
                if (pelabuhanAsal != null)
                {
                    cmbFilterFrom.Items.Add(new ComboBoxItem
                    {
                        Content = pelabuhanAsal.nama_pelabuhan,
                        Tag = pelabuhanAsal.pelabuhan_id
                    });
                    cmbFilterFrom.SelectedIndex = 0;
                }

                // ? Populate Pelabuhan Tujuan
                cmbFilterTo.Items.Clear();
                var pelabuhanTujuan = pelabuhans.FirstOrDefault(p => p.pelabuhan_id == _searchCriteria.PelabuhanTujuanId);
                if (pelabuhanTujuan != null)
                {
                    cmbFilterTo.Items.Add(new ComboBoxItem
                    {
                        Content = pelabuhanTujuan.nama_pelabuhan,
                        Tag = pelabuhanTujuan.pelabuhan_id
                    });
                    cmbFilterTo.SelectedIndex = 0;
                }

                // ? Populate Tanggal
                cmbFilterDate.Items.Clear();
                var tanggalText = _searchCriteria.TanggalKeberangkatan.ToString("ddd, dd MMMM yyyy",
                    new System.Globalization.CultureInfo("id-ID"));
                cmbFilterDate.Items.Add(new ComboBoxItem { Content = tanggalText });
                cmbFilterDate.SelectedIndex = 0;

                // ? Populate Jam (optional)
                if (_searchCriteria.JamKeberangkatan.HasValue)
                {
                    cmbFilterTime.Items.Clear();
                    var jamText = _searchCriteria.JamKeberangkatan.Value.ToString("HH:mm");
                    cmbFilterTime.Items.Add(new ComboBoxItem { Content = jamText });
                    cmbFilterTime.SelectedIndex = 0;
                }

                // ? Populate Jenis Kendaraan
                cmbFilterVehicle.Items.Clear();
                string jenisKendaraanText = GetJenisKendaraanText(_searchCriteria.JenisKendaraanId);
                cmbFilterVehicle.Items.Add(new ComboBoxItem { Content = jenisKendaraanText });
                cmbFilterVehicle.SelectedIndex = 0;

                // ? Populate Jumlah Penumpang
                cmbFilterPassenger.Items.Clear();
                var penumpangText = $"{_searchCriteria.JumlahPenumpang} Penumpang";
                cmbFilterPassenger.Items.Add(new ComboBoxItem { Content = penumpangText });
                cmbFilterPassenger.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScheduleWindow] Error populating filters: {ex.Message}");
            }
        }

        /// <summary>
        /// ? Helper untuk convert jenis_kendaraan_id ke text
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
                    // Format tanggal keberangkatan
                    var tanggal = _searchCriteria?.TanggalKeberangkatan ?? DateTime.Today;
                    var boardingDate = tanggal.ToString("dddd, dd MMMM yyyy", 
                        new System.Globalization.CultureInfo("id-ID"));

                    // Hitung durasi perjalanan
                    var duration = jadwal.waktu_tiba.ToTimeSpan() - jadwal.waktu_berangkat.ToTimeSpan();
                    var durationText = $"{duration.Hours} jam {duration.Minutes} menit";

                    // Format waktu check-in (15 menit sebelum berangkat)
                    var checkInTime = jadwal.waktu_berangkat.AddMinutes(-15);
                    var warningText = $"Masuk pelabuhan (check-in) sebelum {checkInTime:HH:mm}";

                    // Cari harga kendaraan yang sesuai
                    var detailKendaraan = jadwal.DetailKendaraans
                        ?.FirstOrDefault(dk => dk.jenis_kendaraan == _searchCriteria?.JenisKendaraanId);

                    decimal harga = detailKendaraan?.harga_kendaraan ?? 0;
                    
                    // ? LOGIC BARU: Hitung total harga berdasarkan jenis kendaraan
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

                    // ? FIX: Pastikan nama pelabuhan ter-load dengan benar
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
                        DepartureTime = jadwal.waktu_berangkat.ToString("HH:mm"),
                        DeparturePort = departurePort,  // ? Sudah benar
                        ArrivalTime = jadwal.waktu_tiba.ToString("HH:mm"),
                        ArrivalPort = arrivalPort,  // ? Sudah benar
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

        /// <summary>
        /// Load sample data (untuk backward compatibility)
        /// </summary>
        private void LoadScheduleData()
        {
            // Sample data - untuk testing tanpa database
            ScheduleItems = new ObservableCollection<ScheduleItem>
            {
                new ScheduleItem
                {
                    FerryType = "Reguler",
                    BoardingDate = "Kamis, 23 Oktober 2025",
                    WarningText = "Masuk pelabuhan (check-in) sebelum 20:15",
                    DepartureTime = "20:30",
                    DeparturePort = "Bakauheni",
                    ArrivalTime = "22:40",
                    ArrivalPort = "Merak",
                    Duration = "2 jam 10 menit",
                    Capacity = "Kapasitas Tersedia (390)",
                    Price = "IDR 187,853",
                    PortName = "Bakauheni (Lampung Selatan)",
                    ShipName = "KMP Portlink III",
                    PortFacilities = new List<string>
                    {
                        "Ruang Tunggu", "Musala", "Toilet Umum",
                        "Area Pengisian Daya (Charging Station)",
                        "ATM Center", "Minimarket & Toko Oleh-oleh",
                        "Kantin / Pujasera (Food Court)",
                        "Pos Kesehatan", "Area Merokok (Smoking Area)"
                    },
                    ShipFacilities = new List<string>
                    {
                        "Ruang Penumpang (AC dan non-AC)",
                        "Kantin / Kafetaria", "Musala", "Toilet",
                        "Dek Terbuka (Area Merokok)",
                        "Ruang Lesehan bertikar",
                        "Hiburan", "Colokan Listrik"
                    }
                },
                new ScheduleItem
                {
                    FerryType = "Express",
                    BoardingDate = "Kamis, 23 Oktober 2025",
                    WarningText = "Masuk pelabuhan (check-in) sebelum 20:15",
                    DepartureTime = "20:30",
                    DeparturePort = "Bakauheni",
                    ArrivalTime = "21:30",
                    ArrivalPort = "Merak",
                    Duration = "1 jam",
                    Capacity = "Kapasitas Tersedia (90)",
                    Price = "IDR 457.853",
                    PortName = "Bakauheni (Lampung Selatan)",
                    ShipName = "KMP Express III",
                    PortFacilities = new List<string>
                    {
                        "Ruang Tunggu VIP",
                        "Musala",
                        "Toilet Umum",
                        "Area Pengisian Daya (Charging Station)",
                        "ATM Center",
                        "Minimarket & Toko Oleh-oleh",
                        "Kantin / Pujasera (Food Court)",
                        "Pos Kesehatan",
                        "Area Merokok (Smoking Area)"
                    },
                    ShipFacilities = new List<string>
                    {
                        "Ruang Penumpang AC (Premium)",
                        "Kantin / Kafetaria",
                        "Musala",
                        "Toilet",
                        "Dek Terbuka (Area Merokok)",
                        "Ruang Lesehan bertikar",
                        "Hiburan",
                        "Colokan Listrik",
                        "WiFi"
                    }
                },
                new ScheduleItem
                {
                    FerryType = "Reguler",
                    BoardingDate = "Kamis, 23 Oktober 2025",
                    WarningText = "Masuk pelabuhan (check-in) sebelum 22:45",
                    DepartureTime = "23:00",
                    DeparturePort = "Bakauheni",
                    ArrivalTime = "01:10",
                    ArrivalPort = "Merak",
                    Duration = "2 jam 10 menit",
                    Capacity = "Kapasitas Tersedia (390)",
                    Price = "IDR 187.853",
                    PortName = "Bakauheni (Lampung Selatan)",
                    ShipName = "KMP Portlink V",
                    PortFacilities = new List<string>
                    {
                        "Ruang Tunggu",
                        "Musala",
                        "Toilet Umum",
                        "Area Pengisian Daya (Charging Station)",
                        "ATM Center",
                        "Minimarket & Toko Oleh-oleh",
                        "Kantin / Pujasera (Food Court)",
                        "Pos Kesehatan",
                        "Area Merokok (Smoking Area)"
                    },
                    ShipFacilities = new List<string>
                    {
                        "Ruang Penumpang (AC dan non-AC)",
                        "Kantin / Kafetaria",
                        "Musala",
                        "Toilet",
                        "Dek Terbuka (Area Merokok)",
                        "Ruang Lesehan bertikar",
                        "Hiburan",
                        "Colokan Listrik"
                    }
                }
            };

            icScheduleList.ItemsSource = ScheduleItems;
        }

        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            // Kembali ke HomePage dengan state login
            bool isLoggedIn = TiketLaut.Services.SessionManager.IsLoggedIn;
            string username = TiketLaut.Services.SessionManager.CurrentUser?.nama ?? "";
            
            var homePage = new HomePage(isLoggedIn: isLoggedIn, username: username);
            homePage.Left = this.Left;
            homePage.Top = this.Top;
            homePage.Width = this.Width;
            homePage.Height = this.Height;
            homePage.WindowState = this.WindowState;
            homePage.Show();
            this.Close();
        }

        private void BtnCari_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fungsi pencarian akan diimplementasikan dengan filter yang dipilih", 
                           "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPilihTiket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ScheduleItem schedule)
            {
                // Buat instance BookingDetailWindow
                var bookingDetailWindow = new BookingDetailWindow(isFromSchedule: true);
                
                // Set data schedule yang dipilih
                bookingDetailWindow.SetScheduleData(schedule);
                
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