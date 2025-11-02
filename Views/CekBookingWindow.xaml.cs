using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TiketLaut.Views
{
    public partial class CekBookingWindow : Window
    {
        public ObservableCollection<BookingItem> BookingItems { get; set; } = new ObservableCollection<BookingItem>();

        public CekBookingWindow()
        {
            InitializeComponent();
            LoadBookingData();
            
            // Set user info di navbar
            navbarPostLogin.SetUserInfo("Admin User");
        }

        private void LoadBookingData()
        {
            // Sample data - ganti dengan data dari database
            BookingItems = new ObservableCollection<BookingItem>
            {
                new BookingItem
                {
                    Route = "Bakauheni - Merak",
                    Status = "Aktif",
                    StatusColor = new SolidColorBrush(Color.FromRgb(106, 201, 54)), // #6AC936
                    ShipName = "KMP Portlink III",
                    Date = "Kamis, 23 Oktober 2025",
                    Time = "20:30 - 22:40",
                    ShowWarning = Visibility.Visible,
                    WarningText = "Masuk pelabuhan (check-in) sebelum 20:15"
                },
                new BookingItem
                {
                    Route = "Bakauheni - Merak",
                    Status = "Menunggu Pembayaran",
                    StatusColor = new SolidColorBrush(Color.FromRgb(0, 180, 181)), // #00B4B5
                    ShipName = "KMP Portlink III",
                    Date = "Kamis, 23 Oktober 2025",
                    Time = "20:30 - 22:40",
                    ShowWarning = Visibility.Visible,
                    WarningText = "Masuk pelabuhan (check-in) sebelum 20:15"
                },
                new BookingItem
                {
                    Route = "Bakauheni - Merak",
                    Status = "Gagal",
                    StatusColor = new SolidColorBrush(Color.FromRgb(248, 33, 33)), // #F82121
                    ShipName = "KMP Portlink III",
                    Date = "Kamis, 23 Oktober 2025",
                    Time = "20:30 - 22:40",
                    ShowWarning = Visibility.Collapsed,
                    WarningText = ""
                }
            };

            icBookingList.ItemsSource = BookingItems;
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
