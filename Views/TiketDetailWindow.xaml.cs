using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class TiketDetailWindow : Window
    {
        private readonly TiketService _tiketService;
        private readonly RincianPenumpangService _rincianPenumpangService;
        private int _tiketId;
        private Tiket? _tiket;

        public TiketDetailWindow(int tiketId)
        {
            InitializeComponent();
            _tiketService = new TiketService();
            _rincianPenumpangService = new RincianPenumpangService();
            _tiketId = tiketId;

            // Set user info di navbar
            if (SessionManager.CurrentUser != null)
            {
                navbarPostLogin.SetUserInfo(SessionManager.CurrentUser.nama);
            }

            LoadTiketData();
        }

        private async void LoadTiketData()
        {
            try
            {
                // Load tiket dengan semua relasi
                _tiket = await _tiketService.GetTiketByIdAsync(_tiketId);

                if (_tiket == null)
                {
                    Components.CustomDialog.ShowError("Error", "Tiket tidak ditemukan!");
                    this.Close();
                    return;
                }

                var jadwal = _tiket.Jadwal;
                var pelabuhan_asal = jadwal.pelabuhan_asal;
                var pelabuhan_tujuan = jadwal.pelabuhan_tujuan;
                var kapal = jadwal.kapal;

                var offsetAsalHours = pelabuhan_asal?.TimezoneOffsetHours ?? 7;
                var offsetTujuanHours = pelabuhan_tujuan?.TimezoneOffsetHours ?? 7;
                
                var waktuBerangkatLocal = jadwal.waktu_berangkat.AddHours(offsetAsalHours);
                var waktuTibaLocal = jadwal.waktu_tiba.AddHours(offsetTujuanHours);

                // Set data ke UI
                txtKodeTiket.Text = _tiket.kode_tiket;
                txtPelabuhanAsal.Text = pelabuhan_asal?.nama_pelabuhan ?? "N/A";
                txtPelabuhanTujuan.Text = pelabuhan_tujuan?.nama_pelabuhan ?? "N/A";
                txtWaktuBerangkat.Text = waktuBerangkatLocal.ToString("HH:mm");
                txtWaktuTiba.Text = waktuTibaLocal.ToString("HH:mm");
                
                // Hitung durasi (actual duration dari UTC)
                var durasi = jadwal.waktu_tiba - jadwal.waktu_berangkat;
                txtDurasi.Text = $"{durasi.Hours}j {durasi.Minutes}m";

                var culture = new System.Globalization.CultureInfo("id-ID");
                txtTanggalBerangkat.Text = waktuBerangkatLocal.ToString("dddd, dd MMMM yyyy", culture);
                
                txtNamaKapal.Text = kapal.nama_kapal;
                txtTotalHarga.Text = $"Rp {_tiket.total_harga:N0}";

                var checkInTime = waktuBerangkatLocal.AddMinutes(-15);
                txtCheckInTime.Text = $"Harap tiba di pelabuhan {pelabuhan_asal?.nama_pelabuhan ?? "N/A"} sebelum {checkInTime:HH:mm} untuk proses check-in.";

                // Load penumpang
                await LoadPenumpangData();

                // Load kendaraan (jika ada)
                if (_tiket.jenis_kendaraan_enum != null && _tiket.jenis_kendaraan_enum != "Tidak Ada")
                {
                    panelKendaraan.Visibility = Visibility.Visible;
                    
                    // DEBUG: Log nilai asli dari database
                    
                    // Convert string dari DB ke ID, lalu ke display name
                    int jenisKendaraanId = GetJenisKendaraanIdFromString(_tiket.jenis_kendaraan_enum);
                    
                    string displayName;
                    if (jenisKendaraanId == -1)
                    {
                        // Tidak match dengan mapping baru, tampilkan apa adanya (tiket lama)
                        displayName = _tiket.jenis_kendaraan_enum;
                    }
                    else
                    {
                        displayName = GetJenisKendaraanDisplayNameById(jenisKendaraanId);
                    }
                    
                    txtJenisKendaraan.Text = displayName;
                    txtPlatNomor.Text = string.IsNullOrEmpty(_tiket.plat_nomor) ? "Tidak ada" : _tiket.plat_nomor;
                }
            }
            catch (Exception ex)
            {
                Components.CustomDialog.ShowError("Error", $"Gagal memuat data tiket:\n{ex.Message}");
            }
        }

        /// <summary>
        /// Convert string dari BookingService.GetJenisKendaraanText() ke ID integer
        /// HARUS MATCH PERSIS dengan BookingService!
        /// </summary>
        private int GetJenisKendaraanIdFromString(string jenisKendaraanText)
        {
            return jenisKendaraanText switch
            {
                "Pejalan Kaki" => 0,
                "Sepeda" => 1,
                "Sepeda Motor (<500cc)" => 2,
                "Sepeda Motor (>500cc)" => 3,
                "Mobil Penumpang" => 4,
                "Truk Pickup" => 5,
                "Bus Sedang" => 6,
                "Truk Sedang" => 7,
                "Bus Besar" => 8,
                "Truk Besar" => 9,
                "Truk Tronton" => 10,
                "Truk Tronton (<16 meter)" => 11,
                "Truk Tronton (>16 meter)" => 12,
                _ => -1 // Unknown
            };
        }

        /// <summary>
        /// Convert ID integer ke display name
        /// Mapping langsung dari ID database ke nama di popup ScheduleWindow & HomePage
        /// </summary>
        private string GetJenisKendaraanDisplayNameById(int id)
        {
            return id switch
            {
                0 => "Pejalan Kaki",
                1 => "Sepeda",
                2 => "Sepeda Motor (<500cc)",
                3 => "Sepeda Motor (>500cc)",
                4 => "Mobil Penumpang",
                5 => "Truk Pickup",
                6 => "Bus Sedang",
                7 => "Truk Sedang",
                8 => "Bus Besar",
                9 => "Truk Besar",
                10 => "Truk Tronton",
                11 => "Truk Tronton (<16 meter)",
                12 => "Truk Tronton (>16 meter)",
                _ => "Tidak Diketahui"
            };
        }

        private async System.Threading.Tasks.Task LoadPenumpangData()
        {
            try
            {
                var penumpangs = await _rincianPenumpangService.GetByTiketIdAsync(_tiketId);
                panelPenumpang.Children.Clear();

                int no = 1;
                foreach (var rincian in penumpangs)
                {
                    var penumpangCard = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9FAFB")),
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(20),
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var leftStack = new StackPanel();
                    
                    var namaPenumpang = new TextBlock
                    {
                        Text = $"Penumpang {no}",
                        FontSize = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    leftStack.Children.Add(namaPenumpang);

                    var nama = new TextBlock
                    {
                        Text = rincian.penumpang?.nama ?? "Nama tidak tersedia",
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#042769"))
                    };
                    leftStack.Children.Add(nama);

                    Grid.SetColumn(leftStack, 0);
                    grid.Children.Add(leftStack);

                    var rightStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
                    
                    var jenisLabel = new TextBlock
                    {
                        Text = "Jenis Kelamin",
                        FontSize = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                        Margin = new Thickness(0, 0, 0, 5),
                        TextAlignment = TextAlignment.Right
                    };
                    rightStack.Children.Add(jenisLabel);

                    var jenis = new TextBlock
                    {
                        Text = rincian.penumpang?.jenis_kelamin ?? "-",
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#042769")),
                        TextAlignment = TextAlignment.Right
                    };
                    rightStack.Children.Add(jenis);

                    Grid.SetColumn(rightStack, 1);
                    grid.Children.Add(rightStack);

                    penumpangCard.Child = grid;
                    panelPenumpang.Children.Add(penumpangCard);

                    no++;
                }
            }
            catch (Exception _)
            {
            }
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create SaveFileDialog
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"E-Tiket_{_tiket?.kode_tiket ?? "TiketLaut"}",
                    DefaultExt = ".png",
                    Filter = "PNG Image (.png)|*.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Render ticketCard to bitmap
                    var transform = ticketCard.LayoutTransform;
                    ticketCard.LayoutTransform = null;

                    var size = new System.Windows.Size(ticketCard.ActualWidth, ticketCard.ActualHeight);
                    ticketCard.Measure(size);
                    ticketCard.Arrange(new System.Windows.Rect(size));

                    var renderBitmap = new RenderTargetBitmap(
                        (int)ticketCard.ActualWidth,
                        (int)ticketCard.ActualHeight,
                        96d,
                        96d,
                        System.Windows.Media.PixelFormats.Pbgra32);

                    renderBitmap.Render(ticketCard);

                    // Save to file
                    using (var fileStream = new System.IO.FileStream(saveDialog.FileName, System.IO.FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                        encoder.Save(fileStream);
                    }

                    ticketCard.LayoutTransform = transform;

                    Components.CustomDialog.ShowSuccess(
                        "Download Berhasil",
                        $"E-Tiket berhasil disimpan ke:\n{saveDialog.FileName}",
                        Components.CustomDialog.DialogButtons.OK
                    );
                }
            }
            catch (Exception ex)
            {
                Components.CustomDialog.ShowError(
                    "Download Gagal",
                    $"Terjadi kesalahan saat menyimpan e-tiket:\n{ex.Message}",
                    Components.CustomDialog.DialogButtons.OK
                );
            }
        }

        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            // Kembali ke Cek Booking
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
}
