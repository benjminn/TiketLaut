using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TiketLaut.Helpers;
using TiketLaut.Services;
using TiketLaut.Views.Components;
using System.Text.RegularExpressions;
using TiketLaut.Models;

namespace TiketLaut.Views
{
    public partial class NotifikasiWindow : Window
    {
        private readonly NotifikasiService _service;
        private readonly int _userId;

        public NotifikasiWindow()
        {
            InitializeComponent();
            _service = new NotifikasiService();
            
            // Enable zoom functionality
            ZoomHelper.EnableZoom(this);

            // Ambil user ID dari SessionManager
            if (SessionManager.IsLoggedIn && SessionManager.CurrentUser != null)
            {
                _userId = SessionManager.CurrentUser.pengguna_id;

                // Set user info di navbar
                navbarPostLogin.SetUserInfo(SessionManager.CurrentUser.nama);
            }
            else
            {
                CustomDialog.ShowError("Error", "Anda harus login terlebih dahulu!");
                this.Close();
                return;
            }

            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                notificationList.Children.Clear();

                var data = await _service.GetNotifikasiByPenggunaIdAsync(_userId);

                // ✅ DEBUGGING: Cek jumlah data yang di-fetch
                System.Diagnostics.Debug.WriteLine($"[LOAD DATA] Total notifikasi dari database: {data?.Count ?? 0}");

                if (data == null || !data.Any())
                {
                    ShowEmpty();
                    return;
                }

                var sorted = data.OrderByDescending(n => n.waktu_kirim).ToList();

                // ✅ DEBUGGING: Tampilkan semua notifikasi yang akan di-render
                foreach (var notif in sorted)
                {
                    System.Diagnostics.Debug.WriteLine($"[LOAD DATA] ID: {notif.notifikasi_id}, Judul: {notif.judul_notifikasi}, Status Baca: {notif.status_baca}, Jenis: {notif.jenis_notifikasi}");
                }

                for (int i = 0; i < sorted.Count; i++)
                {
                    // ✅ Pass index untuk menentukan corner radius
                    bool isFirst = (i == 0);
                    bool isLast = (i == sorted.Count - 1);

                    AddItem(sorted[i], isFirst, isLast);

                    if (i < sorted.Count - 1)
                    {
                        notificationList.Children.Add(new Rectangle { Style = (Style)FindResource("SeparatorStyle") });
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[LOAD DATA] Total notifikasi yang di-render: {notificationList.Children.Count / 2 + 1}");
            }
            catch (Exception ex)
            {
                CustomDialog.ShowError("Error", $"Error memuat notifikasi: {ex.Message}");
            }
        }

        private void ShowEmpty()
        {
            var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 100, 0, 0) };
            panel.Children.Add(new TextBlock { Text = "🔔", FontSize = 64, TextAlignment = TextAlignment.Center });
            panel.Children.Add(new TextBlock { Text = "Belum ada notifikasi", FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Gray, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 0) });
            notificationList.Children.Add(panel);
        }

        // ✅ Update method signature untuk terima parameter isFirst & isLast
        private void AddItem(Notifikasi n, bool isFirst = false, bool isLast = false)
        {
            System.Diagnostics.Debug.WriteLine($"Notifikasi ID: {n.notifikasi_id}, Status Baca: {n.status_baca}");

            // ✅ Tentukan corner radius berdasarkan posisi
            CornerRadius cornerRadius;
            if (isFirst && isLast)
            {
                // Jika hanya 1 notifikasi: rounded semua corner
                cornerRadius = new CornerRadius(0, 0, 41, 41);
            }
            else if (isFirst)
            {
                // Notifikasi pertama: tidak ada rounding (sudah ada rounding di header)
                cornerRadius = new CornerRadius(0);
            }
            else if (isLast)
            {
                // Notifikasi terakhir: rounded bottom
                cornerRadius = new CornerRadius(0, 0, 41, 41);
            }
            else
            {
                // Notifikasi tengah: tidak ada rounding
                cornerRadius = new CornerRadius(0);
            }

            var cardBorder = new Border
            {
                Background = n.status_baca
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(226, 247, 255)),
                CornerRadius = cornerRadius, // ✅ Apply corner radius
                Padding = new Thickness(40, 25, 40, 25),
                Margin = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var grid = new Grid
            {
                Margin = new Thickness(0)
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Views/Assets/Icons/iconNotifikasi.png")), Width = 24, Height = 24, VerticalAlignment = VerticalAlignment.Top };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            var stack = new StackPanel();
            Grid.SetColumn(stack, 2);

            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };

            var catIcon = new Image { Source = new BitmapImage(new Uri($"pack://application:,,,/Views/Assets/Icons/{GetIcon(n.jenis_notifikasi)}")), Width = 22, Height = 22, Margin = new Thickness(0, 0, 8, 0) };
            titlePanel.Children.Add(catIcon);

            var title = new TextBlock { FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Black, TextWrapping = TextWrapping.Wrap };
            title.Inlines.Add(new Run(n.judul_notifikasi));
            title.Inlines.Add(new Run(" · ") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00B4B5")) });
            title.Inlines.Add(new Run(n.waktu_kirim.ToString("d MMMM")) { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00B4B5")) });
            titlePanel.Children.Add(title);
            stack.Children.Add(titlePanel);

            string bodyText = GetDetail(n.pesan);
            TextBlock detail = BuildFormattedDetail(bodyText);
            stack.Children.Add(detail);

            grid.Children.Add(stack);
            cardBorder.Child = grid;

            cardBorder.MouseLeftButtonDown += async (s, e) =>
            {
                if (!n.status_baca)
                {
                    var success = await _service.MarkAsReadAsync(n.notifikasi_id);
                    System.Diagnostics.Debug.WriteLine($"Mark as read {n.notifikasi_id}: {success}");
                }

                await HandleNotificationClick(n);
            };

            notificationList.Children.Add(cardBorder);
        }

        // ✅ GANTI MessageBox jadi CustomDialog
        // ✅ LOGIC BARU: Redirect berdasarkan status pembayaran yang spesifik
        private async System.Threading.Tasks.Task HandleNotificationClick(Notifikasi n)
        {
            string redirectMessage = "";
            Action? redirectAction = null;

            // Cek judul notifikasi untuk menentukan status
            string judulLower = n.judul_notifikasi?.ToLower() ?? "";
            string jenisLower = n.jenis_notifikasi?.ToLower() ?? "";

            // ✅ PRIORITAS 1: Cek berdasarkan JENIS dulu, baru judul
            if (jenisLower == "pengingat")
            {
                // Semua notifikasi dengan jenis "pengingat" -> Redirect ke TiketDetailWindow
                redirectMessage = "Lihat detail tiket Anda?";
                redirectAction = () =>
                {
                    if (n.tiket_id.HasValue && n.tiket_id.Value > 0)
                    {
                        try
                        {
                            var tiketDetailWindow = new TiketDetailWindow(n.tiket_id.Value);
                            tiketDetailWindow.Show();
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            CustomDialog.ShowError("Error", $"Gagal membuka detail tiket: {ex.Message}");
                        }
                    }
                    else
                    {
                        CustomDialog.ShowError("Error", "Data tiket tidak ditemukan.");
                    }
                };
            }
            else if (judulLower.Contains("konfirmasi pembayaran") || judulLower.Contains("segera melakukan"))
            {
                // 1. Menunggu pembayaran -> Redirect ke PaymentWindow
                redirectMessage = "Menuju halaman pembayaran?";
                redirectAction = () =>
                {
                    var paymentWindow = new PaymentWindow();
                    paymentWindow.Show();
                    this.Close();
                };
            }
            else if (judulLower.Contains("menunggu validasi") || judulLower.Contains("diverifikasi"))
            {
                // 2. Menunggu validasi -> Redirect ke Cek Booking
                redirectMessage = "Menuju halaman Cek Booking?";
                redirectAction = () =>
                {
                    var cekBookingWindow = new CekBookingWindow();
                    cekBookingWindow.Show();
                    this.Close();
                };
            }
            else if (judulLower.Contains("berhasil dikonfirmasi") || judulLower.Contains("berhasil divalidasi") || judulLower.Contains("tiket anda sudah aktif"))
            {
                // 3. Pembayaran berhasil divalidasi -> Redirect ke TiketDetailWindow
                redirectMessage = "Lihat detail tiket Anda?";
                redirectAction = () =>
                {
                    if (n.tiket_id.HasValue && n.tiket_id.Value > 0)
                    {
                        try
                        {
                            var tiketDetailWindow = new TiketDetailWindow(n.tiket_id.Value);
                            tiketDetailWindow.Show();
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            CustomDialog.ShowError("Error", $"Gagal membuka detail tiket: {ex.Message}");
                        }
                    }
                    else
                    {
                        CustomDialog.ShowError("Error", "Data tiket tidak ditemukan.");
                    }
                };
            }
            else if (judulLower.Contains("pembatalan") || judulLower.Contains("dibatalkan") || jenisLower == "pembatalan")
            {
                // 5. Pembatalan -> Redirect ke History
                redirectMessage = "Lihat riwayat pemesanan?";
                redirectAction = () =>
                {
                    var historyWindow = new HistoryWindow();
                    historyWindow.Show();
                    this.Close();
                };
            }
            else if (jenisLower == "pemberitahuan" || jenisLower == "pembayaran")
            {
                // 6. Pemberitahuan/Pembayaran lainnya -> Redirect ke Cek Booking
                redirectMessage = "Menuju halaman Cek Booking?";
                redirectAction = () =>
                {
                    var cekBookingWindow = new CekBookingWindow();
                    cekBookingWindow.Show();
                    this.Close();
                };
            }
            else
            {
                // Default: Hanya mark as read, tidak redirect
                await LoadData();
                return;
            }

            // Show confirmation dialog
            if (redirectAction != null)
            {
                var result = CustomDialog.ShowQuestion("Konfirmasi", redirectMessage, CustomDialog.DialogButtons.YesNo);

                if (result == true)
                {
                    redirectAction.Invoke();
                }
                else
                {
                    // Tetap reload untuk update warna
                    await LoadData();
                }
            }
        }

        private TextBlock BuildFormattedDetail(string bodyText)
        {
            var textBlock = new TextBlock
            {
                FontSize = 18,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 26
            };

            // ✅ UPDATE PATTERN: Sesuai dengan design Figma
            string pattern = @"(" +
                    @"#[A-Z0-9-]+" +                                          // Kode tiket/booking (cth: #TKT-20251107-001)
                    @"|\b\s+[A-Z][A-Za-z]+\s*-\s*[A-Z][A-Za-z]+" +    // Rute dengan "jurusan" (cth: jurusan Merak - Bakauheni)
                    @"|\d{1,2}\s+\w+\s+\d{4}\s+\s+\d{2}:\d{2}" +        // Tanggal lengkap dengan waktu (cth: 8 November 2025 pukul 08:00)
                    @"|\d{1,2}\s+\w+\s+\d{4}" +                              // Tanggal (cth: 8 November 2025)
                    @"|\d{2}:\d{2}\s+WIB\s*\([^)]+\)" +                      // Waktu dengan info tambahan (cth: 10:30 WIB (delay 1.5 jam))
                    @"|\d{2}:\d{2}\s+WIB" +                                  // Waktu biasa (cth: 08:00 WIB, 14:00 WIB)
                    @")";

            var matches = Regex.Matches(bodyText, pattern, RegexOptions.IgnoreCase);
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    textBlock.Inlines.Add(new Run(bodyText.Substring(lastIndex, match.Index - lastIndex)));
                }

                textBlock.Inlines.Add(new Run(match.Value) { FontWeight = FontWeights.Bold });
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < bodyText.Length)
            {
                textBlock.Inlines.Add(new Run(bodyText.Substring(lastIndex)));
            }

            return textBlock;
        }

        private string GetIcon(string jenisNotifikasi)
        {
            return jenisNotifikasi.ToLower() switch
            {
                "pembayaran" => "iconPaymentNotif.png",
                "pengingat" => "iconTimerNotif.png",
                "pemberitahuan" => "iconDangerNotif.png",
                "pembatalan" => "iconGagalNotif.png",
                "umum" => "iconTaskNotif.png",
                _ => "iconNotifikasi.png"
            };
        }

        private string GetDetail(string pesan)
        {
            var lines = pesan.Split(new[] { "\n\n" }, StringSplitOptions.None);
            if (lines.Length >= 2)
            {
                return lines[1].Trim();
            }
            return pesan;
        }
    }
}