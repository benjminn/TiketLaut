using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TiketLaut.Services;
using TiketLaut.Views.Components;

namespace TiketLaut.Views
{
    public partial class CekBookingWindow : Window
    {
        public ObservableCollection<BookingItem> BookingItems { get; set; } = new ObservableCollection<BookingItem>();

        private readonly PembayaranService _pembayaranService;
        private readonly RiwayatService _riwayatService;
        private readonly TiketService _tiketService;

        public CekBookingWindow()
        {
            InitializeComponent();
            _pembayaranService = new PembayaranService();
            _riwayatService = new RiwayatService();
            _tiketService = new TiketService();

            // Set user info di navbar
            if (SessionManager.CurrentUser != null)
            {
                navbarPostLogin.SetUserInfo(SessionManager.CurrentUser.nama);
            }

            LoadBookingDataFromDatabaseAsync();
        }

        private string GetStatusDisplayText(string statusString)
        {
            return statusString switch
            {
                "Menunggu Pembayaran" => "Menunggu Pembayaran",
                "Menunggu Validasi" => "Menunggu Validasi",
                "Sukses" => "Sukses",
                "Gagal" => "Gagal",
                "Selesai" => "Selesai",
                _ => statusString
            };
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
                    CustomDialog.ShowError("Error", "Session user tidak ditemukan!");
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
                    string status = GetStatusDisplayText(pembayaran.status_bayar);
                    SolidColorBrush statusColor;
                    Visibility showWarning;

                    switch (status)
                    {
                        case "Sukses":
                            System.Diagnostics.Debug.WriteLine($"  - Matched case: Sukses (GREEN)");
                            statusColor = new SolidColorBrush(Color.FromRgb(106, 201, 54)); // Green
                            showWarning = Visibility.Visible;
                            break;
                        case "Menunggu Pembayaran":
                            System.Diagnostics.Debug.WriteLine($"  - Matched case: Menunggu Pembayaran (ORANGE)");
                            statusColor = new SolidColorBrush(Color.FromRgb(255, 165, 0)); // Orange
                            showWarning = Visibility.Collapsed;
                            break;
                        case "Menunggu Validasi":
                            System.Diagnostics.Debug.WriteLine($"  - Matched case: Menunggu Validasi (CYAN)");
                            statusColor = new SolidColorBrush(Color.FromRgb(0, 180, 181)); // Cyan
                            showWarning = Visibility.Collapsed;
                            break;
                        case "Gagal":
                            System.Diagnostics.Debug.WriteLine($"  - Matched case: Gagal (RED)");
                            statusColor = new SolidColorBrush(Color.FromRgb(248, 33, 33)); // Red
                            showWarning = Visibility.Collapsed;
                            break;
                        default:
                            System.Diagnostics.Debug.WriteLine($"  - Matched case: DEFAULT (GRAY)");
                            statusColor = Brushes.Gray;
                            showWarning = Visibility.Collapsed;
                            break;
                    }

                    // ✅ TIMEZONE FIX: Convert UTC to pelabuhan timezone
                    var offsetAsalHours = jadwal.pelabuhan_asal?.TimezoneOffsetHours ?? 7;  // Default WIB
                    var offsetTujuanHours = jadwal.pelabuhan_tujuan?.TimezoneOffsetHours ?? 7;
                    
                    var waktuBerangkatLocal = jadwal.waktu_berangkat.AddHours(offsetAsalHours);
                    var waktuTibaLocal = jadwal.waktu_tiba.AddHours(offsetTujuanHours);

                    // Format tanggal
                    var tanggal = tiket.tanggal_pemesanan;
                    var dateText = tanggal.ToString("dddd, dd MMMM yyyy",
                        new System.Globalization.CultureInfo("id-ID"));

                    // Format warning text (gunakan waktu lokal pelabuhan asal)
                    var checkInTime = waktuBerangkatLocal.AddMinutes(-15);
                    var warningText = $"Masuk pelabuhan (check-in) sebelum {checkInTime:HH:mm}";

                    var bookingItem = new BookingItem
                    {
                        TiketId = tiket.tiket_id,
                        PembayaranId = pembayaran.pembayaran_id,
                        Route = $"{jadwal.pelabuhan_asal.nama_pelabuhan} - {jadwal.pelabuhan_tujuan.nama_pelabuhan}",
                        Status = status,
                        StatusColor = statusColor,
                        ShipName = jadwal.kapal.nama_kapal,
                        Date = dateText,
                        Time = $"{waktuBerangkatLocal:HH:mm} - {waktuTibaLocal:HH:mm}",  // ✅ Gunakan waktu lokal
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
                    CustomDialog.ShowInfo(
                        "Info",
                        "Anda belum memiliki booking Sukses.");
                }
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError(
                    "Error",
                    $"Terjadi kesalahan saat memuat data:\n{ex.Message}");

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
            // Get booking item dari sender
            var border = sender as Border;
            if (border?.DataContext is not BookingItem bookingItem)
                return;

            var status = bookingItem.Status;

            switch (status)
            {
                case "Sukses":
                    // Navigate to detail tiket e-boarding pass
                    var tiketDetailWindow = new TiketDetailWindow(bookingItem.TiketId);
                    tiketDetailWindow.Left = this.Left;
                    tiketDetailWindow.Top = this.Top;
                    tiketDetailWindow.Width = this.Width;
                    tiketDetailWindow.Height = this.Height;
                    tiketDetailWindow.WindowState = this.WindowState;
                    tiketDetailWindow.Show();
                    this.Close();
                    break;

                case "Menunggu Pembayaran":
                    // Navigate to payment page
                    var paymentWindow = new PaymentWindow();
                    paymentWindow.Left = this.Left;
                    paymentWindow.Top = this.Top;
                    paymentWindow.Width = this.Width;
                    paymentWindow.Height = this.Height;
                    paymentWindow.WindowState = this.WindowState;
                    paymentWindow.Show();
                    this.Close();
                    break;

                case "Menunggu Validasi":
                    // Show popup - sedang menunggu validasi
                    CustomDialog.ShowInfo(
                        "Menunggu Validasi",
                        "Pembayaran Anda sedang menunggu validasi oleh admin. Harap menunggu konfirmasi lebih lanjut.",
                        CustomDialog.DialogButtons.OK
                    );
                    break;

                case "Gagal":
                    // Show popup - pembayaran gagal
                    CustomDialog.ShowError(
                        "Pembayaran Gagal",
                        "Pembayaran Anda gagal diproses. Kemungkinan penyebab:\n\n• Bukti pembayaran tidak valid\n• Jumlah pembayaran tidak sesuai\n• Waktu pembayaran telah habis\n\nSilakan hubungi customer service untuk informasi lebih lanjut.",
                        CustomDialog.DialogButtons.OK
                    );
                    break;

                default:
                    CustomDialog.ShowInfo("Info", $"Status: {status}");
                    break;
            }
        }

