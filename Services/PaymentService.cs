using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    /// <summary>
    /// Service untuk menangani proses pembayaran tiket
    /// </summary>
    public class PaymentService
    {
        private readonly AppDbContext _context;

        public PaymentService()
        {
            _context = DatabaseService.GetContext();
        }

        /// <summary>
        /// Create pembayaran baru setelah booking
        /// </summary>
        public async Task<Pembayaran> CreatePembayaranAsync(int tiketId, string metodePembayaran, int kodeUnik = 0)
        {
            try
            {
                // Get tiket untuk ambil total harga
                var tiket = await _context.Tikets
                    .Include(t => t.Pengguna)
                    .Include(t => t.Jadwal)
                    .FirstOrDefaultAsync(t => t.tiket_id == tiketId);

                if (tiket == null)
                {
                    throw new Exception($"Tiket dengan ID {tiketId} tidak ditemukan");
                }

                // Hitung jumlah bayar (tambahkan kode unik jika transfer bank)
                decimal jumlahBayar = tiket.total_harga;
                if (metodePembayaran.Contains("BCA") || metodePembayaran.Contains("Mandiri"))
                {
                    jumlahBayar += kodeUnik;
                }

                // Buat pembayaran baru
                var pembayaran = new Pembayaran
                {
                    tiket_id = tiketId,
                    metode_pembayaran = metodePembayaran,
                    jumlah_bayar = jumlahBayar,
                    tanggal_bayar = DateTime.UtcNow,
                    status_bayar = "Menunggu Pembayaran"
                };

                _context.Pembayarans.Add(pembayaran);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[PaymentService] Pembayaran created: ID={pembayaran.pembayaran_id}, Jumlah={pembayaran.jumlah_bayar}");

                return pembayaran;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentService] Error creating payment: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update status pembayaran
        /// </summary>
        public async Task<bool> UpdateStatusPembayaranAsync(int pembayaranId, string newStatus)
        {
            try
            {
                var pembayaran = await _context.Pembayarans
                    .Include(p => p.tiket)
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaranId);

                if (pembayaran == null) return false;

                pembayaran.status_bayar = newStatus;
                pembayaran.tanggal_bayar = DateTime.UtcNow;

                // Update status tiket jika pembayaran dikonfirmasi
                if (newStatus == "Lunas" || newStatus == "Confirmed")
                {
                    pembayaran.tiket.status_tiket = "Paid";
                }
                else if (newStatus == "Gagal" || newStatus == "Expired")
                {
                    pembayaran.tiket.status_tiket = "Cancelled";
                }

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[PaymentService] Status updated: {pembayaranId} -> {newStatus}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentService] Error updating status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Konfirmasi pembayaran (admin/automatic)
        /// </summary>
        public async Task<bool> KonfirmasiPembayaranAsync(int pembayaranId)
        {
            return await UpdateStatusPembayaranAsync(pembayaranId, "Lunas");
        }

        /// <summary>
        /// Get pembayaran by ID dengan include relasi
        /// </summary>
        public async Task<Pembayaran?> GetPembayaranByIdAsync(int pembayaranId)
        {
            try
            {
                return await _context.Pembayarans
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Pengguna)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                            .ThenInclude(j => j.pelabuhan_asal)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                            .ThenInclude(j => j.pelabuhan_tujuan)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                            .ThenInclude(j => j.kapal)
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaranId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentService] Error getting payment: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get pembayaran by tiket ID
        /// </summary>
        public async Task<Pembayaran?> GetPembayaranByTiketIdAsync(int tiketId)
        {
            try
            {
                return await _context.Pembayarans
                    .Include(p => p.tiket)
                    .FirstOrDefaultAsync(p => p.tiket_id == tiketId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentService] Error getting payment by tiket: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get riwayat pembayaran pengguna
        /// </summary>
        public async Task<List<Pembayaran>> GetRiwayatPembayaranAsync(int penggunaId)
        {
            try
            {
                return await _context.Pembayarans
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                            .ThenInclude(j => j.pelabuhan_asal)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                            .ThenInclude(j => j.pelabuhan_tujuan)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                            .ThenInclude(j => j.kapal)
                    .Where(p => p.tiket.pengguna_id == penggunaId)
                    .OrderByDescending(p => p.tanggal_bayar)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentService] Error getting payment history: {ex.Message}");
                return new List<Pembayaran>();
            }
        }

        /// <summary>
        /// Check status pembayaran (simulasi untuk demo)
        /// </summary>
        public async Task<string> CheckStatusPembayaranAsync(int pembayaranId)
        {
            var pembayaran = await GetPembayaranByIdAsync(pembayaranId);
            return pembayaran?.status_bayar ?? "Tidak Ditemukan";
        }

        /// <summary>
        /// Generate payment reference number
        /// </summary>
        public string GeneratePaymentReference(int pembayaranId, string metodePembayaran)
        {
            string prefix = metodePembayaran.Substring(0, Math.Min(3, metodePembayaran.Length)).ToUpper();
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{prefix}-{timestamp}-{pembayaranId:D6}";
        }

        /// <summary>
        /// Calculate payment expiry time
        /// </summary>
        public DateTime GetPaymentExpiry()
        {
            // Pembayaran expire setelah 24 jam
            return DateTime.UtcNow.AddHours(24);
        }

        /// <summary>
        /// Auto-cancel expired payments (untuk background job)
        /// </summary>
        public async Task<int> CancelExpiredPaymentsAsync()
        {
            try
            {
                var expiredPayments = await _context.Pembayarans
                    .Include(p => p.tiket)
                    .Where(p => p.status_bayar == "Menunggu Pembayaran" &&
                               p.tanggal_bayar < DateTime.UtcNow.AddHours(-24))
                    .ToListAsync();

                foreach (var payment in expiredPayments)
                {
                    payment.status_bayar = "Expired";
                    payment.tiket.status_tiket = "Cancelled";
                }

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[PaymentService] Cancelled {expiredPayments.Count} expired payments");
                return expiredPayments.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentService] Error cancelling expired payments: {ex.Message}");
                return 0;
            }
        }
    }

    /// <summary>
    /// DTO untuk tracking pembayaran
    /// </summary>
    public class PaymentTrackingData
    {
        public int PembayaranId { get; set; }
        public int TiketId { get; set; }
        public string KodeTiket { get; set; } = string.Empty;
        public string PaymentReference { get; set; } = string.Empty;
        public string MetodePembayaran { get; set; } = string.Empty;
        public decimal JumlahBayar { get; set; }
        public string StatusBayar { get; set; } = string.Empty;
        public DateTime TanggalBayar { get; set; }
        public DateTime ExpiredAt { get; set; }
        public int KodeUnik { get; set; }
    }
}