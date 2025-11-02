using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Services;

namespace TiketLaut.Views
{
    public partial class PaymentWindow : Window
    {
        private string selectedPaymentMethod = "";
        private bool isPaymentMethodSelected = false;
        private int kodeUnik = 0;
        private int hargaAsli = 487000;

        private Tiket? _tiket;
        private readonly PembayaranService _pembayaranService;
        private readonly BookingService _bookingService;

        public PaymentWindow()
        {
            InitializeComponent();
            _pembayaranService = new PembayaranService();
            _bookingService = new BookingService();

            ApplyResponsiveLayout();
            GenerateKodeUnik();
            LoadTiketData();
        }

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
                        "Tidak ditemukan tiket yang menunggu pembayaran.\nSilakan lakukan booking terlebih dahulu.",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error loading tiket: {ex.Message}");
            }
        }

        // ✅ ADD: Window_SizeChanged event handler
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void PaymentMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                selectedPaymentMethod = rb.Tag.ToString() ?? "";
                isPaymentMethodSelected = true;

                if (txtMainActionButton != null)
                    txtMainActionButton.Text = "Konfirmasi Pembayaran";

                if (selectedPaymentMethod == "BCA" || selectedPaymentMethod == "Mandiri")
                {
                    if (rbTransferBank != null)
                        rbTransferBank.IsChecked = true;
                }
                else if (selectedPaymentMethod == "Indomaret" || selectedPaymentMethod == "Alfamart")
                {
                    if (rbGeraiRetail != null)
                        rbGeraiRetail.IsChecked = true;
                }
                else
                {
                    if (rbTransferBank != null) rbTransferBank.IsChecked = false;
                    if (rbGeraiRetail != null) rbGeraiRetail.IsChecked = false;
                }

                UpdateBankInfo(selectedPaymentMethod);
            }
        }

        // ✅ ADD: PaymentMethod_Unchecked event handler
        private void PaymentMethod_Unchecked(object sender, RoutedEventArgs e)
        {
            // Optional: Handle unchecked state if needed
        }

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
                        txtKodeUnik.Text = $"Kode unik (+{kodeUnik}) pada 3 digit terakhir";
                }
            }
        }

        private async void BtnMainAction_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaymentMethodSelected)
            {
                MessageBox.Show("Silakan pilih metode pembayaran terlebih dahulu!", "Informasi",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (PaymentMethodsCard.Visibility == Visibility.Visible)
            {
                PaymentMethodsCard.Visibility = Visibility.Collapsed;
                PaymentInstructionsCard.Visibility = Visibility.Visible;
                txtMainActionButton.Text = "Konfirmasi Pembayaran";
            }
            else
            {
                await KonfirmasiPembayaranAsync();
            }
        }

        private async Task KonfirmasiPembayaranAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PaymentWindow] KonfirmasiPembayaranAsync started");

                if (_tiket == null)
                {
                    System.Diagnostics.Debug.WriteLine("[PaymentWindow] ERROR: _tiket is null");
                    MessageBox.Show(
                        "Data tiket tidak ditemukan!\nSilakan ulangi proses booking dari awal.",
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

                btnMainAction.IsEnabled = false;
                txtMainActionButton.Text = "Memproses pembayaran...";

                decimal jumlahBayar = hargaAsli + kodeUnik;
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Total jumlahBayar: {jumlahBayar}");

                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Calling _pembayaranService.CreatePembayaranAsync...");

                var pembayaran = await _pembayaranService.CreatePembayaranAsync(
                    tiketId: _tiket.tiket_id,
                    metodePembayaran: selectedPaymentMethod,
                    jumlahBayar: jumlahBayar
                );

                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] CreatePembayaranAsync completed");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] pembayaran.pembayaran_id: {pembayaran.pembayaran_id}");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] pembayaran.status_bayar: {pembayaran.status_bayar}");

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
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] InnerException: {ex.InnerException.Message}")
                        ;
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
            }
        }

        // ✅ ADD: BtnGantiMetode_Click event handler
        private void BtnGantiMetode_Click(object sender, RoutedEventArgs e)
        {
            PaymentInstructionsCard.Visibility = Visibility.Collapsed;
            PaymentMethodsCard.Visibility = Visibility.Visible;
            txtMainActionButton.Text = "Konfirmasi Pembayaran";
        }

        // ✅ ADD: BtnCaraMembayar_Click event handler
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
            int totalPembayaran = hargaAsli + kodeUnik;
            Clipboard.SetText(totalPembayaran.ToString());
            MessageBox.Show("Jumlah pembayaran telah disalin!", "Berhasil",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnToggleDetailPembayaran_Click(object sender, RoutedEventArgs e)
        {
            if (pnlDetailPembayaran.Visibility == Visibility.Collapsed)
            {
                pnlDetailPembayaran.Visibility = Visibility.Visible;
                AnimateArrow(180);

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
                AnimateArrow(0);
            }
        }

        // ✅ ADD: BtnKembali_Click event handler
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

        private void AnimateArrow(double to)
        {
            var rotateTransform = imgToggleDetailPembayaran.RenderTransform as System.Windows.Media.RotateTransform;
            if (rotateTransform != null)
            {
                var animation = new DoubleAnimation
                {
                    To = to,
                    Duration = TimeSpan.FromMilliseconds(200)
                };
                rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
            }
        }

        private void GenerateKodeUnik()
        {
            Random random = new Random();
            kodeUnik = random.Next(1, 1000);
            UpdateTotalPembayaran();
        }

        private void UpdateTotalPembayaran()
        {
            int totalPembayaran = hargaAsli + kodeUnik;
            string totalString = totalPembayaran.ToString();

            if (totalString.Length >= 3)
            {
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
            }
        }

        private void ApplyResponsiveLayout()
        {
            double windowWidth = this.ActualWidth;
            if (windowWidth < 1280)
                MainContentGrid.Margin = new Thickness(20, 15, 20, 20);
            else if (windowWidth < 1600)
                MainContentGrid.Margin = new Thickness(30, 20, 30, 25);
            else
                MainContentGrid.Margin = new Thickness(40, 20, 40, 30);
        }

        // ✅ ADD: Method untuk membuka PaymentWindow dari window lain
        public static void Open()
        {
            var paymentWindow = new PaymentWindow();
            paymentWindow.Show();
        }
    }
}