        // Filter popup handlers
        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            popupFilter.IsOpen = !popupFilter.IsOpen;
        }

        private void PopupFilter_Opened(object sender, EventArgs e)
        {
            // Center popup under the button so it feels anchored to the label
            if (popupFilter.Child is FrameworkElement child)
            {
                var buttonWidth = btnFilter.ActualWidth;
                var popupWidth = child.ActualWidth > 0 ? child.ActualWidth : 300; // fallback to MinWidth
                var offset = (buttonWidth - popupWidth) / 2;
                // Clamp so popup stays under the button (allow slight left shift but not past previous button)
                popupFilter.HorizontalOffset = offset < -20 ? -20 : offset;
            }
        }

        private void PopupFilter_Closed(object sender, EventArgs e)
        {
            // Optional: do something when popup closes
        }

        private void FilterOption_Changed(object sender, RoutedEventArgs e)
        {
            if (chkSemua == null || icBookingList == null) return;

            // If "Semua" is checked, uncheck others
            if (sender == chkSemua && chkSemua.IsChecked == true)
            {
                chkMenungguPembayaran.IsChecked = false;
                chkMenungguValidasi.IsChecked = false;
                chkSukses.IsChecked = false;
                chkGagal.IsChecked = false;
            }
            // If any other checkbox is checked, uncheck "Semua"
            else if (sender != chkSemua && ((CheckBox)sender).IsChecked == true)
            {
                chkSemua.IsChecked = false;
            }

            // Check if all are unchecked, then check "Semua"
            if (chkMenungguPembayaran?.IsChecked != true && 
                chkMenungguValidasi?.IsChecked != true && 
                chkSukses?.IsChecked != true && 
                chkGagal?.IsChecked != true)
            {
                chkSemua.IsChecked = true;
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (BookingItems == null || icBookingList == null) return;

            var filtered = BookingItems.AsEnumerable();

            // Apply status filter
            if (chkSemua?.IsChecked != true)
            {
                var statusFilters = new List<string>();
                
                if (chkMenungguPembayaran?.IsChecked == true)
                    statusFilters.Add("Menunggu Pembayaran");
                
                if (chkMenungguValidasi?.IsChecked == true)
                    statusFilters.Add("Menunggu Validasi");
                
                if (chkSukses?.IsChecked == true)
                    statusFilters.Add("Sukses");
                
                if (chkGagal?.IsChecked == true)
                    statusFilters.Add("Gagal");

                if (statusFilters.Any())
                {
                    filtered = filtered.Where(b => statusFilters.Contains(b.Status));
                }
            }

            // Apply sort
            filtered = ApplySort(filtered);

            icBookingList.ItemsSource = filtered.ToList();

            // Update filter text
            UpdateFilterText();
        }

        private void UpdateFilterText()
        {
            if (txtFilter == null) return;

            if (chkSemua?.IsChecked == true)
            {
                txtFilter.Text = "Filter";
            }
            else
            {
                var activeFilters = new List<string>();
                if (chkMenungguPembayaran?.IsChecked == true) activeFilters.Add("Menunggu Pembayaran");
                if (chkMenungguValidasi?.IsChecked == true) activeFilters.Add("Menunggu Validasi");
                if (chkSukses?.IsChecked == true) activeFilters.Add("Sukses");
                if (chkGagal?.IsChecked == true) activeFilters.Add("Gagal");

                txtFilter.Text = activeFilters.Any() ? $"Filter ({activeFilters.Count})" : "Filter";
            }
        }

        // Sort popup handlers
        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            popupSort.IsOpen = !popupSort.IsOpen;
        }

