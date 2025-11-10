using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Services;
using TiketLaut.Views;
using TiketLaut.Views.Components;

using QRCoder; 
using System.IO; 
using System.Windows.Media.Imaging; 
using System.Drawing; 
using System.Drawing.Imaging; 
using System.Text.RegularExpressions; 
using System.Windows.Input; 

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

        // --- Variabel Regex (hanya untuk validasi akhir, BUKAN input filter) ---
        private readonly Regex _regexExpiry = new Regex(@"^(0[1-9]|1[0-2])\/\d{2}$");


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

        #region Timer, LoadData, dan Navigasi Dasar

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

                // --- PERBAIKAN AMBIGUITAS ---
                if (deadlineCard != null) { deadlineCard.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)); }
                if (txtDeadlineTime != null)
                {
                    txtDeadlineTime.Text = "WAKTU PEMBAYARAN TELAH HABIS";
                    txtDeadlineTime.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
                }
                // --- AKHIR PERBAIKAN ---


                if (btnMainAction != null)
                {
                    btnMainAction.IsEnabled = false;
                    txtMainActionButton.Text = "Waktu Pembayaran Habis";
                }

                await MarkPaymentAsFailedDueToTimeout();
                CustomDialog.ShowWarning("Waktu Habis", "Waktu pembayaran telah berakhir. Pembayaran Anda telah dibatalkan secara otomatis.\nSilakan lakukan booking ulang.");

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

        private async void LoadTiketData()
        {
            try
            {
                if (SessionManager.CurrentUser == null)
                {
                    CustomDialog.ShowError("Error", "Session user tidak ditemukan!");
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

                    UpdateUIWithTicketData(); // Method yang hilang
                    UpdateTotalPembayaran();
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Loaded tiket: {_tiket.kode_tiket}, Total: {_tiket.total_harga}");
                }
                else
                {
                    CustomDialog.ShowInfo(
                        "Info",
                        "Tidak ditemukan tiket yang menunggu pembayaran.\nSilakan lakukan booking terlebih dahulu.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error loading tiket: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
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

        private void ApplyResponsiveLayout()
        {
            double windowWidth = this.ActualWidth;
            if (windowWidth < 1280) MainContentGrid.Margin = new Thickness(20, 15, 20, 20);
            else if (windowWidth < 1600) MainContentGrid.Margin = new Thickness(30, 20, 30, 25);
            else MainContentGrid.Margin = new Thickness(40, 20, 40, 30);
        }

        #endregion

        #region Generator QR Code

        private void GenerateQrCode()
        {
            try
            {
                decimal totalPembayaran = CalculateTotalPayment();
                string payload = totalPembayaran.ToString("F0");

                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                Bitmap qrCodeImage = qrCode.GetGraphic(20);

                imgQrCode.Source = BitmapToImageSource(qrCodeImage);

                if (txtPindai != null) txtPindai.Text = "Pindai Kode QR di Bawah Ini";
                if (txtGunakan != null) txtGunakan.Text = "Gunakan aplikasi e-wallet (GoPay, OVO, Dana, ShopeePay) atau mobile banking Anda untuk memindai.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error generating QR code: {ex.Message}");
                CustomDialog.ShowError("Error QR Code", "Gagal membuat gambar QR code. Silakan coba metode lain.");
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        #endregion

        #region Logika Pembayaran

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
                    if (rbTransferBank != null) rbTransferBank.IsChecked = true;
                }
                else if (selectedPaymentMethod.StartsWith("Retail"))
                {
                    if (rbGeraiRetail != null) rbGeraiRetail.IsChecked = true;
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

        // --- METHOD YANG HILANG DITAMBAHKAN KEMBALI ---
        private void PaymentMethod_Unchecked(object sender, RoutedEventArgs e)
        {
            // Optional: Handle unchecked state if needed
        }
        // --- AKHIR TAMBAHAN ---

        private async Task HandlePaymentMethodSelection()
        {
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
                CustomDialog.ShowWarning("Error", $"Terjadi kesalahan saat memproses metode pembayaran: {ex.Message}");
            }
        }

        private void RestorePaymentMethodSelection(string paymentMethod)
        {
            try
            {
                switch (paymentMethod)
                {
                    case "Transfer BCA":
                        if (FindName("rbBCA") is RadioButton rbBCA) rbBCA.IsChecked = true;
                        break;
                    case "Transfer Mandiri":
                        if (FindName("rbMandiri") is RadioButton rbMandiri) rbMandiri.IsChecked = true;
                        break;
                    case "Retail Indomaret":
                        if (FindName("rbIndomaret") is RadioButton rbIndomaret) rbIndomaret.IsChecked = true;
                        break;
                    case "Retail Alfamart":
                        if (FindName("rbAlfamart") is RadioButton rbAlfamart) rbAlfamart.IsChecked = true;
                        break;
                    case "QRIS":
                        if (FindName("rbQRIS") is RadioButton rbQRIS) rbQRIS.IsChecked = true;
                        break;
                    case "Kartu Kredit":
                        if (FindName("rbKartuKredit") is RadioButton rbKredit) rbKredit.IsChecked = true;
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
            try
            {
                System.Diagnostics.Debug.WriteLine("[PaymentWindow] KonfirmasiPembayaranAsync started");
                if (_tiket == null || _currentPembayaran == null)
                {
                    CustomDialog.ShowError("Error", "Data pembayaran tidak ditemukan!\nSilakan pilih metode pembayaran terlebih dahulu.");
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
                    CustomDialog.ShowSuccess(
                        "Pembayaran Berhasil",
                        $"✅ Pembayaran berhasil dikonfirmasi!\n\nKode Tiket: {_tiket.kode_tiket}\nMetode: {selectedPaymentMethod}\nJumlah: Rp {_currentPembayaran.jumlah_bayar:N0}\nStatus: {_currentPembayaran.status_bayar}\n\nPembayaran Anda sedang diverifikasi oleh admin.\nAnda dapat mengecek status di menu 'Cek Booking'.");

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
                    CustomDialog.ShowError("Error", "Gagal memperbarui status pembayaran. Silakan coba lagi.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] EXCEPTION in KonfirmasiPembayaranAsync:");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Message: {ex.Message}");
                CustomDialog.ShowError("Error", $"❌ Terjadi kesalahan saat memproses pembayaran:\n\n{ex.Message}");
            }
            finally
            {
                btnMainAction.IsEnabled = true;
                txtMainActionButton.Text = "Konfirmasi Pembayaran";
            }
        }

        private async Task MarkPaymentAsFailedDueToTimeout()
        {
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

        #endregion

        #region Update UI Sesuai Data (Termasuk Helper yang Hilang)

        // --- METHOD DIPERBARUI ---
        // Method ini sekarang menghandle Transfer, Retail, QRIS, dan Kartu Kredit
        private void UpdateBankInfo(string paymentMethod)
        {
            // 1. Pastikan semua panel XAML baru bisa ditemukan
            if (txtBankName == null || imgSelectedBank == null || imgBankBorder == null ||
                pnlTransferInfo == null || pnlQrCode == null || pnlCreditCard == null ||
                pnlWarningMessage == null || pnlKeteranganPembayaran == null || pnlAccountNumber == null)
            {
                System.Diagnostics.Debug.WriteLine("[PaymentWindow] UpdateBankInfo failed: one or more XAML controls not found.");
                return;
            }

            bool isTransferBank = IsTransferBankMethod();

            // --- 2. RESET SEMUA PANEL INSTRUKSI ---
            pnlTransferInfo.Visibility = Visibility.Collapsed;
            pnlQrCode.Visibility = Visibility.Collapsed;
            pnlCreditCard.Visibility = Visibility.Collapsed;

            // Reset panel spesifik bank/retail
            pnlWarningMessage.Visibility = Visibility.Collapsed;
            pnlKeteranganPembayaran.Visibility = Visibility.Collapsed;
            imgBankBorder.Visibility = Visibility.Visible;
            pnlAccountNumber.Visibility = Visibility.Visible;

            // --- 3. TAMPILKAN PANEL YANG SESUAI ---
            switch (paymentMethod)
            {
                case "Transfer BCA":
                case "Transfer Mandiri":
                    pnlTransferInfo.Visibility = Visibility.Visible;    // Tampilkan panel bank
                    pnlWarningMessage.Visibility = Visibility.Visible;  // Tampilkan warning 3 digit
                    pnlKeteranganPembayaran.Visibility = Visibility.Visible; // Tampilkan kode unik di sidebar

                    txtBankName.Text = (paymentMethod == "Transfer BCA") ? "Bank BCA" : "Bank Mandiri";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri((paymentMethod == "Transfer BCA") ? "/Views/Assets/Images/BCA.png" : "/Views/Assets/Images/Mandiri.png", UriKind.Relative));
                    txtAccountNumber.Text = (paymentMethod == "Transfer BCA") ? "385 0833 817" : "142 0099 123456";
                    break;

                case "Retail Indomaret":
                case "Retail Alfamart":
                    pnlTransferInfo.Visibility = Visibility.Visible; // Tampilkan panel retail
                                                                     // (Warning 3 digit tetap collapsed)

                    txtBankName.Text = (paymentMethod == "Retail Indomaret") ? "Indomaret" : "Alfamart";
                    imgSelectedBank.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri((paymentMethod == "Retail Indomaret") ? "/Views/Assets/Images/Indomaret.png" : "/Views/Assets/Images/Alfamart.png", UriKind.Relative));
                    txtAccountNumber.Text = (paymentMethod == "Retail Indomaret") ? "Kode: INDOM12345" : "Kode: ALFA67890";
                    break;

                case "QRIS":
                    pnlQrCode.Visibility = Visibility.Visible; // Tampilkan panel QR
                    GenerateQrCode(); // Panggil generator
                    break;

                case "Kartu Kredit":
                    pnlCreditCard.Visibility = Visibility.Visible; // Tampilkan panel Kartu Kredit
                    break;

                default:
                    // Tampilkan panel bank/retail tapi dengan info default
                    pnlTransferInfo.Visibility = Visibility.Visible;
                    imgBankBorder.Visibility = Visibility.Collapsed;
                    txtBankName.Text = "Metode Pembayaran";
                    txtAccountNumber.Text = "-";
                    break;
            }

            // Handle tampilan kode unik di sidebar
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

        // --- METHOD BARU (DIKEMBALIKAN) ---
        private void UpdateUIWithTicketData()
        {
            if (_tiket?.Jadwal == null) return;

            try
            {
                var jadwal = _tiket.Jadwal;
                if (txtOrderId != null) { txtOrderId.Text = $"Order ID: {_tiket.kode_tiket}"; }
                if (txtFerryType != null) { txtFerryType.Text = jadwal.kelas_layanan ?? "Reguler"; }
                if (txtDeparturePort != null && jadwal.pelabuhan_asal != null) { txtDeparturePort.Text = jadwal.pelabuhan_asal.nama_pelabuhan; }
                if (txtArrivalPort != null && jadwal.pelabuhan_tujuan != null) { txtArrivalPort.Text = jadwal.pelabuhan_tujuan.nama_pelabuhan; }

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

        // --- METHOD BARU (DIKEMBALIKAN) ---
        private async void UpdatePaymentDetails()
        {
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
            }
        }

        // --- METHOD BARU (DIKEMBALIKAN) ---
        private string GetJenisKendaraanText(int jenisKendaraanId)
        {
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

        // --- METHOD BARU (DIKEMBALIKAN) ---
        private int GetJenisKendaraanIdFromEnum(string jenisKendaraanEnum)
        {
            if (string.IsNullOrEmpty(jenisKendaraanEnum)) { return 0; }
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

        #endregion

        #region Logika Tombol (Click Handlers)

        private async void BtnMainAction_Click(object sender, RoutedEventArgs e)
        {
            if (!isPaymentMethodSelected)
            {
                CustomDialog.ShowInfo("Informasi", "Silakan pilih metode pembayaran terlebih dahulu!");
                return;
            }

            if (PaymentMethodsCard.Visibility == Visibility.Visible)
            {
                // Pindah dari "Pilih Metode" ke "Instruksi"
                PaymentMethodsCard.Visibility = Visibility.Collapsed;
                PaymentInstructionsCard.Visibility = Visibility.Visible;
                txtMainActionButton.Text = "Konfirmasi Pembayaran";

                // Panggil UpdateBankInfo LAGI untuk memastikan panel instruksi yang benar (QR/CC/Bank) ditampilkan
                UpdateBankInfo(selectedPaymentMethod);
            }
            else
            {
                // Ini adalah klik "Konfirmasi Pembayaran"

                // --- VALIDASI KARTU KREDIT (BARU) ---
                if (selectedPaymentMethod == "Kartu Kredit")
                {
                    // Gunakan helper IsPlaceholderText
                    if (string.IsNullOrWhiteSpace(txtCardNumber.Text) || IsPlaceholderText(txtCardNumber))
                    {
                        CustomDialog.ShowWarning("Validasi", "Nomor kartu tidak boleh kosong.");
                        txtCardNumber.Focus();
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(txtExpiryDate.Text) || IsPlaceholderText(txtExpiryDate))
                    {
                        CustomDialog.ShowWarning("Validasi", "Tanggal kedaluwarsa tidak boleh kosong.");
                        txtExpiryDate.Focus();
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(txtCvv.Text) || IsPlaceholderText(txtCvv))
                    {
                        CustomDialog.ShowWarning("Validasi", "CVV tidak boleh kosong.");
                        txtCvv.Focus();
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(txtNameOnCard.Text) || IsPlaceholderText(txtNameOnCard))
                    {
                        CustomDialog.ShowWarning("Validasi", "Nama pemilik kartu tidak boleh kosong.");
                        txtNameOnCard.Focus();
                        return;
                    }

                    // --- TAMBAHAN VALIDASI FORMAT ---
                    if (txtCardNumber.Text.Length < 15 || txtCardNumber.Text.Length > 16)
                    {
                        CustomDialog.ShowWarning("Validasi", "Nomor kartu harus 15 atau 16 digit.");
                        txtCardNumber.Focus();
                        return;
                    }
                    if (!_regexExpiry.IsMatch(input: txtExpiryDate.Text)) // Gunakan argumen bernama
                    {
                        CustomDialog.ShowWarning("Validasi", "Format tanggal kedaluwarsa harus MM/YY (contoh: 09/27).");
                        txtExpiryDate.Focus();
                        return;
                    }
                    if (txtCvv.Text.Length < 3 || txtCvv.Text.Length > 4)
                    {
                        CustomDialog.ShowWarning("Validasi", "CVV harus 3 atau 4 digit.");
                        txtCvv.Focus();
                        return;
                    }
                    // --- AKHIR VALIDASI FORMAT ---
                }
                // --- AKHIR VALIDASI ---

                // Lanjutkan ke konfirmasi
                await KonfirmasiPembayaranAsync();
            }
        }

        private void BtnGantiMetode_Click(object sender, RoutedEventArgs e)
        {
            PaymentInstructionsCard.Visibility = Visibility.Collapsed;
            PaymentMethodsCard.Visibility = Visibility.Visible;
            txtMainActionButton.Text = "Konfirmasi Pembayaran";

            // --- TAMBAHAN: Reset UI Instruksi ---
            if (pnlQrCode != null) pnlQrCode.Visibility = Visibility.Collapsed;
            if (pnlCreditCard != null) pnlCreditCard.Visibility = Visibility.Collapsed;
            if (pnlTransferInfo != null) pnlTransferInfo.Visibility = Visibility.Visible;
            if (pnlWarningMessage != null) pnlWarningMessage.Visibility = Visibility.Visible;
        }

        private void BtnCaraMembayar_Click(object sender, RoutedEventArgs e)
        {
            string instruksi = "";
            switch (selectedPaymentMethod)
            {
                case "Transfer BCA":
                case "Transfer Mandiri":
                    instruksi = "1. Buka aplikasi mobile banking atau ATM\n" +
                                "2. Pilih menu Transfer\n" +
                                "3. Masukkan nomor rekening tujuan\n" +
                                "4. Masukkan nominal sesuai yang tertera (termasuk 3 digit unik)\n" +
                                "5. Konfirmasi transaksi\n" +
                                "6. Simpan bukti transfer";
                    break;
                case "QRIS":
                    instruksi = "1. Buka aplikasi e-wallet atau mobile banking Anda.\n" +
                                "2. Pilih menu 'Bayar' atau 'Scan QR'.\n" +
                                "3. Pindai kode QR yang tampil di layar.\n" +
                                "4. Pastikan jumlah pembayaran sudah sesuai.\n" +
                                "5. Masukkan PIN Anda untuk konfirmasi.";
                    break;
                case "Kartu Kredit":
                    instruksi = "1. Pastikan semua data kartu sudah terisi dengan benar.\n" +
                                "2. Klik 'Konfirmasi Pembayaran'.\n" +
                                "3. Anda mungkin akan diarahkan ke halaman 3D Secure bank Anda.\n" +
                                "4. Masukkan kode OTP yang dikirimkan ke ponsel Anda.\n" +
                                "5. Pembayaran selesai.";
                    break;
                case "Retail Indomaret":
                case "Retail Alfamart":
                    instruksi = "1. Pergi ke gerai retail terdekat.\n" +
                                "2. Tunjukkan kode pembayaran (contoh: 'INDOM12345') kepada kasir.\n" +
                                "3. Lakukan pembayaran sesuai nominal.\n" +
                                "4. Simpan struk sebagai bukti pembayaran.";
                    break;
                default:
                    instruksi = "Silakan ikuti instruksi pembayaran untuk metode yang Anda pilih.";
                    break;
            }

            CustomDialog.ShowInfo(
                $"Cara Membayar - {selectedPaymentMethod}",
                instruksi);
        }

        private void BtnCopyAccount_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtAccountNumber.Text))
            {
                Clipboard.SetText(txtAccountNumber.Text);
                CustomDialog.ShowSuccess("Berhasil", "Nomor rekening telah disalin!");
            }
        }

        private void BtnCopyAmount_Click(object sender, RoutedEventArgs e)
        {
            decimal totalPembayaran = CalculateTotalPayment();
            Clipboard.SetText(totalPembayaran.ToString("F0")); // Salin sebagai angka bulat
            CustomDialog.ShowSuccess("Berhasil", "Jumlah pembayaran telah disalin!");
        }

        private void BtnToggleDetailPembayaran_Click(object sender, RoutedEventArgs e)
        {
            if (pnlDetailPembayaran.Visibility == Visibility.Collapsed)
            {
                pnlDetailPembayaran.Visibility = Visibility.Visible;
                AnimateArrow(180);
                if (IsTransferBankMethod())
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
            var result = CustomDialog.ShowQuestion(
                "Konfirmasi Pembatalan",
                "Apakah Anda yakin membatalkan pembayaran?",
                CustomDialog.DialogButtons.YesNo);

            if (result == true)
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

        #endregion

        #region Helper UI (Animasi, Harga, Floating Label)

        // --- TAMBAHAN BARU: Floating Label Logic (UNTUK ERROR CS1061) ---

        private void FloatingTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null)
                {
                    var labelBorder = label.Parent as Border;
                    if (labelBorder != null)
                    {
                        labelBorder.VerticalAlignment = VerticalAlignment.Top;
                        labelBorder.Margin = new Thickness(12, 8, 0, 0);
                    }
                    label.FontSize = 11;
                    // --- PERBAIKAN AMBIGUITAS ---
                    label.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00658D"));
                    textBox.Padding = new Thickness(16, 16, 16, 8);
                }
            }
        }

        private void FloatingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string labelName)
            {
                var label = this.FindName(labelName) as TextBlock;
                if (label != null && string.IsNullOrWhiteSpace(textBox.Text))
                {
                    var labelBorder = label.Parent as Border;
                    if (labelBorder != null)
                    {
                        labelBorder.VerticalAlignment = VerticalAlignment.Center;
                        labelBorder.Margin = new Thickness(12, 0, 0, 0);
                    }
                    label.FontSize = 14;
                    // --- PERBAIKAN AMBIGUITAS ---
                    label.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8"));
                    textBox.Padding = new Thickness(16, 0, 16, 0);
                }
            }
        }

        private void FloatingTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // HANYA JALANKAN LOGIC FLOATING LABEL
            // Logika spesifik 'ExpiryDate_TextChanged' akan ditangani oleh methodnya sendiri
            if (sender is TextBox textBox && textBox.Tag is string labelName)
            {
                // Hindari loop tak terbatas dengan ExpiryDate_TextChanged
                if (textBox.Name == "txtExpiryDate") return;

                var label = this.FindName(labelName) as TextBlock;
                if (label != null)
                {
                    var labelBorder = label.Parent as Border;
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        if (labelBorder != null)
                        {
                            labelBorder.VerticalAlignment = VerticalAlignment.Top;
                            labelBorder.Margin = new Thickness(12, 8, 0, 0);
                        }
                        label.FontSize = 11;
                        // --- PERBAIKAN AMBIGUITAS ---
                        label.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00658D"));
                        textBox.Padding = new Thickness(16, 16, 16, 8);
                    }
                    else if (!textBox.IsFocused)
                    {
                        if (labelBorder != null)
                        {
                            labelBorder.VerticalAlignment = VerticalAlignment.Center;
                            labelBorder.Margin = new Thickness(12, 0, 0, 0);
                        }
                        label.FontSize = 14;
                        // --- PERBAIKAN AMBIGUITAS ---
                        label.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8"));
                        textBox.Padding = new Thickness(16, 0, 16, 0);
                    }
                }
            }
        }

        // --- TAMBAHAN BARU: Placeholder Logic (untuk validasi) ---
        private bool IsPlaceholderText(TextBox textBox)
        {
            if (textBox == null || textBox.Tag == null) return false;

            string labelName = textBox.Tag.ToString();
            var label = this.FindName(labelName) as TextBlock;
            if (label == null)
            {
                return false;
            }
            // Periksa jika label text adalah null sebelum membandingkan
            return label.Text != null && textBox.Text == label.Text;
        }

        // --- TAMBAHAN BARU: Input Filtering Kartu Kredit ---

        /// <summary>
        /// (FIX BARU) Hanya mengizinkan input angka (untuk Nomor Kartu dan CVV).
        /// </summary>
        private void NumbersOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Kita cek satu per satu karakternya
            foreach (char c in e.Text)
            {
                // Jika karakter BUKAN angka (digit)
                if (!char.IsDigit(c))
                {
                    // Batalkan input
                    e.Handled = true;
                    return;
                }
            }
        }

        /// <summary>
        /// (FIX BARU) Hanya mengizinkan input angka dan / (untuk Tanggal Kedaluwarsa).
        /// </summary>
        private void ExpiryDate_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Kita cek satu per satu karakternya
            foreach (char c in e.Text)
            {
                // Jika karakter BUKAN angka (digit) DAN BUKAN '/'
                if (!char.IsDigit(c) && c != '/')
                {
                    // Batalkan input
                    e.Handled = true;
                    return;
                }
            }
        }

        /// <summary>
        /// (FIXED) Menambahkan '/' secara otomatis setelah 2 digit bulan (MM) diketik.
        /// </summary>
        private void ExpiryDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Panggil logic floating label dulu
            FloatingTextBox_TextChanged(sender, e);

            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Hapus semua TextChanged handler sementara untuk menghindari infinite loop
            textBox.TextChanged -= ExpiryDate_TextChanged;
            textBox.TextChanged -= FloatingTextBox_TextChanged;

            string text = textBox.Text;

            // --- PERBAIKAN CS1503 (ARGUMEN BERNAMA) ---
            if (text.Length == 2 && !text.Contains(value: '/'))
            {
                string mm = text.Substring(startIndex: 0, length: 2);

                if (int.TryParse(mm, out int month))
                {
                    if (month > 12) mm = "12";
                    else if (month == 0) mm = "01";
                }

                textBox.Text = mm + "/";
                textBox.CaretIndex = textBox.Text.Length;
            }
            else if (text.Length == 2 && e.Changes.Any(c => c.RemovedLength > 0))
            {
                textBox.Text = text.Substring(startIndex: 0, length: 1);
                textBox.CaretIndex = textBox.Text.Length;
            }
            else if (text.Length > 5)
            {
                textBox.Text = text.Substring(startIndex: 0, length: 5);
                textBox.CaretIndex = 5;
            }
            // --- AKHIR PERBAIKAN CS1503 ---

            // Tambahkan kembali handler
            textBox.TextChanged += ExpiryDate_TextChanged;
            textBox.TextChanged += FloatingTextBox_TextChanged;
        }
        // --- AKHIR TAMBAHAN ---


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
            decimal totalPembayaran = CalculateTotalPayment();
            string mainPart;
            string lastThreeDigits = "";
            var culture = new System.Globalization.CultureInfo("id-ID");

            if (IsTransferBankMethod())
            {
                string rawTotalString = ((int)totalPembayaran).ToString(); // "130639"
                if (rawTotalString.Length > 3)
                {
                    string mainPartRaw = rawTotalString.Substring(0, rawTotalString.Length - 3); // "130"
                    lastThreeDigits = rawTotalString.Substring(rawTotalString.Length - 3); // "639"

                    mainPart = decimal.Parse(mainPartRaw).ToString("N0", culture) + "."; // "130."
                }
                else
                {
                    mainPart = "0.";
                    lastThreeDigits = rawTotalString.PadLeft(3, '0'); // "639"
                }
            }
            else
            {
                mainPart = totalPembayaran.ToString("N0", culture); // "20.000"
                lastThreeDigits = "";
            }

            if (txtTotalPembayaran != null)
            {
                txtTotalPembayaran.Text = $"IDR {mainPart}";
                txtTotalPembayaranDigit.Text = lastThreeDigits;
            }
            if (txtDetailTotalPembayaran != null)
            {
                txtDetailTotalPembayaran.Text = $"IDR {mainPart}";
                txtDetailTotalPembayaranDigit.Text = lastThreeDigits;
            }
            if (txtInstructionTotalPembayaran != null)
            {
                txtInstructionTotalPembayaran.Text = $"IDR {mainPart}";
                txtInstructionTotalPembayaranDigit.Text = lastThreeDigits;
            }

            System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Total pembayaran updated: {totalPembayaran} (Method: {selectedPaymentMethod}, IsBank: {IsTransferBankMethod()})");
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
                // QRIS, Retail, Kartu Kredit, dll. tidak pakai kode unik
                return hargaAsli;
            }
        }

        private bool IsTransferBankMethod()
        {
            return selectedPaymentMethod.StartsWith("Transfer");
        }

        public static void Open()
        {
            var paymentWindow = new PaymentWindow();
            paymentWindow.Show();
        }

        #endregion
    }
}