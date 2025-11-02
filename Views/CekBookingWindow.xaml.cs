using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq; // ? ADD THIS
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

        public CekBookingWindow()
        {
            InitializeComponent();
            _pembayaranService = new PembayaranService();
            
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

                // Get semua pembayaran user
                var pembayarans = await _pembayaranService.GetPembayaranByPenggunaIdAsync(
                    SessionManager.CurrentUser.pengguna_id);

                BookingItems.Clear();

                foreach (var pembayaran in pembayarans)
                {
                    var tiket = pembayaran.tiket;
                    var jadwal = tiket.Jadwal;

                    // Tentukan status dan warna
                    string status = pembayaran.status_bayar;
                    SolidColorBrush statusColor;
                    Visibility showWarning;

                    switch (status)
                    {
                        case "Confirmed":
                            statusColor = new SolidColorBrush(Color.FromRgb(106, 201, 54)); // Green
                            showWarning = Visibility.Visible;
                            status = "Aktif";
                            break;
                        case "Menunggu Konfirmasi":
                            statusColor = new SolidColorBrush(Color.FromRgb(0, 180, 181)); // Cyan
                            showWarning = Visibility.Visible;
                            status = "Menunggu Pembayaran";
                            break;
                        case "Gagal":
                            statusColor = new SolidColorBrush(Color.FromRgb(248, 33, 33)); // Red
                            showWarning = Visibility.Collapsed;
                            break;
                        default:
                            statusColor = Brushes.Gray;
                            showWarning = Visibility.Collapsed;
                            break;
                    }

                    // Format tanggal (dari search criteria atau default hari ini)
                    var tanggal = SessionManager.LastSearchCriteria?.TanggalKeberangkatan ?? DateTime.Today;
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
                }

                icBookingList.ItemsSource = BookingItems;

                if (!BookingItems.Any())
                {
                    MessageBox.Show(
                        "Anda belum memiliki riwayat booking.",
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
                
                System.Diagnostics.Debug.WriteLine($"[CekBookingWindow] Error: {ex.Message}");
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
