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

                // Get IDs yang perlu di-update
                var pembayaranAktif = await _context.Pembayarans
                    .AsNoTracking()
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                    .Where(p => p.status_bayar == "Aktif")
                    .Select(p => new
                    {
                        p.pembayaran_id,
                        p.tiket.tanggal_pemesanan,  // ❌ INI YANG SALAH - Harusnya tanggal keberangkatan
                        p.tiket.Jadwal.waktu_berangkat,
                        p.tiket.Jadwal.waktu_tiba,
                        p.tiket.kode_tiket
                    })
                    .ToListAsync();

                // ✅ FIX: Hanya update jika kapal sudah TIBA (bukan hanya berangkat)
                var idsToUpdate = pembayaranAktif
                    .Where(p =>
                    {
                        // waktu_tiba sudah DateTime (timestamptz) dari database
                        var waktuTibaUtc = DateTime.SpecifyKind(p.waktu_tiba, DateTimeKind.Utc);

                        System.Diagnostics.Debug.WriteLine(
                            $"[RiwayatService] Checking pembayaran {p.pembayaran_id} " +
                            $"(Tiket: {p.kode_tiket}): waktuTiba={waktuTibaUtc:yyyy-MM-dd HH:mm}, now={nowUtc:yyyy-MM-dd HH:mm}");

                        return waktuTibaUtc < nowUtc;
                    })
                    .Select(p => p.pembayaran_id)
                    .ToList();

                if (!idsToUpdate.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[RiwayatService] No payments to update");
                    return 0;
                }

                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Found {idsToUpdate.Count} payments to update to Selesai");
                foreach (var id in idsToUpdate)
                {
                    System.Diagnostics.Debug.WriteLine($"  - Pembayaran ID: {id}");
                }

                // ✅ USE RAW SQL - Bypass EF tracking
                string ids = string.Join(",", idsToUpdate);
                string sql = $@"
                    UPDATE ""Pembayaran"" 
                    SET status_bayar = 'Selesai' 
                    WHERE pembayaran_id IN ({ids})";

                int rowsAffected = await _context.Database.ExecuteSqlRawAsync(sql);

                System.Diagnostics.Debug.WriteLine(
                    $"[RiwayatService] Updated {rowsAffected} payments using raw SQL");

                return rowsAffected;
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

                // ✅ HANYA ambil pembayaran dengan status "Selesai"
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
                                p.status_bayar == "Selesai") // ✅ HANYA "Selesai"
                    .OrderByDescending(p => p.tiket.tanggal_pemesanan)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[RiwayatService] Found {riwayat.Count} completed trips for user {penggunaId}");

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