        private void PopupSort_Opened(object sender, EventArgs e)
        {
            // Center popup under the button so it feels anchored to the label
            if (popupSort.Child is FrameworkElement child)
            {
                var buttonWidth = btnSort.ActualWidth;
                var popupWidth = child.ActualWidth > 0 ? child.ActualWidth : 300; // fallback to MinWidth
                var offset = (buttonWidth - popupWidth) / 2;
                // Prevent the popup from sliding left under the Filter button and keep a small gap
                popupSort.HorizontalOffset = offset < 10 ? 10 : offset;
            }
        }

        private void PopupSort_Closed(object sender, EventArgs e)
        {
            // Optional: do something when popup closes
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if ((popupFilter?.IsOpen == true || popupSort?.IsOpen == true) &&
                (Math.Abs(e.VerticalChange) > 0.0 || Math.Abs(e.HorizontalChange) > 0.0))
            {
                var filterPopup = popupFilter;
                if (filterPopup?.IsOpen == true)
                {
                    filterPopup.IsOpen = false;
                }

                var sortPopup = popupSort;
                if (sortPopup?.IsOpen == true)
                {
                    sortPopup.IsOpen = false;
                }
            }
        }

        private void SortOption_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilter(); // Re-apply filter which includes sorting
        }

        private IEnumerable<BookingItem> ApplySort(IEnumerable<BookingItem> items)
        {
            if (rbTanggalTerbaru?.IsChecked == true)
            {
                return items.OrderByDescending(b => b.TiketId);
            }
            else if (rbTanggalTerlama?.IsChecked == true)
            {
                return items.OrderBy(b => b.TiketId);
            }
            else if (rbHargaTertinggi?.IsChecked == true)
            {
                // For now, sort by TiketId as proxy. You can add Price property later
                return items.OrderByDescending(b => b.TiketId);
            }
            else if (rbHargaTerendah?.IsChecked == true)
            {
                // For now, sort by TiketId as proxy. You can add Price property later
                return items.OrderBy(b => b.TiketId);
            }

            return items;
        }
    }

    // Model class untuk Booking Item
    public class BookingItem : INotifyPropertyChanged
    {
        private int _tiketId;
        private int _pembayaranId;
        private string _route = string.Empty;
        private string _status = string.Empty;
        private SolidColorBrush _statusColor = Brushes.Black;
        private string _shipName = string.Empty;
        private string _date = string.Empty;
        private string _time = string.Empty;
        private Visibility _showWarning = Visibility.Collapsed;
        private string _warningText = string.Empty;

        public int TiketId
        {
            get => _tiketId;
            set { _tiketId = value; OnPropertyChanged(nameof(TiketId)); }
        }

        public int PembayaranId
        {
            get => _pembayaranId;
            set { _pembayaranId = value; OnPropertyChanged(nameof(PembayaranId)); }
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