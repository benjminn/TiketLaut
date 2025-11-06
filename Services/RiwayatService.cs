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
                var nowUtc = DateTime.UtcNow;
                int totalUpdated = 0;

                // ✅ BAGIAN 1: Update "Aktif" menjadi "Selesai" jika waktu tiba sudah lewat
                var pembayaranAktif = await _context.Pembayarans
                    .AsNoTracking()
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                    .Where(p => p.status_bayar == "Aktif")
                    .Select(p => new
                    {
                        p.pembayaran_id,
                        p.tiket.tanggal_pemesanan,
                        p.tiket.Jadwal.waktu_berangkat,
                        p.tiket.Jadwal.waktu_tiba,
                        p.tiket.kode_tiket
                    })
                    .ToListAsync();

                var idsToComplete = pembayaranAktif
                    .Where(p =>
                    {
                        var waktuTibaUtc = DateTime.SpecifyKind(p.waktu_tiba, DateTimeKind.Utc);
                        return waktuTibaUtc < nowUtc;
                    })
                    .Select(p => p.pembayaran_id)
                    .ToList();

                if (idsToComplete.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Found {idsToComplete.Count} payments to update to Selesai");

                    string completeIds = string.Join(",", idsToComplete);
                    string completeSql = $@"
                UPDATE ""Pembayaran"" 
                SET status_bayar = 'Selesai' 
                WHERE pembayaran_id IN ({completeIds})";

                    int completedRows = await _context.Database.ExecuteSqlRawAsync(completeSql);
                    totalUpdated += completedRows;

                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Updated {completedRows} payments to Selesai");
                }

                // ✅ BAGIAN 2: Mark pembayaran yang timeout (24 jam) sebagai "Gagal"
                var timeoutCutoff = nowUtc.AddHours(-24); // 24 hours ago

                var pembayaranTimeout = await _context.Pembayarans
                    .AsNoTracking()
                    .Include(p => p.tiket)
                    .Where(p => p.status_bayar == "Menunggu Pembayaran" &&
                               p.tiket.tanggal_pemesanan < timeoutCutoff)
                    .Select(p => new
                    {
                        p.pembayaran_id,
                        p.tiket.kode_tiket,
                        p.tiket.tanggal_pemesanan
                    })
                    .ToListAsync();

                if (pembayaranTimeout.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Found {pembayaranTimeout.Count} timed-out payments to mark as Gagal");

                    foreach (var timeout in pembayaranTimeout)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Tiket: {timeout.kode_tiket}, Booking: {timeout.tanggal_pemesanan}");
                    }

                    string timeoutIds = string.Join(",", pembayaranTimeout.Select(p => p.pembayaran_id));
                    string timeoutSql = $@"
                UPDATE ""Pembayaran"" 
                SET status_bayar = 'Gagal' 
                WHERE pembayaran_id IN ({timeoutIds})";

                    int timeoutRows = await _context.Database.ExecuteSqlRawAsync(timeoutSql);
                    totalUpdated += timeoutRows;

                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Marked {timeoutRows} timed-out payments as Gagal");

                    // ✅ BAGIAN 3: Update status tiket yang timeout juga menjadi "Gagal"
                    var tiketIds = string.Join(",", pembayaranTimeout.Select(p => p.pembayaran_id));
                    string updateTiketSql = $@"
                UPDATE ""Tiket"" 
                SET status_tiket = 'Gagal' 
                WHERE tiket_id IN (
                    SELECT tiket_id FROM ""Pembayaran"" 
                    WHERE pembayaran_id IN ({timeoutIds})
                )";

                    await _context.Database.ExecuteSqlRawAsync(updateTiketSql);
                    System.Diagnostics.Debug.WriteLine($"[RiwayatService] Updated corresponding tickets to Gagal status");
                }

                // ✅ BAGIAN 4: Mark pembayaran "Menunggu Validasi" yang sudah 48 jam sebagai "Gagal"
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

