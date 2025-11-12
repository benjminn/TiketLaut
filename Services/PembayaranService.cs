using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    public class PembayaranService
    {
        private readonly AppDbContext _context;

        public PembayaranService()
        {
            _context = DatabaseService.GetContext();
        }

        /// <summary>
        /// Buat pembayaran baru setelah user konfirmasi di PaymentWindow
        /// Status awal: "Menunggu Pembayaran"
        /// </summary>
        public async Task<Pembayaran> CreatePembayaranAsync(
            int tiketId,
            string metodePembayaran,
            decimal jumlahBayar)
        {
            try
            {
                // DEBUG: Log awal
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] CreatePembayaranAsync called");
                System.Diagnostics.Debug.WriteLine($"  tiketId: {tiketId}");
                System.Diagnostics.Debug.WriteLine($"  metodePembayaran: {metodePembayaran}");
                System.Diagnostics.Debug.WriteLine($"  jumlahBayar: {jumlahBayar}");

                // Validasi tiket exists
                var tiket = await _context.Tikets.FindAsync(tiketId);
                if (tiket == null)
                {
                    throw new Exception($"Tiket dengan ID {tiketId} tidak ditemukan!");
                }

                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Tiket found: {tiket.kode_tiket}");

                // Check existing pembayaran using correct enum values
                var existingPembayaran = await _context.Pembayarans
                    .FirstOrDefaultAsync(p => p.tiket_id == tiketId &&
                        (p.status_bayar == "Menunggu Validasi" ||
                         p.status_bayar == "Aktif"));

                if (existingPembayaran != null)
                {
                    throw new Exception($"Tiket ini sudah memiliki pembayaran dengan status {existingPembayaran.status_bayar}!");
                }

                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Creating new Pembayaran record...");

                // Status awal setelah pilih metode
                var pembayaran = new Pembayaran
                {
                    tiket_id = tiketId,
                    metode_pembayaran = metodePembayaran,
                    jumlah_bayar = jumlahBayar,
                    tanggal_bayar = DateTime.UtcNow,
                    status_bayar = "Menunggu Pembayaran" // Status awal setelah pilih metode
                };

                _context.Pembayarans.Add(pembayaran);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Created payment with status: {pembayaran.status_bayar}");

                return pembayaran;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] ERROR in CreatePembayaranAsync:");
                System.Diagnostics.Debug.WriteLine($"  Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"  StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  InnerException: {ex.InnerException.Message}");
                }

                throw;
            }
        }

        /// <summary>
        /// User konfirmasi sudah melakukan pembayaran (ubah status ke Menunggu Validasi)
        /// </summary>
        public async Task<(bool success, string message)> KonfirmasiPembayaranAsync(int pembayaranId)
        {
            try
            {
                var pembayaran = await _context.Pembayarans
                    .Include(p => p.tiket)
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaranId);

                if (pembayaran == null)
                {
                    return (false, "Pembayaran tidak ditemukan");
                }

                if (pembayaran.status_bayar != "Menunggu Pembayaran")
                {
                    return (false, $"Pembayaran dengan status '{pembayaran.status_bayar}' tidak bisa dikonfirmasi");
                }

                // Update status ke Menunggu Validasi
                pembayaran.status_bayar = "Menunggu Validasi";
                pembayaran.tanggal_bayar = DateTime.UtcNow; // Update waktu konfirmasi
                
                // NOTE: Tidak update status_tiket karena database constraint belum support "Menunggu Validasi"
                // TODO: Setelah database schema diupdate, uncomment code di bawah:
                // if (pembayaran.tiket != null)
                // {
                //     pembayaran.tiket.status_tiket = "Menunggu Validasi";
                // }

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Pembayaran {pembayaranId} dikonfirmasi user");
                return (true, "Pembayaran berhasil dikonfirmasi. Menunggu validasi admin.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error KonfirmasiPembayaranAsync: {ex.Message}");
                return (false, $"Gagal konfirmasi pembayaran: {ex.Message}");
            }
        }

        /// <summary>
        /// Update status pembayaran (untuk admin konfirmasi)
        /// Status yang valid: "Menunggu Pembayaran", "Menunggu Validasi", "Aktif", "Gagal", "Selesai"
        /// </summary>
        public async Task<bool> UpdateStatusPembayaranAsync(int pembayaranId, string statusBaru)
        {
            try
            {
                var pembayaran = await _context.Pembayarans
                    .Include(p => p.tiket)
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaranId);

                if (pembayaran == null)
                {
                    throw new Exception($"Pembayaran dengan ID {pembayaranId} tidak ditemukan!");
                }

                // Validasi status yang valid
                var validStatuses = new[] { "Menunggu Pembayaran", "Menunggu Validasi", "Aktif", "Gagal", "Selesai" };
                if (!validStatuses.Contains(statusBaru))
                {
                    throw new Exception($"Status '{statusBaru}' tidak valid! Gunakan: {string.Join(", ", validStatuses)}");
                }

                // Update status pembayaran
                pembayaran.status_bayar = statusBaru;

                // Update status tiket sesuai status pembayaran
                switch (statusBaru)
                {
                    case "Aktif":
                        pembayaran.tiket.status_tiket = "Aktif";
                        break;
                    case "Gagal":
                        pembayaran.tiket.status_tiket = "Gagal";
                        break;
                    case "Selesai":
                        pembayaran.tiket.status_tiket = "Selesai";
                        break;
                }

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Pembayaran {pembayaranId} status updated to: {statusBaru}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error updating status: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update payment method and amount (when user changes payment method)
        /// </summary>
        public async Task<bool> UpdatePembayaranMethodAsync(int pembayaranId, string newMethodePembayaran, decimal newJumlahBayar)
        {
            try
            {
                var pembayaran = await _context.Pembayarans
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaranId);

                if (pembayaran == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[PembayaranService] Payment {pembayaranId} not found");
                    return false;
                }

                // Only allow updating if status is still "Menunggu Pembayaran"
                if (pembayaran.status_bayar != "Menunggu Pembayaran")
                {
                    System.Diagnostics.Debug.WriteLine($"[PembayaranService] Cannot update payment method. Current status: {pembayaran.status_bayar}");
                    return false;
                }

                var oldMethod = pembayaran.metode_pembayaran;
                var oldAmount = pembayaran.jumlah_bayar;

                // Update method and amount
                pembayaran.metode_pembayaran = newMethodePembayaran;
                pembayaran.jumlah_bayar = newJumlahBayar;
                pembayaran.tanggal_bayar = DateTime.UtcNow; // Update timestamp

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Updated payment {pembayaranId}:");
                System.Diagnostics.Debug.WriteLine($"  Method: {oldMethod} → {newMethodePembayaran}");
                System.Diagnostics.Debug.WriteLine($"  Amount: {oldAmount} → {newJumlahBayar}");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error updating payment method: {ex.Message}");
                return false;
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
                        .ThenInclude(t => t.Jadwal)
                            .ThenInclude(j => j.pelabuhan_asal)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                            .ThenInclude(j => j.pelabuhan_tujuan)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                            .ThenInclude(j => j.kapal)
                    .FirstOrDefaultAsync(p => p.tiket_id == tiketId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error getting pembayaran: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get pembayaran by ID dengan semua relasi untuk detail view
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
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.RincianPenumpangs)
                            .ThenInclude(rp => rp.penumpang)
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaranId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error GetPembayaranByIdAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get semua pembayaran untuk user tertentu
        /// </summary>
        public async Task<List<Pembayaran>> GetPembayaranByPenggunaIdAsync(int penggunaId)
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
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error getting pembayaran list: {ex.Message}");
                return new List<Pembayaran>();
            }
        }

        /// <summary>
        /// Get pembayaran dengan filter status
        /// </summary>
        public async Task<List<Pembayaran>> GetPembayaranByStatusAsync(string status)
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
                        .ThenInclude(t => t.Pengguna)
                    .Where(p => p.status_bayar == status)
                    .OrderByDescending(p => p.tanggal_bayar)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error getting pembayaran by status: {ex.Message}");
                return new List<Pembayaran>();
            }
        }

        /// <summary>
        /// Validasi apakah jumlah bayar sesuai dengan total harga tiket
        /// </summary>
        public async Task<bool> ValidateJumlahBayarAsync(int tiketId, decimal jumlahBayar)
        {
            var tiket = await _context.Tikets.FindAsync(tiketId);
            if (tiket == null) return false;

            // Toleransi untuk kode unik (misalnya +999)
            return jumlahBayar >= tiket.total_harga &&
                   jumlahBayar <= (tiket.total_harga + 999);
        }

        /// <summary>
        /// Get all pembayaran dengan relasi lengkap untuk Admin Page
        /// </summary>
        public async Task<List<Pembayaran>> GetAllPembayaranAsync()
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
                    .OrderByDescending(p => p.tanggal_bayar)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error GetAllPembayaranAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Admin validasi pembayaran - ubah status dari Menunggu Validasi ke Sukses
        /// </summary>
        public async Task<(bool success, string message)> ValidasiPembayaranAsync(int pembayaranId)
        {
            try
            {
                var pembayaran = await _context.Pembayarans
                    .Include(p => p.tiket)
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaranId);

                if (pembayaran == null)
                {
                    return (false, "Pembayaran tidak ditemukan");
                }

                if (pembayaran.status_bayar == "Sukses")
                {
                    return (false, "Pembayaran sudah divalidasi sebelumnya");
                }

                if (pembayaran.status_bayar != "Menunggu Validasi")
                {
                    return (false, $"Hanya pembayaran dengan status 'Menunggu Validasi' yang bisa divalidasi. Status saat ini: {pembayaran.status_bayar}");
                }

                // Update status pembayaran menjadi Sukses
                pembayaran.status_bayar = "Sukses";
                
                // Update status tiket menjadi aktif
                if (pembayaran.tiket != null)
                {
                    pembayaran.tiket.status_tiket = "Aktif";
                }

                await _context.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Pembayaran {pembayaranId} validated successfully");
                return (true, "Pembayaran berhasil divalidasi dan tiket telah diaktifkan");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error ValidasiPembayaranAsync: {ex.Message}");
                return (false, $"Gagal validasi pembayaran: {ex.Message}");
            }
        }

        /// <summary>
        /// Tolak pembayaran - ubah status menjadi Gagal dan batalkan tiket
        /// </summary>
        public async Task<(bool success, string message)> TolakPembayaranAsync(int pembayaranId, string alasan)
        {
            try
            {
                var pembayaran = await _context.Pembayarans
                    .Include(p => p.tiket)
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaranId);

                if (pembayaran == null)
                {
                    return (false, "Pembayaran tidak ditemukan");
                }

                if (pembayaran.status_bayar == "Aktif")
                {
                    return (false, "Pembayaran yang sudah aktif tidak bisa ditolak");
                }

                // Update status pembayaran
                pembayaran.status_bayar = "Gagal";
                
                // Update status tiket menjadi gagal
                if (pembayaran.tiket != null)
                {
                    pembayaran.tiket.status_tiket = "Gagal";
                }

                await _context.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Pembayaran {pembayaranId} ditolak: {alasan}");
                return (true, "Pembayaran berhasil ditolak");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error TolakPembayaranAsync: {ex.Message}");
                return (false, $"Gagal tolak pembayaran: {ex.Message}");
            }
        }

        /// <summary>
        /// Mark payment as failed due to validation timeout or admin rejection
        /// </summary>
        public async Task<bool> MarkPaymentAsFailedAsync(int pembayaranId, string reason = "Tidak tervalidasi")
        {
            try
            {
                var pembayaran = await _context.Pembayarans
                    .Include(p => p.tiket)
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaranId);

                if (pembayaran == null)
                {
                    throw new Exception($"Pembayaran dengan ID {pembayaranId} tidak ditemukan!");
                }

                // Only allow changing status if currently waiting for validation
                if (pembayaran.status_bayar != "Menunggu Validasi")
                {
                    throw new Exception($"Pembayaran tidak dalam status 'Menunggu Validasi'. Status saat ini: {pembayaran.status_bayar}");
                }

                // Update payment status to failed
                pembayaran.status_bayar = "Gagal";

                // Update ticket status to failed
                pembayaran.tiket.status_tiket = "Gagal";

                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Payment {pembayaranId} marked as failed. Reason: {reason}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error marking payment as failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get all payments that are pending validation and past deadline (for automated cleanup)
        /// </summary>
        public async Task<List<Pembayaran>> GetExpiredPendingPaymentsAsync(int timeoutHours = 48)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-timeoutHours);

                return await _context.Pembayarans
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                    .Where(p => p.status_bayar == "Menunggu Validasi" &&
                               p.tanggal_bayar < cutoffTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error getting expired pending payments: {ex.Message}");
                return new List<Pembayaran>();
            }
        }

        /// <summary>
        /// Auto-mark expired pending payments as failed (for background service)
        /// </summary>
        public async Task<int> AutoMarkExpiredPaymentsAsFailedAsync(int timeoutHours = 48)
        {
            try
            {
                var expiredPayments = await GetExpiredPendingPaymentsAsync(timeoutHours);
                int failedCount = 0;

                foreach (var payment in expiredPayments)
                {
                    try
                    {
                        await MarkPaymentAsFailedAsync(payment.pembayaran_id, "Timeout validasi");
                        failedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PembayaranService] Failed to mark payment {payment.pembayaran_id} as failed: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Auto-marked {failedCount} expired payments as failed");
                return failedCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error in auto-mark expired payments: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get unique metode pembayaran untuk filter dropdown
        /// </summary>
        public async Task<List<string>> GetUniqueMetodePembayaranAsync()
        {
            try
            {
                return await _context.Pembayarans
                    .Where(p => !string.IsNullOrEmpty(p.metode_pembayaran))
                    .Select(p => p.metode_pembayaran)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error GetUniqueMetodePembayaranAsync: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get unique pengguna names untuk filter dropdown
        /// </summary>
        public async Task<List<string>> GetUniquePenggunaAsync()
        {
            try
            {
                return await _context.Pembayarans
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Pengguna)
                    .Where(p => p.tiket != null && p.tiket.Pengguna != null)
                    .Select(p => p.tiket.Pengguna.nama)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error GetUniquePenggunaAsync: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Update pembayaran (status, metode, keterangan)
        /// </summary>
        public async Task<(bool success, string message)> UpdatePembayaranAsync(Pembayaran pembayaran)
        {
            try
            {
                // Validasi input
                if (pembayaran == null)
                {
                    return (false, "Data pembayaran tidak valid");
                }

                // Get existing pembayaran from database
                var existingPembayaran = await _context.Pembayarans
                    .Include(p => p.tiket)
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaran.pembayaran_id);

                if (existingPembayaran == null)
                {
                    return (false, "Pembayaran tidak ditemukan");
                }

                // Update fields
                existingPembayaran.status_bayar = pembayaran.status_bayar;
                existingPembayaran.metode_pembayaran = pembayaran.metode_pembayaran;

                _context.Pembayarans.Update(existingPembayaran);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Pembayaran {pembayaran.pembayaran_id} updated successfully");
                return (true, "Pembayaran berhasil diperbarui");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PembayaranService] Error UpdatePembayaranAsync: {ex.Message}");
                return (false, $"Error: {ex.Message}");
            }
        }
    }
}