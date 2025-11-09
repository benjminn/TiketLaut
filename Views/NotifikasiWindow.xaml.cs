using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TiketLaut.Services;

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
                MessageBox.Show("Anda harus login terlebih dahulu!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        notificationList.Children.Add(new Rectangle { Fill = new SolidColorBrush(Color.FromRgb(162, 162, 162)), Height = 1, Margin = new Thickness(0, 20, 0, 20) });
                    }
                }
                
                await _service.MarkAllAsReadAsync(_userId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowEmpty()
        {
            var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 100, 0, 0) };
            panel.Children.Add(new TextBlock { Text = "🔔", FontSize = 64, TextAlignment = TextAlignment.Center });
            panel.Children.Add(new TextBlock { Text = "Belum ada notifikasi", FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Gray, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 0) });
            notificationList.Children.Add(panel);
        }

        private void AddItem(Notifikasi n)
        {
            var grid = new Grid();
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
            title.Inlines.Add(new Run(" · ") { Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)) });
            title.Inlines.Add(new Run(n.waktu_kirim.ToString("d MMMM")) { Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)) });
            titlePanel.Children.Add(title);
            stack.Children.Add(titlePanel);

            var detail = new TextBlock { FontSize = 18, Foreground = Brushes.Black, TextWrapping = TextWrapping.Wrap, LineHeight = 24, Text = GetDetail(n.pesan) };
            stack.Children.Add(detail);

            grid.Children.Add(stack);
            notificationList.Children.Add(grid);
        }

        private string GetIcon(string jenisNotifikasi)
        {
            // Deteksi icon berdasarkan jenis_notifikasi field
            return jenisNotifikasi.ToLower() switch
            {
                "pembayaran" => "iconPaymentNotif.png",      // pembayaran
                "pengingat" => "iconTimerNotif.png",         // countdown/pengingat
                "pemberitahuan" => "iconDangerNotif.png",    // warning/pemberitahuan
                "pembatalan" => "iconGagalNotif.png",        // pembatalan
                "umum" => "iconTaskNotif.png",               // tips/pengumuman
                _ => "iconNotifikasi.png"                    // default
            };
        }

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

        private string GetDetail(string pesan)
        {
            var lines = pesan.Split(new[] { "\n\n" }, StringSplitOptions.None);
            return lines.Length >= 2 ? lines[1].Trim() : pesan;
        }
    }
}
