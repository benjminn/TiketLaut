using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TiketLaut.Services;
using TiketLaut.Views.Components;
using System.Text.RegularExpressions; // Diperlukan untuk teks bold
using TiketLaut.Models; // Diperlukan untuk referensi class Notifikasi

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

                if (data == null || !data.Any())
                {
                    ShowEmpty();
                    return;
                }

                var sorted = data.OrderByDescending(n => n.waktu_kirim).ToList();
                for (int i = 0; i < sorted.Count; i++)
                {
                    AddItem(sorted[i]);
                    if (i < sorted.Count - 1)
                    {
                        // Menggunakan style separator dari XAML
                        notificationList.Children.Add(new Rectangle { Style = (Style)FindResource("SeparatorStyle") });
                    }
                }

                await _service.MarkAllAsReadAsync(_userId);
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

        // --- INI ADALAH AddItem DARI KODE ASLI ANDA ---
        // (Layout ini sudah benar sesuai keinginan Anda)
        private void AddItem(Notifikasi n)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Ikon generik kiri
            var icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Views/Assets/Icons/iconNotifikasi.png")), Width = 24, Height = 24, VerticalAlignment = VerticalAlignment.Top };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            var stack = new StackPanel();
            Grid.SetColumn(stack, 2);

            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };

            // Ikon kategori
            var catIcon = new Image { Source = new BitmapImage(new Uri($"pack://application:,,,/Views/Assets/Icons/{GetIcon(n.jenis_notifikasi)}")), Width = 22, Height = 22, Margin = new Thickness(0, 0, 8, 0) };
            titlePanel.Children.Add(catIcon);

            // Judul dan Timestamp
            var title = new TextBlock { FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Black, TextWrapping = TextWrapping.Wrap };
            title.Inlines.Add(new Run(n.judul_notifikasi));
            title.Inlines.Add(new Run(" · ") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00B4B5")) }); // Warna disesuaikan
            title.Inlines.Add(new Run(n.waktu_kirim.ToString("d MMMM")) { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00B4B5")) }); // Warna disesuaikan
            titlePanel.Children.Add(title);
            stack.Children.Add(titlePanel);

            // --- [INI BAGIAN YANG DIPERBAIKI] ---
            // Kita ganti TextBlock biasa dengan helper baru untuk teks bold
            string bodyText = GetDetail(n.pesan);
            TextBlock detail = BuildFormattedDetail(bodyText);
            // --- AKHIR PERBAIKAN ---

            stack.Children.Add(detail);

            grid.Children.Add(stack);
            notificationList.Children.Add(grid);
        }

        // --- [MODIFIKASI TOTAL PADA BuildFormattedDetail] ---
        // Mengganti Regex.Split dengan Regex.Matches untuk memperbaiki bug duplikasi
        private TextBlock BuildFormattedDetail(string bodyText)
        {
            var textBlock = new TextBlock
            {
                FontSize = 18,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 26 // Memberi sedikit spasi antar baris
            };

            // Pola regex untuk menemukan kata-kata yang ingin di-bold
            string pattern = @"(#[A-Z0-9-]+" + // Kode Tiket/Booking (cth: #TKT-123)
                             @"|\b[A-Za-z ]+ - [A-Za-z ]+\b" + // Rute (cth: Merak - Bakauheni)
                             @"|\b\d{1,2} \w+ \d{4} pukul \d{2}:\d{2}\b" + // Tgl & Jam (cth: 8 November 2025 pukul 08:00)
                             @"|\b\d{2}:\d{2} WIB \([^)]+\)\b" + // Jam & Delay (cth: 10:30 WIB (delay 1.5 jam))
                             @"|\b\d{2}:\d{2} WIB\b" + // Jam saja (cth: 14:00 WIB)
                             @"|\b1x24 jam\b" + // Teks spesifik
                             @"|\b2 jam\b" + // Teks spesifik
                             @"|\b24 jam\b" + // Teks spesifik
                             @")";

            // Menggunakan Regex.Matches untuk menemukan semua kecocokan
            var matches = Regex.Matches(bodyText, pattern, RegexOptions.IgnoreCase);

            int lastIndex = 0;

            foreach (Match match in matches)
            {
                // 1. Tambahkan teks BIASA sebelum kata kunci
                if (match.Index > lastIndex)
                {
                    textBlock.Inlines.Add(new Run(bodyText.Substring(lastIndex, match.Index - lastIndex)));
                }

                // 2. Tambahkan teks BOLD (kata kunci)
                textBlock.Inlines.Add(new Run(match.Value) { FontWeight = FontWeights.Bold });

                // 3. Update indeks terakhir
                lastIndex = match.Index + match.Length;
            }

            // 4. Tambahkan sisa teks BIASA setelah kata kunci terakhir
            if (lastIndex < bodyText.Length)
            {
                textBlock.Inlines.Add(new Run(bodyText.Substring(lastIndex)));
            }

            return textBlock;
        }


        // --- [KODE ASLI ANDA] ---
        // (Tidak diubah, sudah benar)
        private string GetIcon(string jenisNotifikasi)
        {
            // Deteksi icon berdasarkan jenis_notifikasi field
            return jenisNotifikasi.ToLower() switch
            {
                "pembayaran" => "iconPaymentNotif.png",    // pembayaran
                "pengingat" => "iconTimerNotif.png",        // countdown/pengingat
                "pemberitahuan" => "iconDangerNotif.png",   // warning/pemberitahuan
                "pembatalan" => "iconGagalNotif.png",      // pembatalan
                "umum" => "iconTaskNotif.png",           // tips/pengumuman
                _ => "iconNotifikasi.png"                 // default
            };
        }

        // --- [KODE ASLI ANDA] ---
        // Method GetTitle tidak terpakai tapi kita biarkan saja
        private string GetTitle(Notifikasi n)
        {
            var lines = n.pesan.Split(new[] { "\n\n" }, StringSplitOptions.None);
            if (lines.Length > 0)
            {
                var first = lines[0].Trim();
                var parts = first.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 1 ? parts[1].Trim() : first;
            }
            return "Notifikasi";
        }

        // --- [KODE ASLI ANDA] ---
        // (Sudah benar)
        private string GetDetail(string pesan)
        {
            var lines = pesan.Split(new[] { "\n\n" }, StringSplitOptions.None);

            // Jika formatnya benar (Judul\n\nIsi), ambil bagian Isi
            if (lines.Length >= 2)
            {
                return lines[1].Trim();
            }

            // Fallback jika tidak ada format \n\n (cth: notif manual lama)
            return pesan;
        }

        // Method GetTitle sudah tidak diperlukan lagi, Anda bisa menghapusnya
        /*
        private string GetTitle(Notifikasi n)
        {
            var lines = n.pesan.Split(new[] { "\n\n" }, StringSplitOptions.None);
            if (lines.Length > 0)
            {
                var first = lines[0].Trim();
                var parts = first.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 1 ? parts[1].Trim() : first;
            }
            return "Notifikasi";
        }
        */
    }
}