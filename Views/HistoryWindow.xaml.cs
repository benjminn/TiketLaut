using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TiketLaut.Services;
using TiketLaut.Views.Components;

namespace TiketLaut.Views
{
    public partial class HistoryWindow : Window
    {
        public ObservableCollection<HistoryItem> HistoryItems { get; set; } = new ObservableCollection<HistoryItem>();

        private readonly RiwayatService _riwayatService;

        public HistoryWindow()
        {
            InitializeComponent();
            _riwayatService = new RiwayatService();

            // Set user info di navbar
            if (SessionManager.CurrentUser != null)
            {
                navbarPostLogin.SetUserInfo(SessionManager.CurrentUser.nama);
            }

            LoadHistoryDataFromDatabaseAsync();
        }

        /// <summary>
        /// Load riwayat data dari database
        /// </summary>
        private async void LoadHistoryDataFromDatabaseAsync()
        {
            try
            {
                if (SessionManager.CurrentUser == null)
                {
                    CustomDialog.ShowError("Error", "Session user tidak ditemukan!");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[HistoryWindow] Loading history for user: {SessionManager.CurrentUser.pengguna_id}");

                // Get riwayat dari database (HANYA yang "Selesai")
                var riwayatList = await _riwayatService.GetRiwayatByPenggunaIdAsync(
                    SessionManager.CurrentUser.pengguna_id);

                HistoryItems.Clear();

                foreach (var riwayat in riwayatList)
                {
                    var tiket = riwayat.tiket;
                    var jadwal = tiket.Jadwal;

                    // ✅ TIMEZONE FIX: Convert UTC to pelabuhan timezone
                    var offsetAsalHours = jadwal.pelabuhan_asal?.TimezoneOffsetHours ?? 7;  // Default WIB
                    var offsetTujuanHours = jadwal.pelabuhan_tujuan?.TimezoneOffsetHours ?? 7;
                    
                    var waktuBerangkatLocal = jadwal.waktu_berangkat.AddHours(offsetAsalHours);
                    var waktuTibaLocal = jadwal.waktu_tiba.AddHours(offsetTujuanHours);

                    // Format tanggal
                    var dateText = tiket.tanggal_pemesanan.ToString("dddd, dd MMMM yyyy",
                        new System.Globalization.CultureInfo("id-ID"));

                    // ? Semua riwayat pasti "Selesai" karena sudah difilter di service
                    var historyItem = new HistoryItem
                    {
                        PembayaranId = riwayat.pembayaran_id,
                        TiketId = tiket.tiket_id,
                        Route = $"{jadwal.pelabuhan_asal.nama_pelabuhan} - {jadwal.pelabuhan_tujuan.nama_pelabuhan}",
                        Status = "Selesai", // ? Always "Selesai"
                        StatusColor = new SolidColorBrush(Color.FromRgb(0, 180, 181)), // ?? Cyan #00B4B5
                        ShipName = jadwal.kapal.nama_kapal,
                        Date = dateText,
                        Time = $"{waktuBerangkatLocal:HH:mm} - {waktuTibaLocal:HH:mm}",  // ✅ Gunakan waktu lokal
                        KodeTiket = tiket.kode_tiket,
                        TotalHarga = riwayat.jumlah_bayar
                    };

                    HistoryItems.Add(historyItem);

                    System.Diagnostics.Debug.WriteLine($"[HistoryWindow] Added history: {historyItem.KodeTiket}");
                }

                icHistoryList.ItemsSource = HistoryItems;

                if (!HistoryItems.Any())
                {
                    CustomDialog.ShowInfo(
                        "Info",
                        "Anda belum memiliki riwayat perjalanan yang selesai.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[HistoryWindow] Total history loaded: {HistoryItems.Count}");
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError(
                    "Error",
                    $"Terjadi kesalahan saat memuat riwayat:\n{ex.Message}");

                System.Diagnostics.Debug.WriteLine($"[HistoryWindow] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[HistoryWindow] StackTrace: {ex.StackTrace}");
            }
        }

        private void HistoryCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is HistoryItem historyItem)
            {
                try
                {
                    // TODO: Bisa tambahkan detail window jika diperlukan
                    CustomDialog.ShowInfo(
                        "Detail Riwayat",
                        $"Kode Tiket: {historyItem.KodeTiket}\nRute: {historyItem.Route}\nKapal: {historyItem.ShipName}\nTanggal: {historyItem.Date}\nWaktu: {historyItem.Time}\nTotal: Rp {historyItem.TotalHarga:N0}\nStatus: {historyItem.Status}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[HistoryWindow] Error on card click: {ex.Message}");
                }
            }
        }

        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            // Kembali ke CekBookingWindow
            var cekBookingWindow = new CekBookingWindow();
            cekBookingWindow.Left = this.Left;
            cekBookingWindow.Top = this.Top;
            cekBookingWindow.Width = this.Width;
            cekBookingWindow.Height = this.Height;
            cekBookingWindow.WindowState = this.WindowState;
            cekBookingWindow.Show();
            this.Close();
        }
    }

    // Model untuk History Item
    public class HistoryItem : INotifyPropertyChanged
    {
        private int _pembayaranId;
        private int _tiketId;
        private string _route = string.Empty;
        private string _status = string.Empty;
        private SolidColorBrush? _statusColor;
        private string _shipName = string.Empty;
        private string _date = string.Empty;
        private string _time = string.Empty;
        private string _kodeTiket = string.Empty;
        private decimal _totalHarga;

        public int PembayaranId
        {
            get => _pembayaranId;
            set { _pembayaranId = value; OnPropertyChanged(nameof(PembayaranId)); }
        }

        public int TiketId
        {
            get => _tiketId;
            set { _tiketId = value; OnPropertyChanged(nameof(TiketId)); }
        }

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

        public string KodeTiket
        {
            get => _kodeTiket;
            set { _kodeTiket = value; OnPropertyChanged(nameof(KodeTiket)); }
        }

        public decimal TotalHarga
        {
            get => _totalHarga;
            set { _totalHarga = value; OnPropertyChanged(nameof(TotalHarga)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}