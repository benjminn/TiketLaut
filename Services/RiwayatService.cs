using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    /// <summary>
    /// Service untuk menangani riwayat perjalanan yang sudah selesai
    /// </summary>
    public class RiwayatService
    {
        private readonly AppDbContext _context;

        public RiwayatService()
        {
            _context = DatabaseService.GetContext();
        }

        /// <summary>
        /// ✨ AUTO-UPDATE: Ubah status_bayar "Aktif" menjadi "Selesai" jika waktu keberangkatan sudah lewat
        /// </summary>
        public async Task<int> AutoUpdatePembayaranSelesaiAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("==============================================");
                System.Diagnostics.Debug.WriteLine($"[RiwayatService] AUTO-UPDATE STARTED at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Current UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Current Local: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine("==============================================");
                
                var nowUtc = DateTime.UtcNow;
                var nowLocal = DateTime.Now;
                int totalUpdated = 0;

                // ✅ BAGIAN 1: Update "Aktif" atau "Sukses" menjadi "Selesai" jika waktu tiba sudah lewat
                var pembayaranAktif = await _context.Pembayarans
                    .AsNoTracking()
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                    .Where(p => p.status_bayar == "Aktif" || p.status_bayar == "Sukses")
                    .Select(p => new
                    {
                        p.pembayaran_id,
                        p.status_bayar,
                        p.tiket.tanggal_pemesanan,
                        p.tiket.Jadwal.waktu_berangkat,
                        p.tiket.Jadwal.waktu_tiba,
                        p.tiket.kode_tiket
                    })
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Found {pembayaranAktif.Count} active/sukses payments in database");

                var idsToComplete = pembayaranAktif
                    .Where(p =>
                    {
                        // Try both UTC and Local time comparison
                        var waktuTibaUtc = DateTime.SpecifyKind(p.waktu_tiba, DateTimeKind.Utc);
                        var waktuTibaLocal = DateTime.SpecifyKind(p.waktu_tiba, DateTimeKind.Local);
                        var shouldCompleteUtc = waktuTibaUtc < nowUtc;
                        var shouldCompleteLocal = waktuTibaLocal < nowLocal;
                        
                        System.Diagnostics.Debug.WriteLine($"  - Tiket {p.kode_tiket}:");
                        System.Diagnostics.Debug.WriteLine($"    DB waktu_tiba: {p.waktu_tiba:yyyy-MM-dd HH:mm:ss}");
                        System.Diagnostics.Debug.WriteLine($"    As UTC: {waktuTibaUtc:yyyy-MM-dd HH:mm:ss}, should_complete={shouldCompleteUtc}");
                        System.Diagnostics.Debug.WriteLine($"    As Local: {waktuTibaLocal:yyyy-MM-dd HH:mm:ss}, should_complete={shouldCompleteLocal}");
                        
                        // Use local time comparison since database likely stores local time
                        return shouldCompleteLocal;
                    })
                    .Select(p => p.pembayaran_id)
                    .ToList();

                if (idsToComplete.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Found {idsToComplete.Count} payments to update to Selesai");

                    string completeIds = string.Join(",", idsToComplete);
                    
                    // Update status pembayaran menjadi "Selesai"
                    string completeSql = $@"
                UPDATE ""Pembayaran"" 
                SET status_bayar = 'Selesai' 
                WHERE pembayaran_id IN ({completeIds})";

                    int completedRows = await _context.Database.ExecuteSqlRawAsync(completeSql);
                    totalUpdated += completedRows;

                    // Update status tiket menjadi "Selesai"
                    string updateTiketSql = $@"
                UPDATE ""Tiket"" 
                SET status_tiket = 'Selesai' 
                WHERE tiket_id IN (
                    SELECT tiket_id FROM ""Pembayaran"" 
                    WHERE pembayaran_id IN ({completeIds})
                )";

                    await _context.Database.ExecuteSqlRawAsync(updateTiketSql);

                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Updated {completedRows} payments and tickets to Selesai");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] No payments to update to Selesai");
                }

                // ✅ BAGIAN 1B: Update SEMUA jadwal yang waktu tiba sudah lewat menjadi "Inactive"
                // (tidak peduli ada tiket atau tidak)
                string updateAllJadwalSql = @"
                UPDATE ""Jadwal"" 
                SET status = 'Inactive' 
                WHERE status = 'Active' 
                AND waktu_tiba < NOW()";

                int jadwalRows = await _context.Database.ExecuteSqlRawAsync(updateAllJadwalSql);
                
                if (jadwalRows > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Updated {jadwalRows} jadwals from Active to Inactive");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] No jadwals to update to Inactive");
                }

                // ✅ BAGIAN 2: Mark tiket "Menunggu Pembayaran" sebagai "Gagal" jika:
                // - Timeout 24 jam dari tanggal pemesanan, ATAU
                // - Jadwal keberangkatan sudah lewat
                var timeoutCutoff = nowUtc.AddHours(-24); // 24 hours ago
                var timeoutCutoffLocal = nowLocal.AddHours(-24);

                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Checking 'Menunggu Pembayaran' tikets...");
                System.Diagnostics.Debug.WriteLine($"  Timeout cutoff (UTC): {timeoutCutoff:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"  Timeout cutoff (Local): {timeoutCutoffLocal:yyyy-MM-dd HH:mm:ss}");

                // Query TIKET table, not Pembayaran table (status is in status_tiket)
                var tiketsTimeout = await _context.Tikets
                    .AsNoTracking()
                    .Include(t => t.Jadwal)
                    .Include(t => t.Pembayarans)
                    .Where(t => t.status_tiket == "Menunggu Pembayaran")
                    .Select(t => new
                    {
                        t.tiket_id,
                        t.kode_tiket,
                        t.tanggal_pemesanan,
                        t.Jadwal.waktu_berangkat,
                        pembayaran_ids = t.Pembayarans.Select(p => p.pembayaran_id).ToList()
                    })
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Found {tiketsTimeout.Count} 'Menunggu Pembayaran' tikets");

                // Filter in memory with detailed logging
                var tiketsToFail = tiketsTimeout
                    .Where(t =>
                    {
                        var pemesananLocal = DateTime.SpecifyKind(t.tanggal_pemesanan, DateTimeKind.Local);
                        var berangkatLocal = DateTime.SpecifyKind(t.waktu_berangkat, DateTimeKind.Local);
                        
                        var isTimeout = pemesananLocal < timeoutCutoffLocal;
                        var isDeparted = berangkatLocal < nowLocal;
                        
                        if (isTimeout || isDeparted)
                        {
                            var reason = isTimeout ? "timeout 24 jam" : "jadwal sudah berangkat";
                            System.Diagnostics.Debug.WriteLine($"  - Tiket {t.kode_tiket}:");
                            System.Diagnostics.Debug.WriteLine($"    Pemesanan: {pemesananLocal:yyyy-MM-dd HH:mm:ss}, isTimeout={isTimeout}");
                            System.Diagnostics.Debug.WriteLine($"    Berangkat: {berangkatLocal:yyyy-MM-dd HH:mm:ss}, isDeparted={isDeparted}");
                            System.Diagnostics.Debug.WriteLine($"    Reason: {reason}");
                            return true;
                        }
                        return false;
                    })
                    .ToList();

                if (tiketsToFail.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Found {tiketsToFail.Count} timed-out/expired tikets to mark as Gagal");

                    // Update tiket status to "Gagal"
                    string tiketIds = string.Join(",", tiketsToFail.Select(t => t.tiket_id));
                    string updateTiketSql = $@"
                UPDATE ""Tiket"" 
                SET status_tiket = 'Gagal' 
                WHERE tiket_id IN ({tiketIds})";

                    int tiketRows = await _context.Database.ExecuteSqlRawAsync(updateTiketSql);
                    totalUpdated += tiketRows;
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Marked {tiketRows} tikets as Gagal");

                    // Update pembayaran status to "Gagal" for these tickets
                    var pembayaranIds = tiketsToFail.SelectMany(t => t.pembayaran_ids).Distinct().ToList();
                    if (pembayaranIds.Any())
                    {
                        string pembayaranIdsStr = string.Join(",", pembayaranIds);
                        string updatePembayaranSql = $@"
                UPDATE ""Pembayaran"" 
                SET status_bayar = 'Gagal' 
                WHERE pembayaran_id IN ({pembayaranIdsStr})";

                        await _context.Database.ExecuteSqlRawAsync(updatePembayaranSql);
                        System.Diagnostics.Debug.WriteLine($"[RiwayatService] Updated {pembayaranIds.Count} pembayaran records to Gagal");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] No payments to update");
                }

                // ✅ BAGIAN 3: Mark pembayaran "Menunggu Validasi" yang sudah 48 jam sebagai "Gagal"
                var validationTimeoutCutoff = nowUtc.AddHours(-48); // 48 hours ago

                var pembayaranValidationTimeout = await _context.Pembayarans
                    .AsNoTracking()
                    .Include(p => p.tiket)
                    .Where(p => p.status_bayar == "Menunggu Validasi" &&
                               p.tanggal_bayar < validationTimeoutCutoff)
                    .Select(p => new
                    {
                        p.pembayaran_id,
                        p.tiket.kode_tiket,
                        p.tanggal_bayar
                    })
                    .ToListAsync();

                if (pembayaranValidationTimeout.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Found {pembayaranValidationTimeout.Count} validation-timed-out payments to mark as Gagal");

                    string validationTimeoutIds = string.Join(",", pembayaranValidationTimeout.Select(p => p.pembayaran_id));
                    string validationTimeoutSql = $@"
                UPDATE ""Pembayaran"" 
                SET status_bayar = 'Gagal' 
                WHERE pembayaran_id IN ({validationTimeoutIds})";

                    int validationTimeoutRows = await _context.Database.ExecuteSqlRawAsync(validationTimeoutSql);
                    totalUpdated += validationTimeoutRows;

                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Marked {validationTimeoutRows} validation-timed-out payments as Gagal");

                    // Update corresponding tickets
                    string updateValidationTiketSql = $@"
                UPDATE ""Tiket"" 
                SET status_tiket = 'Gagal' 
                WHERE tiket_id IN (
                    SELECT tiket_id FROM ""Pembayaran"" 
                    WHERE pembayaran_id IN ({validationTimeoutIds})
                )";

                    await _context.Database.ExecuteSqlRawAsync(updateValidationTiketSql);
                }

                if (totalUpdated == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[RiwayatService] No payments to update");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Total updated payments: {totalUpdated}");
                }

                return totalUpdated;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RiwayatService] StackTrace: {ex.StackTrace}");
                return 0;
            }
        }

        /// <summary>
        /// ✅ Get riwayat pembayaran user - HANYA yang "Selesai" (tidak termasuk "Gagal")
        /// </summary>
        public async Task<List<Pembayaran>> GetRiwayatByPenggunaIdAsync(int penggunaId)
        {
            try
            {
                // ✅ AUTO-UPDATE: Jalankan auto-update dulu
                await AutoUpdatePembayaranSelesaiAsync();

                // ✅ UPDATED: Include both "Selesai" and "Gagal" in history
                var riwayat = await _context.Pembayarans
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
                        .ThenInclude(t => t.Pengguna)
                    .Where(p => p.tiket.pengguna_id == penggunaId &&
                                (p.status_bayar == "Selesai" || p.status_bayar == "Gagal"))
                    .OrderByDescending(p => p.tiket.tanggal_pemesanan)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Found {riwayat.Count} completed/failed trips for user {penggunaId}");

                return riwayat;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RiwayatService] StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Get detail lengkap riwayat termasuk data penumpang
        /// </summary>
        public async Task<Pembayaran?> GetDetailRiwayatAsync(int pembayaranId)
        {
            try
            {
                var detail = await _context.Pembayarans
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
                            .ThenInclude(r => r.penumpang)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Pengguna)
                    .FirstOrDefaultAsync(p => p.pembayaran_id == pembayaranId);

                return detail;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Error getting detail: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get statistik riwayat user (hanya yang "Selesai")
        /// </summary>
        public async Task<RiwayatStats> GetRiwayatStatsAsync(int penggunaId)
        {
            try
            {
                var riwayatSelesai = await GetRiwayatByPenggunaIdAsync(penggunaId);

                var stats = new RiwayatStats
                {
                    TotalPerjalanan = riwayatSelesai.Count,
                    TotalPengeluaran = riwayatSelesai.Sum(r => r.jumlah_bayar),
                    PerjalananTahunIni = riwayatSelesai.Count(r => r.tiket.tanggal_pemesanan.Year == DateTime.UtcNow.Year)
                };

                // Hitung rute favorit
                if (riwayatSelesai.Any())
                {
                    var ruteFavorit = riwayatSelesai
                        .GroupBy(r => $"{r.tiket.Jadwal.pelabuhan_asal.nama_pelabuhan} - {r.tiket.Jadwal.pelabuhan_tujuan.nama_pelabuhan}")
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault();

                    stats.RuteFavorit = ruteFavorit?.Key ?? "Belum ada";
                }

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Error calculating stats: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Model untuk statistik riwayat
    /// </summary>
    public class RiwayatStats
    {
        public int TotalPerjalanan { get; set; }
        public decimal TotalPengeluaran { get; set; }
        public int PerjalananTahunIni { get; set; }
        public string RuteFavorit { get; set; } = string.Empty;
    }
}
