using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TiketLaut.Views
{
    public partial class HistoryWindow : Window
    {
        public ObservableCollection<HistoryItem> HistoryItems { get; set; } = new ObservableCollection<HistoryItem>();

        public HistoryWindow()
        {
            InitializeComponent();
            LoadHistoryData();
            
            // Set user info di navbar - element name: navbarPostLogin
            navbarPostLogin.SetUserInfo("Admin User");
        }

        private void LoadHistoryData()
        {
            // Sample data - ganti dengan data dari database
            HistoryItems = new ObservableCollection<HistoryItem>
            {
                new HistoryItem
                {
                    Route = "Bakauheni - Merak",
                    Status = "Selesai",
                    StatusColor = new SolidColorBrush(Color.FromRgb(0, 180, 181)), // #00B4B5
                    ShipName = "KMP Portlink III",
                    Date = "Kamis, 23 Oktober 2025",
                    Time = "20:30 - 22:40"
                },
                new HistoryItem
                {
                    Route = "Bakauheni - Merak",
                    Status = "Selesai",
                    StatusColor = new SolidColorBrush(Color.FromRgb(0, 180, 181)), // #00B4B5
                    ShipName = "KMP Portlink III",
                    Date = "Kamis, 23 Oktober 2025",
                    Time = "20:30 - 22:40"
                },
                new HistoryItem
                {
                    Route = "Bakauheni - Merak",
                    Status = "Selesai",
                    StatusColor = new SolidColorBrush(Color.FromRgb(0, 180, 181)), // #00B4B5
                    ShipName = "KMP Portlink III",
                    Date = "Kamis, 23 Oktober 2025",
                    Time = "20:30 - 22:40"
                }
            };

            icHistoryList.ItemsSource = HistoryItems;
        }

        private void HistoryCard_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Detail riwayat akan ditampilkan di sini", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            // Kembali ke HomePage dengan state login
            var homePage = new HomePage(isLoggedIn: true, username: "Admin User");
            homePage.Left = this.Left;
            homePage.Top = this.Top;
            homePage.Width = this.Width;
            homePage.Height = this.Height;
            homePage.WindowState = this.WindowState;
            homePage.Show();
            this.Close();
        }
    }

    // Model untuk History Item
    public class HistoryItem : INotifyPropertyChanged
    {
        private string _route = string.Empty;
        private string _status = string.Empty;
        private SolidColorBrush? _statusColor;
        private string _shipName = string.Empty;
        private string _date = string.Empty;
        private string _time = string.Empty;

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

        public SolidColorBrush? StatusColor
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
