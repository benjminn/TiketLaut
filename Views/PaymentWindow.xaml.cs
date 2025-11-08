using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Services;
using TiketLaut.Views;

namespace TiketLaut.Views
{
    public partial class PaymentWindow : Window
    {
        private string selectedPaymentMethod = "";
        private bool isPaymentMethodSelected = false;
        private int kodeUnik = 0;
        private decimal hargaAsli = 0;

        private Tiket? _tiket;
        private readonly PembayaranService _pembayaranService;
        private readonly BookingService _bookingService;

        private Pembayaran? _currentPembayaran = null;
        private DispatcherTimer? _countdownTimer;
        private DateTime _paymentDeadline;

        public PaymentWindow()
        {
            InitializeComponent();
            _pembayaranService = new PembayaranService();
            _bookingService = new BookingService();

            ApplyResponsiveLayout();
            GenerateKodeUnik();
            InitializeCountdownTimer();
            LoadTiketData();
        }

        private void InitializeCountdownTimer()
        {
            _paymentDeadline = DateTime.Now.AddHours(24);
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
            UpdateCountdownDisplay();
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            UpdateCountdownDisplay();
        }

        private async void UpdateCountdownDisplay()
        {
            var timeRemaining = _paymentDeadline - DateTime.Now;

            if (timeRemaining.TotalSeconds <= 0)
            {
                _countdownTimer?.Stop();
                var deadlineCard = FindName("DeadlineCard") as Border;
                if (deadlineCard != null)
                {
                    deadlineCard.BorderBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(220, 38, 38)); // Red color
                }
                if (txtDeadlineTime != null)
                {
                    txtDeadlineTime.Text = "WAKTU PEMBAYARAN TELAH HABIS";
                    txtDeadlineTime.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(220, 38, 38));
                }
                if (btnMainAction != null)
                {
                    btnMainAction.IsEnabled = false;
                    txtMainActionButton.Text = "Waktu Pembayaran Habis";
                }

                await MarkPaymentAsFailedDueToTimeout();

                MessageBox.Show(
                    "Waktu pembayaran telah berakhir. Pembayaran Anda telah dibatalkan secara otomatis.\nSilakan lakukan booking ulang.",
                    "Waktu Habis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                var scheduleWindow = new ScheduleWindow();
                scheduleWindow.Left = this.Left;
                scheduleWindow.Top = this.Top;
                scheduleWindow.Width = this.Width;
                scheduleWindow.Height = this.Height;
                scheduleWindow.WindowState = this.WindowState;
                scheduleWindow.Show();
                this.Close();
                return;
            }

            string countdownText;
            if (timeRemaining.Days > 0)
            {
                countdownText = $"{timeRemaining.Days} hari {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
            }
            else
            {
                countdownText = $"{timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
            }

            if (txtDeadlineTime != null)
            {
                var culture = new System.Globalization.CultureInfo("id-ID");
                var deadlineFormatted = _paymentDeadline.ToString("dd MMMM yyyy, HH:mm", culture);
                txtDeadlineTime.Text = $"{deadlineFormatted} WIB ({countdownText})";
            }
        }

        private async void PaymentMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                string newPaymentMethod = rb.Tag.ToString() ?? "";
                if (selectedPaymentMethod == newPaymentMethod) return;

                selectedPaymentMethod = newPaymentMethod;
                isPaymentMethodSelected = true;

                if (txtMainActionButton != null)
                    txtMainActionButton.Text = "Konfirmasi Pembayaran";

                if (IsTransferBankMethod()) 
                {
                    if (rbTransferBank != null)
                        rbTransferBank.IsChecked = true;
                }
                else if (selectedPaymentMethod.StartsWith("Retail")) 
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
                UpdateTotalPembayaran();

