using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class CekBookingWindow : Window
    {
        public ObservableCollection<BookingItem> BookingItems { get; set; } = new ObservableCollection<BookingItem>();

        private readonly PembayaranService _pembayaranService;
        private readonly RiwayatService _riwayatService;

        public CekBookingWindow()
        {
            InitializeComponent();
            _pembayaranService = new PembayaranService();
            _riwayatService = new RiwayatService();

            // Set user info di navbar
            if (SessionManager.CurrentUser != null)
            {
                navbarPostLogin.SetUserInfo(SessionManager.CurrentUser.nama);
            }

            LoadBookingDataFromDatabaseAsync();
        }

        /// <summary>
        /// Load booking data real dari database
        /// </summary>
        private async void LoadBookingDataFromDatabaseAsync()
        {
            try
            {
                if (SessionManager.CurrentUser == null)
                {
                    MessageBox.Show("Session user tidak ditemukan!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] ========== START LOADING ==========");
                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] User ID: {SessionManager.CurrentUser.pengguna_id}");

                // ? AUTO-UPDATE: Pindahkan tiket yang sudah selesai ke riwayat
                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] Calling AutoUpdatePembayaranSelesaiAsync...");
                var updatedCount = await _riwayatService.AutoUpdatePembayaranSelesaiAsync();
                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] AutoUpdate completed. Updated {updatedCount} records.");

                // Get semua pembayaran user
                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] Fetching all pembayarans...");
                var pembayarans = await _pembayaranService.GetPembayaranByPenggunaIdAsync(
                    SessionManager.CurrentUser.pengguna_id);

                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] Total pembayarans fetched: {pembayarans.Count}");

                // ? DEBUG: Log semua status sebelum filter
                foreach (var p in pembayarans)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Pembayaran #{p.pembayaran_id}: status_bayar = '{p.status_bayar}', tiket = {p.tiket.kode_tiket}");
                }

                // ? FILTER: Jangan tampilkan tiket dengan status "Selesai" (sudah di riwayat)
                pembayarans = pembayarans
                    .Where(p => p.status_bayar != "Selesai")
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] After filter (exclude 'Selesai'): {pembayarans.Count} records");

                // ? DEBUG: Log semua status setelah filter
                foreach (var p in pembayarans)
                {
                    System.Diagnostics.Debug.WriteLine($"  - After Filter #{p.pembayaran_id}: status_bayar = '{p.status_bayar}'");
                }

                BookingItems.Clear();

                foreach (var pembayaran in pembayarans)
                {
                    var tiket = pembayaran.tiket;
                    var jadwal = tiket.Jadwal;

                    System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] Processing pembayaran #{pembayaran.pembayaran_id}:");
                    System.Diagnostics.Debug.WriteLine($"  - status_bayar: '{pembayaran.status_bayar}'");
                    System.Diagnostics.Debug.WriteLine($"  - tiket: {tiket.kode_tiket}");

                    // ? Tentukan status dan warna berdasarkan status_bayar
                    string status = pembayaran.status_bayar;
                    SolidColorBrush statusColor;
                    Visibility showWarning;

                    switch (status)
                    {
                        case "Aktif":
                            System.Diagnostics.Debug.WriteLine($"  - Matched case: Aktif (GREEN)");
                            statusColor = new SolidColorBrush(Color.FromRgb(106, 201, 54)); // ?? Green #6AC936
                            showWarning = Visibility.Visible;
                            status = "Aktif";
                            break;
                        case "Menunggu Pembayaran":
                            System.Diagnostics.Debug.WriteLine($"  - Matched case: Menunggu Pembayaran (CYAN)");
                            statusColor = new SolidColorBrush(Color.FromRgb(0, 180, 181)); // ?? Cyan #00B4B5
                            showWarning = Visibility.Collapsed;
                            status = "Menunggu Pembayaran";
                            break;
                        case "Gagal":
                            System.Diagnostics.Debug.WriteLine($"  - Matched case: Gagal (RED)");
                            statusColor = new SolidColorBrush(Color.FromRgb(248, 33, 33)); // ?? Red #F82121
                            showWarning = Visibility.Collapsed;
                            status = "Gagal";
                            break;
                        default:
                            System.Diagnostics.Debug.WriteLine($"  - Matched case: DEFAULT (GRAY) - status_bayar was: '{pembayaran.status_bayar}'");
                            statusColor = Brushes.Gray;
                            showWarning = Visibility.Collapsed;
                            break;
                    }

                    // Format tanggal
                    var tanggal = tiket.tanggal_pemesanan;
                    var dateText = tanggal.ToString("dddd, dd MMMM yyyy",
                        new System.Globalization.CultureInfo("id-ID"));

                    // Format warning text
                    var checkInTime = jadwal.waktu_berangkat.AddMinutes(-15);
                    var warningText = $"Masuk pelabuhan (check-in) sebelum {checkInTime:HH:mm}";

                    var bookingItem = new BookingItem
                    {
                        Route = $"{jadwal.pelabuhan_asal.nama_pelabuhan} - {jadwal.pelabuhan_tujuan.nama_pelabuhan}",
                        Status = status,
                        StatusColor = statusColor,
                        ShipName = jadwal.kapal.nama_kapal,
                        Date = dateText,
                        Time = $"{jadwal.waktu_berangkat:HH:mm} - {jadwal.waktu_tiba:HH:mm}",
                        ShowWarning = showWarning,
                        WarningText = warningText
                    };

                    BookingItems.Add(bookingItem);

                    System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] ? Added booking: {tiket.kode_tiket} - Status: {status}");
                }

                icBookingList.ItemsSource = BookingItems;

                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] ========== LOADING COMPLETE ==========");
                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] Total bookings loaded: {BookingItems.Count}");

                if (!BookingItems.Any())
                {
                    MessageBox.Show(
                        "Anda belum memiliki booking aktif.",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Terjadi kesalahan saat memuat data:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] ========== ERROR ==========");
                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] StackTrace: {ex.StackTrace}");
            }
        }

        private void BtnRiwayat_Click(object sender, RoutedEventArgs e)
        {
            // Navigasi ke halaman History dengan mempertahankan ukuran window
            var historyWindow = new HistoryWindow();
            historyWindow.Left = this.Left;
            historyWindow.Top = this.Top;
            historyWindow.Width = this.Width;
            historyWindow.Height = this.Height;
            historyWindow.WindowState = this.WindowState;
            historyWindow.Show();
            this.Close();
        }

        private void BookingCard_Click(object sender, MouseButtonEventArgs e)
        {
            // TODO: Navigate to booking detail page
            MessageBox.Show("Navigasi ke detail pemesanan", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    // Model class untuk Booking Item
    public class BookingItem : INotifyPropertyChanged
    {
        private string _route = string.Empty;
        private string _status = string.Empty;
        private SolidColorBrush _statusColor = Brushes.Black;
        private string _shipName = string.Empty;
        private string _date = string.Empty;
        private string _time = string.Empty;
        private Visibility _showWarning = Visibility.Collapsed;
        private string _warningText = string.Empty;

        public string Route
        {
            get => _route;
            set { _route = value; OnPropertyChanged(nameof(Route)); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public SolidColorBrush StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(nameof(StatusColor)); }
        }

        public string ShipName
        {
            get => _shipName;
            set { _shipName = value; OnPropertyChanged(nameof(ShipName)); }
        }

        public string Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(nameof(Date)); }
        }

        public string Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(nameof(Time)); }
        }

        public Visibility ShowWarning
        {
            get => _showWarning;
            set { _showWarning = value; OnPropertyChanged(nameof(ShowWarning)); }
        }

        public string WarningText
        {
            get => _warningText;
            set { _warningText = value; OnPropertyChanged(nameof(WarningText)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}