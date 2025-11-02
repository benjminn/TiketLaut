using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
<<<<<<< Updated upstream
=======
using Microsoft.EntityFrameworkCore;
>>>>>>> Stashed changes
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class PaymentWindow : Window
    {
        private string selectedPaymentMethod = "";
        private bool isPaymentMethodSelected = false;
        private int kodeUnik = 0;
<<<<<<< Updated upstream
        private decimal totalHarga = 0;

        // NEW: Payment tracking data
        private int _tiketId;
        private int _pembayaranId;
        private string _paymentReference = "";
        private readonly PaymentService _paymentService;
=======
        private int hargaAsli = 487000;
        
        // ADD: Properties untuk data tiket
        private Tiket? _tiket;
        private readonly PembayaranService _pembayaranService;
        private readonly BookingService _bookingService;
>>>>>>> Stashed changes

        public PaymentWindow()
        {
            InitializeComponent();
            _pembayaranService = new PembayaranService();
            _bookingService = new BookingService();
            
            ApplyResponsiveLayout();
            GenerateKodeUnik();
<<<<<<< Updated upstream

            _paymentService = new PaymentService();
=======
            
            // Load tiket data dari session
            LoadTiketData();
        }

        /// <summary>
        /// Load data tiket terbaru dari session user
        /// </summary>
        private async void LoadTiketData()
        {
            try
            {
                if (SessionManager.CurrentUser == null)
                {
                    MessageBox.Show("Session user tidak ditemukan!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ FIX: Add using Microsoft.EntityFrameworkCore for .Include() and .Where()
                var tikets = await DatabaseService.GetContext().Tikets
                    .Include(t => t.Jadwal)
                    .Where(t => t.pengguna_id == SessionManager.CurrentUser.pengguna_id &&
                               t.status_tiket == "Menunggu Pembayaran")
                    .OrderByDescending(t => t.tanggal_pemesanan)
                    .ToListAsync();

                if (tikets.Any())
                {
                    _tiket = tikets.First();
                    hargaAsli = (int)_tiket.total_harga;
                    UpdateTotalPembayaran();

                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Loaded tiket: {_tiket.kode_tiket}, Total: {_tiket.total_harga}");
                }
                else
                {
                    MessageBox.Show(
                        "Tidak ditemukan tiket yang menunggu pembayaran.\n" +
                        "Silakan lakukan booking terlebih dahulu.",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error loading tiket: {ex.Message}");
            }
>>>>>>> Stashed changes
        }

        /// <summary>
        /// NEW: Set tiket ID dari BookingDetailWindow
        /// </summary>
        public void SetTiketData(int tiketId, decimal totalHarga)
        {
            _tiketId = tiketId;
            this.totalHarga = totalHarga;

            // Update UI dengan harga yang benar
            UpdateTotalPembayaran();
        }

        private void GenerateKodeUnik()
        {
            Random random = new Random();
            kodeUnik = random.Next(1, 1000);
            UpdateTotalPembayaran();
        }

        private void UpdateTotalPembayaran()
        {
            decimal total = totalHarga + kodeUnik;
            string totalString = total.ToString("F0");
            
            if (totalString.Length >= 3)
            {
<<<<<<< Updated upstream
                string mainPart = totalString.Substring(0, totalString.Length - 3);
                string lastThreeDigits = totalString.Substring(totalString.Length - 3);

                if (txtTotalPembayaran != null)
                {
                    txtTotalPembayaran.Text = $"IDR {mainPart}.";
                    txtTotalPembayaranDigit.Text = lastThreeDigits;
                }

                if (txtDetailTotalPembayaran != null)
                {
                    txtDetailTotalPembayaran.Text = $"IDR {mainPart}.";
                    txtDetailTotalPembayaranDigit.Text = lastThreeDigits;
                }
=======
                MainContentGrid.Margin = new Thickness(20, 15, 20, 20);
            }
            else if (windowWidth < 1600)
            {
                MainContentGrid.Margin = new Thickness(30, 20, 30, 25);
            }
            else
            {
                MainContentGrid.Margin = new Thickness(40, 20, 40, 30);
>>>>>>> Stashed changes
            }
        }

        private void PaymentMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                selectedPaymentMethod = rb.Tag.ToString() ?? ""; // ✅ FIX: Handle null
                isPaymentMethodSelected = true;

                if (txtMainActionButton != null)
                {
                    txtMainActionButton.Text = "Konfirmasi Pembayaran";
                }

                // Auto-check parent radio button
                if (selectedPaymentMethod == "BCA" || selectedPaymentMethod == "Mandiri")
                {
<<<<<<< Updated upstream
                    if (rbTransferBank != null) rbTransferBank.IsChecked = true;
                }
                else if (selectedPaymentMethod == "Indomaret" || selectedPaymentMethod == "Alfamart")
                {
                    if (rbGeraiRetail != null) rbGeraiRetail.IsChecked = true;
=======
                    if (rbTransferBank != null)
                    {
                        rbTransferBank.IsChecked = true;
                    }
                }
                else if (selectedPaymentMethod == "Indomaret" || selectedPaymentMethod == "Alfamart")
                {
                    if (rbGeraiRetail != null)
                    {
                        rbGeraiRetail.IsChecked = true;
                    }
>>>>>>> Stashed changes
                }
                else
                {
                    if (rbTransferBank != null) rbTransferBank.IsChecked = false;
                    if (rbGeraiRetail != null) rbGeraiRetail.IsChecked = false;
                }

                UpdateBankInfo(selectedPaymentMethod);
            }
        }

        private void PaymentMethod_Unchecked(object sender, RoutedEventArgs e) { }

        private void UpdateBankInfo(string paymentMethod)
        {
            if (txtBankName == null || imgSelectedBank == null) return;

            bool isTransferBank = (paymentMethod == "BCA" || paymentMethod == "Mandiri");

            switch (paymentMethod)
            {
                case "BCA":
                    txtBankName.Text = "Bank BCA";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/BCA.png", UriKind.Relative));
                    txtAccountNumber.Text = "385 0833 817";
                    break;
                case "Mandiri":
                    txtBankName.Text = "Bank Mandiri";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/Mandiri.png", UriKind.Relative));
                    txtAccountNumber.Text = "142 0099 123456";
                    break;
                case "Indomaret":
                    txtBankName.Text = "Indomaret";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/Indomaret.png", UriKind.Relative));
                    txtAccountNumber.Text = "Kode: INDOM12345";
                    break;
                case "Alfamart":
                    txtBankName.Text = "Alfamart";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/Alfamart.png", UriKind.Relative));
                    txtAccountNumber.Text = "Kode: ALFA67890";
                    break;
                default:
                    txtBankName.Text = "Metode Pembayaran";
                    txtAccountNumber.Text = "-";
                    break;
            }

            if (pnlDetailPembayaran != null && pnlDetailPembayaran.Visibility == Visibility.Visible)
            {
                if (txtKodeUnik != null)
                {
                    txtKodeUnik.Visibility = isTransferBank ? Visibility.Visible : Visibility.Collapsed;
                    if (isTransferBank)
                    {
                        txtKodeUnik.Text = $"Kode unik (+{kodeUnik}) pada 3 digit terakhir";
                    }
                }
            }
        }

<<<<<<< Updated upstream
=======
        // ✅ FIX: Change to async void (event handler)
>>>>>>> Stashed changes
        private async void BtnMainAction_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaymentMethodSelected)
            {
                MessageBox.Show(
                    "Silakan pilih metode pembayaran terlebih dahulu!",
                    "Informasi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (PaymentMethodsCard.Visibility == Visibility.Visible)
            {
<<<<<<< Updated upstream
                // SAVE TO DATABASE
                try
                {
                    btnMainAction.IsEnabled = false;
                    txtMainActionButton.Text = "Memproses...";

                    // Create pembayaran
                    var pembayaran = await _paymentService.CreatePembayaranAsync(
                        _tiketId,
                        selectedPaymentMethod,
                        kodeUnik);

                    _pembayaranId = pembayaran.pembayaran_id;
                    _paymentReference = _paymentService.GeneratePaymentReference(
                        _pembayaranId,
                        selectedPaymentMethod);

                    // Show payment instructions
                    PaymentMethodsCard.Visibility = Visibility.Collapsed;
                    PaymentInstructionsCard.Visibility = Visibility.Visible;
                    txtMainActionButton.Text = "Cek Status Pembayaran";

                    MessageBox.Show(
                        $"✅ Pembayaran berhasil dibuat!\n\n" +
                        $"Payment Ref: {_paymentReference}\n" +
                        $"Metode: {selectedPaymentMethod}\n" +
                        $"Total: Rp {pembayaran.jumlah_bayar:N0}\n\n" +
                        $"Silakan selesaikan pembayaran sebelum {_paymentService.GetPaymentExpiry():dd/MM/yyyy HH:mm}",
                        "Pembayaran Dibuat",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"❌ Terjadi kesalahan:\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    btnMainAction.IsEnabled = true;
                    txtMainActionButton.Text = "Konfirmasi Pembayaran";
                }
            }
            else
            {
                // Check payment status
                await CheckPaymentStatus();
=======
                // Show payment instructions
                PaymentMethodsCard.Visibility = Visibility.Collapsed;
                PaymentInstructionsCard.Visibility = Visibility.Visible;
                txtMainActionButton.Text = "Konfirmasi Pembayaran";
            }
            else
            {
                // ===== SIMPAN PEMBAYARAN KE DATABASE =====
                await KonfirmasiPembayaranAsync();
            }
        }

        /// <summary>
        /// Konfirmasi pembayaran dan simpan ke database
        /// </summary>
        private async Task KonfirmasiPembayaranAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PaymentWindow] KonfirmasiPembayaranAsync started");

                // Validasi tiket ada
                if (_tiket == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PaymentWindow] ERROR: _tiket is null");
                    MessageBox.Show(
                        "Data tiket tidak ditemukan!\n" +
                        "Silakan ulangi proses booking dari awal.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] _tiket found: {_tiket.kode_tiket}");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] _tiket.tiket_id: {_tiket.tiket_id}");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] selectedPaymentMethod: {selectedPaymentMethod}");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] hargaAsli: {hargaAsli}");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] kodeUnik: {kodeUnik}");

                // Show loading
                btnMainAction.IsEnabled = false;
                txtMainActionButton.Text = "Memproses pembayaran...";

                // Hitung jumlah bayar (harga asli + kode unik)
                decimal jumlahBayar = hargaAsli + kodeUnik;
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Total jumlahBayar: {jumlahBayar}");

                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Calling _pembayaranService.CreatePembayaranAsync...");

                // Simpan pembayaran ke database
                var pembayaran = await _pembayaranService.CreatePembayaranAsync(
                    tiketId: _tiket.tiket_id,
                    metodePembayaran: selectedPaymentMethod,
                    jumlahBayar: jumlahBayar
                );

                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] CreatePembayaranAsync completed");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] pembayaran.pembayaran_id: {pembayaran.pembayaran_id}");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] pembayaran.status_bayar: {pembayaran.status_bayar}");

                // Success message
                MessageBox.Show(
                    $"✅ Pembayaran berhasil dikonfirmasi!\n\n" +
                    $"Kode Tiket: {_tiket.kode_tiket}\n" +
                    $"Metode: {selectedPaymentMethod}\n" +
                    $"Jumlah: Rp {jumlahBayar:N0}\n" +
                    $"Status: {pembayaran.status_bayar}\n\n" +
                    $"Pembayaran Anda sedang diverifikasi oleh admin.\n" +
                    $"Anda dapat mengecek status di menu 'Cek Booking'.",
                    "Pembayaran Berhasil",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Navigate ke CekBookingWindow
                var cekBookingWindow = new CekBookingWindow();
                cekBookingWindow.Left = this.Left;
                cekBookingWindow.Top = this.Top;
                cekBookingWindow.Width = this.Width;
                cekBookingWindow.Height = this.Height;
                cekBookingWindow.WindowState = this.WindowState;
                cekBookingWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] EXCEPTION in KonfirmasiPembayaranAsync:");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] StackTrace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] InnerException: {ex.InnerException.Message}");
                }

                MessageBox.Show(
                    $"❌ Terjadi kesalahan saat memproses pembayaran:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                btnMainAction.IsEnabled = true;
                txtMainActionButton.Text = "Konfirmasi Pembayaran";
>>>>>>> Stashed changes
            }
        }

        private async System.Threading.Tasks.Task CheckPaymentStatus()
        {
            if (_pembayaranId == 0)
            {
                MessageBox.Show("Pembayaran belum dibuat!", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var status = await _paymentService.CheckStatusPembayaranAsync(_pembayaranId);

            MessageBox.Show(
                $"📊 Status Pembayaran\n\n" +
                $"Payment Ref: {_paymentReference}\n" +
                $"Status: {status}\n\n" +
                $"Untuk konfirmasi pembayaran, hubungi admin atau tunggu verifikasi otomatis.",
                "Status Pembayaran",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnGantiMetode_Click(object sender, RoutedEventArgs e)
        {
            PaymentInstructionsCard.Visibility = Visibility.Collapsed;
            PaymentMethodsCard.Visibility = Visibility.Visible;
            txtMainActionButton.Text = "Konfirmasi Pembayaran";
        }

        private void BtnCaraMembayar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                $"Cara pembayaran untuk {selectedPaymentMethod}:\n\n" +
                "1. Buka aplikasi mobile banking atau ATM\n" +
                "2. Pilih menu Transfer\n" +
                "3. Masukkan nomor rekening tujuan\n" +
                "4. Masukkan nominal sesuai yang tertera\n" +
                "5. Konfirmasi transaksi\n" +
                "6. Simpan bukti transfer",
                "Cara Membayar",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnCopyAccount_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtAccountNumber.Text))
            {
                Clipboard.SetText(txtAccountNumber.Text);
                MessageBox.Show("Nomor rekening telah disalin!", "Berhasil",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCopyAmount_Click(object sender, RoutedEventArgs e)
        {
<<<<<<< Updated upstream
            decimal total = totalHarga + kodeUnik;
            Clipboard.SetText(total.ToString("F0"));
            MessageBox.Show("Jumlah pembayaran telah disalin!", "Berhasil",
                MessageBoxButton.OK, MessageBoxImage.Information);
=======
            int totalPembayaran = hargaAsli + kodeUnik;
            Clipboard.SetText(totalPembayaran.ToString());
            MessageBox.Show(
                "Jumlah pembayaran telah disalin!",
                "Berhasil",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void GenerateKodeUnik()
        {
            // Generate kode unik random antara 1-999
            Random random = new Random();
            kodeUnik = random.Next(1, 1000);

            // Update total pembayaran dengan kode unik
            UpdateTotalPembayaran();
        }

        private void UpdateTotalPembayaran()
        {
            int totalPembayaran = hargaAsli + kodeUnik;
            string totalString = totalPembayaran.ToString();
            string mainPart = totalString.Substring(0, totalString.Length - 3);
            string lastThreeDigits = totalString.Substring(totalString.Length - 3);

            // Update semua TextBlock total pembayaran
            if (txtTotalPembayaran != null)
            {
                txtTotalPembayaran.Text = $"IDR {mainPart}.";
                txtTotalPembayaranDigit.Text = lastThreeDigits;
            }

            if (txtDetailTotalPembayaran != null)
            {
                txtDetailTotalPembayaran.Text = $"IDR {mainPart}.";
                txtDetailTotalPembayaranDigit.Text = lastThreeDigits;
            }
>>>>>>> Stashed changes
        }

        private void BtnToggleDetailPembayaran_Click(object sender, RoutedEventArgs e)
        {
            if (pnlDetailPembayaran.Visibility == Visibility.Collapsed)
            {
                pnlDetailPembayaran.Visibility = Visibility.Visible;
<<<<<<< Updated upstream
                
=======

                // Rotate arrow icon
>>>>>>> Stashed changes
                var rotateTransform = imgToggleDetailPembayaran.RenderTransform as System.Windows.Media.RotateTransform;
                if (rotateTransform != null)
                {
                    var animation = new DoubleAnimation
                    {
                        To = 180,
                        Duration = TimeSpan.FromMilliseconds(200)
                    };
                    rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
                }
<<<<<<< Updated upstream
                
=======

                // Show kode unik notification if transfer bank is selected
>>>>>>> Stashed changes
                if (selectedPaymentMethod == "BCA" || selectedPaymentMethod == "Mandiri")
                {
                    if (txtKodeUnik != null)
                    {
                        txtKodeUnik.Visibility = Visibility.Visible;
                        txtKodeUnik.Text = $"Kode unik (+{kodeUnik}) pada 3 digit terakhir";
                    }
                }
            }
            else
            {
                pnlDetailPembayaran.Visibility = Visibility.Collapsed;
<<<<<<< Updated upstream
                
=======

                // Rotate arrow icon back
>>>>>>> Stashed changes
                var rotateTransform = imgToggleDetailPembayaran.RenderTransform as System.Windows.Media.RotateTransform;
                if (rotateTransform != null)
                {
                    var animation = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(200)
                    };
                    rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
                }
            }
        }

<<<<<<< Updated upstream
        // Method untuk membuka PaymentWindow dari window lain
        public static void Open()
=======
        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Apakah Anda yakin membatalkan pembayaran?",
                "Konfirmasi Pembatalan",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var scheduleWindow = new ScheduleWindow();
                scheduleWindow.Left = this.Left;
                scheduleWindow.Top = this.Top;
                scheduleWindow.Width = this.Width;
                scheduleWindow.Height = this.Height;
                scheduleWindow.WindowState = this.WindowState;
                scheduleWindow.Show();
                this.Close();
            }
        }

<<<<<<< Updated upstream
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
>>>>>>> Stashed changes
=======
        // Method untuk membuka PaymentWindow dari window lain
        public static void Open()
>>>>>>> Stashed changes
        {
            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            double windowWidth = this.ActualWidth;

            if (windowWidth < 1280)
            {
                MainContentGrid.Margin = new Thickness(20, 15, 20, 20);
            }
            else if (windowWidth < 1600)
            {
                MainContentGrid.Margin = new Thickness(30, 20, 30, 25);
            }
            else
            {
                MainContentGrid.Margin = new Thickness(40, 20, 40, 30);
            }
        }
    }
}