                await HandlePaymentMethodSelection();
            }
        }

        private async Task HandlePaymentMethodSelection()
        {
            // Method ini sudah benar, tidak perlu diubah
            try
            {
                if (_tiket == null) return;
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Payment method selected: {selectedPaymentMethod}");
                decimal jumlahBayar = CalculateTotalPayment();

                if (_currentPembayaran == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Creating new payment record...");
                    _currentPembayaran = await _pembayaranService.CreatePembayaranAsync(
                        tiketId: _tiket.tiket_id,
                        metodePembayaran: selectedPaymentMethod,
                        jumlahBayar: jumlahBayar
                    );
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] ✅ Created payment ID: {_currentPembayaran.pembayaran_id}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Updating existing payment record...");
                    bool updated = await _pembayaranService.UpdatePembayaranMethodAsync(
                        pembayaranId: _currentPembayaran.pembayaran_id,
                        newMethodePembayaran: selectedPaymentMethod,
                        newJumlahBayar: jumlahBayar
                    );

                    if (updated)
                    {
                        _currentPembayaran.metode_pembayaran = selectedPaymentMethod;
                        _currentPembayaran.jumlah_bayar = jumlahBayar;
                        System.Diagnostics.Debug.WriteLine($"[PaymentWindow] ✅ Updated payment method successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[PaymentWindow] ❌ Failed to update payment method");
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Current payment status: {_currentPembayaran?.status_bayar}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error handling payment method selection: {ex.Message}");
                MessageBox.Show($"Terjadi kesalahan saat memproses metode pembayaran: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void LoadTiketData()
        {
            // Method ini sudah benar, tidak perlu diubah
            try
            {
                if (SessionManager.CurrentUser == null)
                {
                    MessageBox.Show("Session user tidak ditemukan!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var tikets = await DatabaseService.GetContext().Tikets
                    .Include(t => t.Jadwal).ThenInclude(j => j.pelabuhan_asal)
                    .Include(t => t.Jadwal).ThenInclude(j => j.pelabuhan_tujuan)
                    .Include(t => t.Jadwal).ThenInclude(j => j.kapal)
                    .Include(t => t.Jadwal).ThenInclude(j => j.GrupKendaraan).ThenInclude(gk => gk != null ? gk.DetailKendaraans : null)
                    .Include(t => t.RincianPenumpangs).ThenInclude(rp => rp.penumpang)
                    .Where(t => t.pengguna_id == SessionManager.CurrentUser.pengguna_id &&
                                 t.status_tiket == "Menunggu Pembayaran")
                    .OrderByDescending(t => t.tanggal_pemesanan)
                    .ToListAsync();

                if (tikets.Any())
                {
                    _tiket = tikets.First();
                    hargaAsli = _tiket.total_harga;
                    _currentPembayaran = await _pembayaranService.GetPembayaranByTiketIdAsync(_tiket.tiket_id);

                    if (_currentPembayaran != null)
                    {
                        if (!string.IsNullOrEmpty(_currentPembayaran.metode_pembayaran))
                        {
                            selectedPaymentMethod = _currentPembayaran.metode_pembayaran;
                            isPaymentMethodSelected = true;
                            RestorePaymentMethodSelection(_currentPembayaran.metode_pembayaran);
                            UpdateBankInfo(selectedPaymentMethod);
                        }
                    }
                    UpdateUIWithTicketData();
                    UpdateTotalPembayaran();
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
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void RestorePaymentMethodSelection(string paymentMethod)
        {
            // Method ini sudah benar, tidak perlu diubah
            try
            {
                switch (paymentMethod)
                {
                    case "Transfer BCA": // Anda sudah ganti ini, bagus
                        if (FindName("rbBCA") is RadioButton rbBCA) rbBCA.IsChecked = true;
                        break;
                    case "Transfer Mandiri": // Anda sudah ganti ini, bagus
                        if (FindName("rbMandiri") is RadioButton rbMandiri) rbMandiri.IsChecked = true;
                        break;
                    case "Retail Indomaret": // Anda sudah ganti ini, bagus
                        if (FindName("rbIndomaret") is RadioButton rbIndomaret) rbIndomaret.IsChecked = true;
                        break;
                    case "Retail Alfamart": // Anda sudah ganti ini, bagus
                        if (FindName("rbAlfamart") is RadioButton rbAlfamart) rbAlfamart.IsChecked = true;
                        break;
                }
                if (txtMainActionButton != null)
                    txtMainActionButton.Text = "Konfirmasi Pembayaran";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error restoring payment method: {ex.Message}");
            }
        }

        private async Task KonfirmasiPembayaranAsync()
        {
            // Method ini sudah benar, tidak perlu diubah
            try
            {
                System.Diagnostics.Debug.WriteLine("[PaymentWindow] KonfirmasiPembayaranAsync started");
                if (_tiket == null || _currentPembayaran == null)
                {
                    MessageBox.Show(
                        "Data pembayaran tidak ditemukan!\nSilakan pilih metode pembayaran terlebih dahulu.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
                btnMainAction.IsEnabled = false;
                txtMainActionButton.Text = "Memproses pembayaran...";

                bool updated = await _pembayaranService.UpdateStatusPembayaranAsync(
                    _currentPembayaran.pembayaran_id,
                    "Menunggu Validasi"
                );

                if (updated)
                {
                    _currentPembayaran.status_bayar = "Menunggu Validasi";
                    _countdownTimer?.Stop();
                    MessageBox.Show(
                        $"✅ Pembayaran berhasil dikonfirmasi!\n\n" +
                        $"Kode Tiket: {_tiket.kode_tiket}\n" +
                        $"Metode: {selectedPaymentMethod}\n" +
                        $"Jumlah: Rp {_currentPembayaran.jumlah_bayar:N0}\n" +
                        $"Status: {_currentPembayaran.status_bayar}\n\n" +
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
                else
                {
                    MessageBox.Show(
                        "Gagal memperbarui status pembayaran. Silakan coba lagi.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] EXCEPTION in KonfirmasiPembayaranAsync:");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Message: {ex.Message}");
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

        private async Task MarkPaymentAsFailedDueToTimeout()
        {
            // Method ini sudah benar, tidak perlu diubah
            try
            {
                if (_tiket == null) return;
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Marking payment as failed due to timeout for tiket: {_tiket.kode_tiket}");

                if (_currentPembayaran != null)
                {
                    await _pembayaranService.UpdateStatusPembayaranAsync(_currentPembayaran.pembayaran_id, "Gagal");
                }
                else
                {
                    _currentPembayaran = await _pembayaranService.CreatePembayaranAsync(
                        tiketId: _tiket.tiket_id,
                        metodePembayaran: string.IsNullOrEmpty(selectedPaymentMethod) ? "Timeout" : selectedPaymentMethod,
                        jumlahBayar: _tiket.total_harga
                    );
                    await _pembayaranService.UpdateStatusPembayaranAsync(_currentPembayaran.pembayaran_id, "Gagal");
                }
                _tiket.status_tiket = "Gagal";
                DatabaseService.GetContext().Tikets.Update(_tiket);
                await DatabaseService.GetContext().SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Payment marked as failed due to timeout");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error marking payment as failed: {ex.Message}");
            }
        }

        private void UpdateUIWithTicketData()
        {
            // Method ini sudah benar, tidak perlu diubah
            if (_tiket?.Jadwal == null) return;
            try
            {
                var jadwal = _tiket.Jadwal;
                if (txtOrderId != null)
                {
                    txtOrderId.Text = $"Order ID: {_tiket.kode_tiket}";
                }
                if (txtFerryType != null)
                {
                    txtFerryType.Text = jadwal.kelas_layanan ?? "Reguler";
                }
                if (txtDeparturePort != null && jadwal.pelabuhan_asal != null)
                {
                    txtDeparturePort.Text = jadwal.pelabuhan_asal.nama_pelabuhan;
                }
                if (txtArrivalPort != null && jadwal.pelabuhan_tujuan != null)
                {
                    txtArrivalPort.Text = jadwal.pelabuhan_tujuan.nama_pelabuhan;
                }
                if (txtDateTime != null)
                {
                    var culture = new System.Globalization.CultureInfo("id-ID");
                    string dateFormatted;
                    if (SessionManager.LastSearchCriteria?.TanggalKeberangkatan != null)
                    {
                        dateFormatted = SessionManager.LastSearchCriteria.TanggalKeberangkatan.ToString("ddd, dd MMM yyyy", culture);
                    }
                    else
                    {
                        dateFormatted = _tiket.tanggal_pemesanan.ToString("ddd, dd MMM yyyy", culture);
                    }
                    var timeFormatted = jadwal.waktu_berangkat.ToString("HH:mm");
                    txtDateTime.Text = $"{dateFormatted} - {timeFormatted}";
                }
                UpdatePaymentDetails();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error updating UI: {ex.Message}");
            }
        }

        private async void UpdatePaymentDetails()
        {
            // Method ini sudah benar, tidak perlu diubah
            if (_tiket == null) return;
            try
            {
                var txtDetailKendaraan = FindName("txtDetailKendaraan") as TextBlock;
                var txtHargaKendaraan = FindName("txtHargaKendaraan") as TextBlock;
                if (txtDetailKendaraan != null && txtHargaKendaraan != null)
                {
                    int jenisKendaraanId = GetJenisKendaraanIdFromEnum(_tiket.jenis_kendaraan_enum);
                    var grupKendaraanId = _tiket.Jadwal.grup_kendaraan_id;
                    var detailKendaraan = await DatabaseService.GetContext().DetailKendaraans
                        .FirstOrDefaultAsync(dk =>
                            dk.grup_kendaraan_id == grupKendaraanId &&
                            dk.jenis_kendaraan == jenisKendaraanId);

                    if (detailKendaraan != null)
                    {
                        string jenisKendaraanText = GetJenisKendaraanText(detailKendaraan.jenis_kendaraan);
                        txtDetailKendaraan.Text = jenisKendaraanText;
                        txtHargaKendaraan.Text = $"IDR {_tiket.total_harga:N0}";
                    }
                    else
                    {
                        var fallbackDetail = await DatabaseService.GetContext().DetailKendaraans
                            .FirstOrDefaultAsync(dk => dk.jenis_kendaraan == jenisKendaraanId);
                        if (fallbackDetail != null)
                        {
                            string jenisKendaraanText = GetJenisKendaraanText(fallbackDetail.jenis_kendaraan);
                            txtDetailKendaraan.Text = jenisKendaraanText;
                        }
                        else
                        {
                            txtDetailKendaraan.Text = _tiket.jenis_kendaraan_enum;
                        }
                        txtHargaKendaraan.Text = $"IDR {_tiket.total_harga:N0}";
                    }
                }
                var txtDetailPenumpang = FindName("txtDetailPenumpang") as TextBlock;
                if (txtDetailPenumpang != null)
                {
                    int jumlahPenumpang = _tiket.jumlah_penumpang;
                    txtDetailPenumpang.Text = $"Dewasa ({jumlahPenumpang}x)";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error updating payment details: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private string GetJenisKendaraanText(int jenisKendaraanId)
        {
            // Method ini sudah benar, tidak perlu diubah
            return jenisKendaraanId switch
            {
                0 => "Pejalan kaki tanpa kendaraan",
                1 => "Sepeda",
                2 => "Sepeda Motor (<500cc)",
                3 => "Sepeda Motor (>500cc) (Golongan III)",
                4 => "Mobil jeep, sedan, minibus",
                5 => "Mobil barang bak muatan",
                6 => "Mobil bus penumpang (5-7 meter)",
                7 => "Mobil barang (truk/tangki) ukuran sedang",
                8 => "Mobil bus penumpang (7-10 meter)",
                9 => "Mobil barang (truk/tangki) sedang",
                10 => "Mobil tronton, tangki, penarik + gandengan (10-12 meter)",
                11 => "Mobil tronton, tangki, alat berat (12-16 meter)",
                12 => "Mobil tronton, tangki, alat berat (>16 meter)",
                _ => "Kendaraan tidak diketahui"
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            _countdownTimer?.Stop();
            base.OnClosed(e);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void PaymentMethod_Unchecked(object sender, RoutedEventArgs e)
        {
            // Optional: Handle unchecked state if needed
        }

        private void UpdateBankInfo(string paymentMethod)
        {
            if (txtBankName == null || imgSelectedBank == null) return;

            bool isTransferBank = IsTransferBankMethod(); 

            switch (paymentMethod)
            {
                case "Transfer BCA":
                    txtBankName.Text = "Bank BCA";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/BCA.png", UriKind.Relative));
                    txtAccountNumber.Text = "385 0833 817";
                    break;
                case "Transfer Mandiri":
                    txtBankName.Text = "Bank Mandiri";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/Mandiri.png", UriKind.Relative));
                    txtAccountNumber.Text = "142 0099 123456";
                    break;
                case "Retail Indomaret":
                    txtBankName.Text = "Indomaret";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/Indomaret.png", UriKind.Relative));
                    txtAccountNumber.Text = "Kode: INDOM12345";
                    break;
                case "Retail Alfamart":
                    txtBankName.Text = "Alfamart";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri("/Views/Assets/Images/Alfamart.png", UriKind.Relative));
                    txtAccountNumber.Text = "Kode: ALFA67890";
                    break;
                case "QRIS":
                    txtBankName.Text = "QRIS";
                    txtAccountNumber.Text = "Silakan pindai kode QR";
                    break;
                case "Kartu Kredit":
                    txtBankName.Text = "Kartu Kredit/Debit";
                    txtAccountNumber.Text = "Silakan isi detail kartu";
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

        private decimal CalculateTotalPayment()
        {
            bool isTransferBank = IsTransferBankMethod();

            if (isTransferBank)
            {
                return hargaAsli + kodeUnik;
            }
            else
            {
                return hargaAsli;
            }
        }

        private bool IsTransferBankMethod()
        {
            return selectedPaymentMethod.StartsWith("Transfer");
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
                "6. Simpan bukti Transfer",
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
            decimal totalPembayaran = CalculateTotalPayment();
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

                if (IsTransferBankMethod()) // Memanggil helper yang sudah diperbarui
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

        private void BtnKembali_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Apakah Anda yakin membatalkan pembayaran?",
                "Konfirmasi Pembatalan",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _countdownTimer?.Stop();
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
            // Method ini sudah benar, tidak perlu diubah
            decimal totalPembayaran = CalculateTotalPayment();
            string totalString = ((int)totalPembayaran).ToString();
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
                if (txtInstructionTotalPembayaran != null)
                {
                    txtInstructionTotalPembayaran.Text = $"IDR {mainPart}.";
                    txtInstructionTotalPembayaranDigit.Text = lastThreeDigits;
                }
            }
            else
            {
                if (txtTotalPembayaran != null)
                {
                    txtTotalPembayaran.Text = $"IDR {totalString}";
                    txtTotalPembayaranDigit.Text = "";
                }
                if (txtDetailTotalPembayaran != null)
                {
                    txtDetailTotalPembayaran.Text = $"IDR {totalString}";
                    txtDetailTotalPembayaranDigit.Text = "";
                }
                if (txtInstructionTotalPembayaran != null)
                {
                    txtInstructionTotalPembayaran.Text = $"IDR {totalString}";
                    txtInstructionTotalPembayaranDigit.Text = "";
                }
            }
            System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Total pembayaran updated: {totalPembayaran} (Method: {selectedPaymentMethod}, IsBank: {IsTransferBankMethod()})");
        }

        private int GetJenisKendaraanIdFromEnum(string jenisKendaraanEnum)
        {
            // Method ini sudah benar, tidak perlu diubah
            if (string.IsNullOrEmpty(jenisKendaraanEnum))
            {
                System.Diagnostics.Debug.WriteLine("[PaymentWindow] Empty or null jenis_kendaraan_enum, defaulting to 0");
                return 0;
            }
            var normalized = jenisKendaraanEnum.ToLower().Trim();
            var result = normalized switch
            {
                "pejalan kaki" => 0,
                "sepeda" => 1,
                "sepeda motor (<500cc)" => 2,
                "sepeda motor (>500cc)" => 3,
                "mobil sedan/jeep/minibus" => 4,
                "mobil barang bak muatan" => 5,
                "bus penumpang (5-7m)" => 6,
                "truk/tangki (5-7m)" => 7,
                "bus penumpang (7-10m)" => 8,
                "truk/tangki (7-10m)" => 9,
                "tronton/gandengan (10-12m)" => 10,
                "alat berat (12-16m)" => 11,
                "alat berat (>16m)" => 12,
                "tidak diketahui" => 0,
                "pejalan kaki tanpa kendaraan" => 0,
                "mobil jeep, sedan, minibus" => 4,
                "mobil barang" => 5,
                "bus kecil" => 6,
                "truk kecil" => 7,
                "bus sedang" => 8,
                "truk sedang" => 9,
                "truk besar" => 10,
                "alat berat sedang" => 11,
                "alat berat besar" => 12,
                "" => 0,
                _ => 0
            };
            return result;
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

        public static void Open()
        {
            var paymentWindow = new PaymentWindow();
            paymentWindow.Show();
        }
    }
}