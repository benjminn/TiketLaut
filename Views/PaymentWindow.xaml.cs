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

        // ✅ TAMBAHAN: Track payment record
        private Pembayaran? _currentPembayaran = null;

        // ✅ TAMBAHAN: Timer untuk countdown
        private DispatcherTimer? _countdownTimer;
        private DateTime _paymentDeadline;

        public PaymentWindow()
        {
            InitializeComponent();
            _pembayaranService = new PembayaranService();
            _bookingService = new BookingService();

            ApplyResponsiveLayout();
            GenerateKodeUnik();

            // ✅ TAMBAHAN: Initialize countdown timer
            InitializeCountdownTimer();

            LoadTiketData();
        }

        // ✅ TAMBAHAN: Initialize countdown timer 24 hours
        private void InitializeCountdownTimer()
        {
            // Set deadline 24 jam dari sekarang
            _paymentDeadline = DateTime.Now.AddHours(24);

            // Create timer yang update setiap detik
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();

            // Update tampilan pertama kali
            UpdateCountdownDisplay();
        }

        // ✅ TAMBAHAN: Update countdown display
        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            UpdateCountdownDisplay();
        }

        private async void UpdateCountdownDisplay()
        {
            var timeRemaining = _paymentDeadline - DateTime.Now;

            if (timeRemaining.TotalSeconds <= 0)
            {
                // Waktu habis
                _countdownTimer?.Stop();

                // Update UI untuk menunjukkan waktu habis
                var deadlineCard = FindName("DeadlineCard") as Border;
                if (deadlineCard != null)
                {
                    deadlineCard.BorderBrush = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(220, 38, 38)); // Red color
                }

                // Update text menjadi "EXPIRED"
                if (txtDeadlineTime != null)
                {
                    txtDeadlineTime.Text = "WAKTU PEMBAYARAN TELAH HABIS";
                    txtDeadlineTime.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(220, 38, 38));
                }

                // Disable payment button
                if (btnMainAction != null)
                {
                    btnMainAction.IsEnabled = false;
                    txtMainActionButton.Text = "Waktu Pembayaran Habis";
                }

                // ✅ TAMBAHAN: Actually call the method to mark payment as failed
                await MarkPaymentAsFailedDueToTimeout();

                MessageBox.Show(
                    "Waktu pembayaran telah berakhir. Pembayaran Anda telah dibatalkan secara otomatis.\nSilakan lakukan booking ulang.",
                    "Waktu Habis",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Navigate back to schedule window
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

            // Format countdown: "DD hari HH:MM:SS"
            string countdownText;
            if (timeRemaining.Days > 0)
            {
                countdownText = $"{timeRemaining.Days} hari {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
            }
            else
            {
                countdownText = $"{timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
            }

            // Update deadline text dengan format yang lebih informatif
            if (txtDeadlineTime != null)
            {
                var culture = new System.Globalization.CultureInfo("id-ID");
                var deadlineFormatted = _paymentDeadline.ToString("dd MMMM yyyy, HH:mm", culture);
                txtDeadlineTime.Text = $"{deadlineFormatted} WIB ({countdownText})";
            }
        }

        // ✅ UPDATED: Payment method selection with automatic database operations
        private async void PaymentMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag != null)
            {
                string newPaymentMethod = rb.Tag.ToString() ?? "";

                // Don't process if same method is selected
                if (selectedPaymentMethod == newPaymentMethod) return;

                selectedPaymentMethod = newPaymentMethod;
                isPaymentMethodSelected = true;

                if (txtMainActionButton != null)
                    txtMainActionButton.Text = "Konfirmasi Pembayaran";

                // Update UI categories
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
                UpdateTotalPembayaran();

                // ✅ TAMBAHAN: Automatic database operations
                await HandlePaymentMethodSelection();
            }
        }

        // ✅ TAMBAHAN: Handle payment method selection with database operations
        private async Task HandlePaymentMethodSelection()
        {
            try
            {
                if (_tiket == null) return;

                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Payment method selected: {selectedPaymentMethod}");

                // Calculate payment amount based on method (with unique code for bank transfers)
                decimal jumlahBayar = CalculateTotalPayment();

                if (_currentPembayaran == null)
                {
                    // ✅ CREATE: First time selecting payment method - create new payment record
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Creating new payment record...");

                    _currentPembayaran = await _pembayaranService.CreatePembayaranAsync(
                        tiketId: _tiket.tiket_id,
                        metodePembayaran: selectedPaymentMethod,
                        jumlahBayar: jumlahBayar
                    );

                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] ✅ Created payment ID: {_currentPembayaran.pembayaran_id}");
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Status: {_currentPembayaran.status_bayar}");
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Amount: {_currentPembayaran.jumlah_bayar}");
                }
                else
                {
                    // ✅ UPDATE: Payment method changed - update existing payment record
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Updating existing payment record...");
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Old method: {_currentPembayaran.metode_pembayaran}");
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] New method: {selectedPaymentMethod}");
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Old amount: {_currentPembayaran.jumlah_bayar}");
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] New amount: {jumlahBayar}");

                    bool updated = await _pembayaranService.UpdatePembayaranMethodAsync(
                        pembayaranId: _currentPembayaran.pembayaran_id,
                        newMethodePembayaran: selectedPaymentMethod,
                        newJumlahBayar: jumlahBayar
                    );

                    if (updated)
                    {
                        // Update local object
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

        // ✅ UPDATED: Load existing payment when window opens
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

                // Load ticket data
                var tikets = await DatabaseService.GetContext().Tikets
                    .Include(t => t.Jadwal)
                        .ThenInclude(j => j.pelabuhan_asal)
                    .Include(t => t.Jadwal)
                        .ThenInclude(j => j.pelabuhan_tujuan)
                    .Include(t => t.Jadwal)
                        .ThenInclude(j => j.kapal)
                    .Include(t => t.Jadwal)
                        .ThenInclude(j => j.GrupKendaraan)
                            .ThenInclude(gk => gk != null ? gk.DetailKendaraans : null)
                    .Include(t => t.RincianPenumpangs)
                        .ThenInclude(rp => rp.penumpang)
                    .Where(t => t.pengguna_id == SessionManager.CurrentUser.pengguna_id &&
                                t.status_tiket == "Menunggu Pembayaran")
                    .OrderByDescending(t => t.tanggal_pemesanan)
                    .ToListAsync();

                if (tikets.Any())
                {
                    _tiket = tikets.First();
                    hargaAsli = _tiket.total_harga;

                    // ✅ TAMBAHAN: Check if payment record already exists
                    _currentPembayaran = await _pembayaranService.GetPembayaranByTiketIdAsync(_tiket.tiket_id);

                    if (_currentPembayaran != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Found existing payment:");
                        System.Diagnostics.Debug.WriteLine($"  - Payment ID: {_currentPembayaran.pembayaran_id}");
                        System.Diagnostics.Debug.WriteLine($"  - Method: {_currentPembayaran.metode_pembayaran}");
                        System.Diagnostics.Debug.WriteLine($"  - Amount: {_currentPembayaran.jumlah_bayar}");
                        System.Diagnostics.Debug.WriteLine($"  - Status: {_currentPembayaran.status_bayar}");

                        // ✅ Restore UI state if payment exists
                        if (!string.IsNullOrEmpty(_currentPembayaran.metode_pembayaran))
                        {
                            selectedPaymentMethod = _currentPembayaran.metode_pembayaran;
                            isPaymentMethodSelected = true;

                            // Set the appropriate radio button
                            RestorePaymentMethodSelection(_currentPembayaran.metode_pembayaran);
                            UpdateBankInfo(selectedPaymentMethod);
                        }
                    }

                    // Continue with existing debug and UI update code...
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] === LOADED TICKET DEBUG ===");
                    System.Diagnostics.Debug.WriteLine($"Kode Tiket: {_tiket.kode_tiket}");
                    System.Diagnostics.Debug.WriteLine($"Total Harga (hargaAsli): {hargaAsli}");
                    System.Diagnostics.Debug.WriteLine($"Jenis Kendaraan Enum: '{_tiket.jenis_kendaraan_enum}'");
                    System.Diagnostics.Debug.WriteLine($"Jumlah Penumpang: {_tiket.jumlah_penumpang}");
                    System.Diagnostics.Debug.WriteLine($"Jadwal ID: {_tiket.jadwal_id}");
                    System.Diagnostics.Debug.WriteLine($"Status Tiket: {_tiket.status_tiket}");

                    // Debug detail kendaraan
                    var grupKendaraanId = _tiket.Jadwal.grup_kendaraan_id;
                    var allDetailKendaraan = await DatabaseService.GetContext().DetailKendaraans
                        .Where(dk => dk.grup_kendaraan_id == grupKendaraanId)
                        .ToListAsync();

                    System.Diagnostics.Debug.WriteLine($"Available DetailKendaraan for grup_kendaraan_id {grupKendaraanId}:");
                    foreach (var detail in allDetailKendaraan)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Jenis: {detail.jenis_kendaraan}, Harga: {detail.harga_kendaraan}, Desc: {detail.deskripsi}");
                    }

                    UpdateUIWithTicketData();
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
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // ✅ TAMBAHAN: Restore payment method selection in UI
        private void RestorePaymentMethodSelection(string paymentMethod)
        {
            try
            {
                switch (paymentMethod)
                {
                    case "BCA":
                        if (FindName("rbBCA") is RadioButton rbBCA) rbBCA.IsChecked = true;
                        break;
                    case "Mandiri":
                        if (FindName("rbMandiri") is RadioButton rbMandiri) rbMandiri.IsChecked = true;
                        break;
                    case "Indomaret":
                        if (FindName("rbIndomaret") is RadioButton rbIndomaret) rbIndomaret.IsChecked = true;
                        break;
                    case "Alfamart":
                        if (FindName("rbAlfamart") is RadioButton rbAlfamart) rbAlfamart.IsChecked = true;
                        break;
                        // Add other payment methods as needed
                }

                if (txtMainActionButton != null)
                    txtMainActionButton.Text = "Konfirmasi Pembayaran";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error restoring payment method: {ex.Message}");
            }
        }

        // ✅ UPDATED: Confirmation now only updates status to "Menunggu Validasi"
        private async Task KonfirmasiPembayaranAsync()
        {
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

                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Updating payment status to 'Menunggu Validasi'");
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Payment ID: {_currentPembayaran.pembayaran_id}");

                // ✅ Only update status to "Menunggu Validasi" (payment record already exists)
                bool updated = await _pembayaranService.UpdateStatusPembayaranAsync(
                    _currentPembayaran.pembayaran_id,
                    "Menunggu Validasi"
                );

                if (updated)
                {
                    _currentPembayaran.status_bayar = "Menunggu Validasi";

                    // Stop countdown timer setelah payment berhasil
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

        // ✅ UPDATED: Update timeout method to work with existing payment
        private async Task MarkPaymentAsFailedDueToTimeout()
        {
            try
            {
                if (_tiket == null) return;

                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Marking payment as failed due to timeout for tiket: {_tiket.kode_tiket}");

                if (_currentPembayaran != null)
                {
                    // Update existing payment to failed
                    await _pembayaranService.UpdateStatusPembayaranAsync(_currentPembayaran.pembayaran_id, "Gagal");
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Updated existing payment {_currentPembayaran.pembayaran_id} to Gagal");
                }
                else
                {
                    // Create a failed payment record if none exists
                    _currentPembayaran = await _pembayaranService.CreatePembayaranAsync(
                        tiketId: _tiket.tiket_id,
                        metodePembayaran: string.IsNullOrEmpty(selectedPaymentMethod) ? "Timeout" : selectedPaymentMethod,
                        jumlahBayar: _tiket.total_harga
                    );

                    await _pembayaranService.UpdateStatusPembayaranAsync(_currentPembayaran.pembayaran_id, "Gagal");
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Created new failed payment record for timeout");
                }

                // Update ticket status to failed
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

        // ✅ TAMBAHAN: Update UI dengan data tiket yang sebenarnya
        private void UpdateUIWithTicketData()
        {
            if (_tiket?.Jadwal == null) return;

            try
            {
                var jadwal = _tiket.Jadwal;

                // ✅ PERBAIKAN: Update Order ID dengan kode tiket dari database
                if (txtOrderId != null)
                {
                    txtOrderId.Text = $"Order ID: {_tiket.kode_tiket}";
                }

                // Update Ferry Type
                if (txtFerryType != null)
                {
                    txtFerryType.Text = jadwal.kelas_layanan ?? "Reguler";
                }

                // Update Route
                if (txtDeparturePort != null && jadwal.pelabuhan_asal != null)
                {
                    txtDeparturePort.Text = jadwal.pelabuhan_asal.nama_pelabuhan;
                }

                if (txtArrivalPort != null && jadwal.pelabuhan_tujuan != null)
                {
                    txtArrivalPort.Text = jadwal.pelabuhan_tujuan.nama_pelabuhan;
                }

                // ✅ PERBAIKAN: Update Date and Time dengan tanggal keberangkatan yang benar
                if (txtDateTime != null)
                {
                    var culture = new System.Globalization.CultureInfo("id-ID");
                    string dateFormatted;

                    // Gunakan tanggal keberangkatan dari session search criteria jika tersedia
                    if (SessionManager.LastSearchCriteria?.TanggalKeberangkatan != null)
                    {
                        dateFormatted = SessionManager.LastSearchCriteria.TanggalKeberangkatan
                            .ToString("ddd, dd MMM yyyy", culture);

                        System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Using departure date from search criteria: {SessionManager.LastSearchCriteria.TanggalKeberangkatan:yyyy-MM-dd}");
                    }
                    else
                    {
                        // Fallback ke tanggal pemesanan jika search criteria tidak tersedia
                        dateFormatted = _tiket.tanggal_pemesanan.ToString("ddd, dd MMM yyyy", culture);

                        System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Fallback to booking date: {_tiket.tanggal_pemesanan:yyyy-MM-dd}");
                    }

                    var timeFormatted = jadwal.waktu_berangkat.ToString("HH:mm");
                    txtDateTime.Text = $"{dateFormatted} - {timeFormatted}";

                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Final date-time display: {txtDateTime.Text}");
                }

                // ✅ TAMBAHAN: Update detail pembayaran dengan data sebenarnya
                UpdatePaymentDetails();

                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] UI updated with ticket data: {_tiket.kode_tiket}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error updating UI: {ex.Message}");
            }
        }

        // ✅ PERBAIKAN: Update detail pembayaran menggunakan grup_kendaraan_id
        private async void UpdatePaymentDetails()
        {
            if (_tiket == null) return;

            try
            {
                var txtDetailKendaraan = FindName("txtDetailKendaraan") as TextBlock;
                var txtHargaKendaraan = FindName("txtHargaKendaraan") as TextBlock;

                if (txtDetailKendaraan != null && txtHargaKendaraan != null)
                {
                    // ✅ PERBAIKAN: Debug info untuk tracking
                    System.Diagnostics.Debug.WriteLine($"[PaymentWindow] === UPDATE PAYMENT DETAILS ===");
                    System.Diagnostics.Debug.WriteLine($"Tiket Total Harga: {_tiket.total_harga}");
                    System.Diagnostics.Debug.WriteLine($"Jenis Kendaraan Enum: '{_tiket.jenis_kendaraan_enum}'");
                    System.Diagnostics.Debug.WriteLine($"Jumlah Penumpang: {_tiket.jumlah_penumpang}");

                    // Parse jenis kendaraan dari enum string ke integer
                    int jenisKendaraanId = GetJenisKendaraanIdFromEnum(_tiket.jenis_kendaraan_enum);
                    System.Diagnostics.Debug.WriteLine($"Parsed Jenis Kendaraan ID: {jenisKendaraanId}");

                    // ✅ PERBAIKAN: Ambil detail kendaraan dari database menggunakan grup_kendaraan_id
                    var grupKendaraanId = _tiket.Jadwal.grup_kendaraan_id;
                    var detailKendaraan = await DatabaseService.GetContext().DetailKendaraans
                        .FirstOrDefaultAsync(dk =>
                            dk.grup_kendaraan_id == grupKendaraanId &&
                            dk.jenis_kendaraan == jenisKendaraanId);

                    if (detailKendaraan != null)
                    {
                        // ✅ PERBAIKAN: Gunakan deskripsi kendaraan yang benar
                        string jenisKendaraanText = GetJenisKendaraanText(detailKendaraan.jenis_kendaraan);
                        txtDetailKendaraan.Text = jenisKendaraanText;

                        // ✅ PERBAIKAN: Gunakan total harga dari tiket, bukan harga per unit
                        txtHargaKendaraan.Text = $"IDR {_tiket.total_harga:N0}";

                        System.Diagnostics.Debug.WriteLine($"Found DetailKendaraan: {jenisKendaraanText}");
                        System.Diagnostics.Debug.WriteLine($"Unit Price: {detailKendaraan.harga_kendaraan}");
                        System.Diagnostics.Debug.WriteLine($"Displayed Total: {_tiket.total_harga}");
                    }
                    else
                    {
                        // ✅ PERBAIKAN: Jika detail kendaraan tidak ditemukan
                        System.Diagnostics.Debug.WriteLine($"DetailKendaraan not found for grup_kendaraan_id: {grupKendaraanId}, jenis: {jenisKendaraanId}");

                        // Coba cari berdasarkan jenis kendaraan saja
                        var fallbackDetail = await DatabaseService.GetContext().DetailKendaraans
                            .FirstOrDefaultAsync(dk => dk.jenis_kendaraan == jenisKendaraanId);

                        if (fallbackDetail != null)
                        {
                            string jenisKendaraanText = GetJenisKendaraanText(fallbackDetail.jenis_kendaraan);
                            txtDetailKendaraan.Text = jenisKendaraanText;
                            System.Diagnostics.Debug.WriteLine($"Using fallback DetailKendaraan: {jenisKendaraanText}");
                        }
                        else
                        {
                            // Ultimate fallback - gunakan enum string dari tiket
                            txtDetailKendaraan.Text = _tiket.jenis_kendaraan_enum;
                            System.Diagnostics.Debug.WriteLine($"Using enum string from tiket: {_tiket.jenis_kendaraan_enum}");
                        }

                        // ✅ PERBAIKAN: Selalu gunakan total harga dari tiket
                        txtHargaKendaraan.Text = $"IDR {_tiket.total_harga:N0}";
                    }
                }

                // Update detail penumpang
                var txtDetailPenumpang = FindName("txtDetailPenumpang") as TextBlock;
                if (txtDetailPenumpang != null)
                {
                    int jumlahPenumpang = _tiket.jumlah_penumpang;
                    txtDetailPenumpang.Text = $"Dewasa ({jumlahPenumpang}x)";
                }

                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Payment details updated - Final display: {txtDetailKendaraan?.Text} = {txtHargaKendaraan?.Text}");
                System.Diagnostics.Debug.WriteLine($"=======================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Error updating payment details: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // ✅ TAMBAHAN: Helper method untuk convert jenis kendaraan
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

        // ✅ TAMBAHAN: Override window closing untuk stop timer
        protected override void OnClosed(EventArgs e)
        {
            _countdownTimer?.Stop();
            base.OnClosed(e);
        }

        // ✅ ADD: Window_SizeChanged event handler
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
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

        // ✅ TAMBAHAN: Method untuk menghitung total pembayaran sesuai metode
        private decimal CalculateTotalPayment()
        {
            bool isTransferBank = (selectedPaymentMethod == "BCA" || selectedPaymentMethod == "Mandiri");

            if (isTransferBank)
            {
                // Untuk bank transfer, tambahkan kode unik
                return hargaAsli + kodeUnik;
            }
            else
            {
                // Untuk metode lain (QRIS, Indomaret, Alfamart, Kartu Kredit), gunakan harga asli saja
                return hargaAsli;
            }
        }

        // ✅ TAMBAHAN: Check apakah metode pembayaran menggunakan kode unik
        private bool IsTransferBankMethod()
        {
            return selectedPaymentMethod == "BCA" || selectedPaymentMethod == "Mandiri";
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
            decimal totalPembayaran = CalculateTotalPayment(); // ✅ PERBAIKAN: Gunakan method baru
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

                if (IsTransferBankMethod()) // ✅ PERBAIKAN: Gunakan method check
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

        // ✅ PERBAIKAN: Update total pembayaran dengan logic kode unik yang benar
        private void UpdateTotalPembayaran()
        {
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
            }
            else
            {
                // Untuk nominal kecil (kurang dari 1000)
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
            }

            System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Total pembayaran updated: {totalPembayaran} (Method: {selectedPaymentMethod}, IsBank: {IsTransferBankMethod()})");
        }

        // ✅ PERBAIKAN: Helper method dengan mapping yang sesuai BookingService + fix exhaustive switch
        private int GetJenisKendaraanIdFromEnum(string jenisKendaraanEnum)
        {
            if (string.IsNullOrEmpty(jenisKendaraanEnum))
            {
                System.Diagnostics.Debug.WriteLine("[PaymentWindow] Empty or null jenis_kendaraan_enum, defaulting to 0");
                return 0;
            }

            var normalized = jenisKendaraanEnum.ToLower().Trim();

            System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Converting enum: '{jenisKendaraanEnum}' -> normalized: '{normalized}'");

            var result = normalized switch
            {
                // ✅ PERBAIKAN: Sesuai dengan output BookingService.GetJenisKendaraanText()
                "pejalan kaki" => 0,
                "sepeda" => 1,
                "sepeda motor (<500cc)" => 2,
                "sepeda motor (>500cc)" => 3,
                "mobil sedan/jeep/minibus" => 4,  // ✅ Sesuai BookingService
                "mobil barang bak muatan" => 5,   // ✅ Sesuai BookingService
                "bus penumpang (5-7m)" => 6,      // ✅ Sesuai BookingService
                "truk/tangki (5-7m)" => 7,        // ✅ Sesuai BookingService
                "bus penumpang (7-10m)" => 8,     // ✅ Sesuai BookingService
                "truk/tangki (7-10m)" => 9,       // ✅ Sesuai BookingService
                "tronton/gandengan (10-12m)" => 10, // ✅ Sesuai BookingService
                "alat berat (12-16m)" => 11,      // ✅ Sesuai BookingService
                "alat berat (>16m)" => 12,        // ✅ Sesuai BookingService
                "tidak diketahui" => 0,           // ✅ Sesuai BookingService fallback

                // Fallback untuk variasi lain
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
                "" => 0, // ✅ Handle empty string for exhaustive switch
                _ => 0   // ✅ Default fallback
            };

            System.Diagnostics.Debug.WriteLine($"[PaymentWindow] Mapped '{jenisKendaraanEnum}' -> {result}");
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

        // ✅ ADD: Method untuk membuka PaymentWindow dari window lain
        public static void Open()
        {
            var paymentWindow = new PaymentWindow();
            paymentWindow.Show();
        }
    }
}
