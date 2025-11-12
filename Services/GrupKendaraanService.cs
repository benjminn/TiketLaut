using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Data;
using TiketLaut.Services;

namespace TiketLaut
{
    /// <summary>
    /// Service untuk mengelola GrupKendaraan
    /// CATATAN PENTING: DatabaseService.GetContext() mengembalikan singleton AppDbContext.
    /// Jangan gunakan 'using' statement untuk dispose context karena akan menyebabkan
    /// "Cannot access a disposed context instance" error saat window dibuka berulang kali.
    /// </summary>
    public class GrupKendaraanService
    {
        /// <summary>
        /// Create grup kendaraan dengan 13 detail kendaraan (1 per golongan)
        /// </summary>
        /// <param name="namaGrup">Nama grup, e.g., "Set Harga November 2025"</param>
        /// <param name="hargaPerGolongan">Dictionary: JenisKendaraan -> Harga</param>
        /// <returns>grup_kendaraan_id yang baru dibuat</returns>
        public async Task<(bool success, string message, GrupKendaraan? grup)> 
            CreateGrupWithDetailAsync(string namaGrup, Dictionary<JenisKendaraan, decimal> hargaPerGolongan)
        {
            // Gunakan context tanpa using statement karena DatabaseService mengembalikan singleton
            var context = DatabaseService.GetContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Validation: harus ada 13 golongan
                if (hargaPerGolongan.Count != 13)
                {
                    return (false, $"Harus ada 13 golongan kendaraan! Saat ini: {hargaPerGolongan.Count}", null);
                }

                // Validation: semua golongan harus ada
                var allGolongan = Enum.GetValues(typeof(JenisKendaraan)).Cast<JenisKendaraan>().ToList();
                var missingGolongan = allGolongan.Where(g => !hargaPerGolongan.ContainsKey(g)).ToList();
                if (missingGolongan.Any())
                {
                    var missingNames = string.Join(", ", missingGolongan);
                    return (false, $"Golongan hilang: {missingNames}", null);
                }

                // Check if exact same grup already exists (reusability)
                var existingGrup = await FindExistingGrupAsync(context, hargaPerGolongan);
                if (existingGrup != null)
                {
                    await transaction.CommitAsync();
                    return (true, $"Grup sudah ada (reused): {existingGrup.nama_grup_kendaraan}", existingGrup);
                }

                // Create new grup
                var newGrup = new GrupKendaraan
                {
                    nama_grup_kendaraan = namaGrup,
                    created_at = DateTime.UtcNow
                };

                context.GrupKendaraans.Add(newGrup);
                await context.SaveChangesAsync();

                // Create 13 detail kendaraan
                foreach (var (jenis, harga) in hargaPerGolongan)
                {
                    var detailKendaraan = DetailKendaraan.Create(jenis, harga);
                    detailKendaraan.grup_kendaraan_id = newGrup.grup_kendaraan_id;
                    context.DetailKendaraans.Add(detailKendaraan);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Grup '{namaGrup}' berhasil dibuat dengan 13 detail kendaraan", newGrup);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Find existing grup with exact same harga configuration (for reusability)
        /// </summary>
        private async Task<GrupKendaraan?> FindExistingGrupAsync(
            AppDbContext context, 
            Dictionary<JenisKendaraan, decimal> hargaPerGolongan)
        {
            var allGrups = await context.GrupKendaraans
                .Include(g => g.DetailKendaraans)
                .Where(g => g.DetailKendaraans.Count == 13)
                .ToListAsync();

            foreach (var grup in allGrups)
            {
                // Check if all 13 details match exactly
                var allMatch = grup.DetailKendaraans.All(dk =>
                {
                    var jenis = (JenisKendaraan)dk.jenis_kendaraan;
                    return hargaPerGolongan.ContainsKey(jenis) && 
                           hargaPerGolongan[jenis] == dk.harga_kendaraan;
                });

                if (allMatch)
                {
                    return grup;
                }
            }

            return null;
        }

        /// <summary>
        /// Get grup kendaraan by ID with all detail kendaraan
        /// </summary>
        public async Task<GrupKendaraan?> GetGrupByIdAsync(int grupKendaraanId)
        {
            // Gunakan context tanpa using statement karena DatabaseService mengembalikan singleton
            var context = DatabaseService.GetContext();
            // AsNoTracking untuk read-only operation, hindari tracking overhead
            var grup = await context.GrupKendaraans
                .AsNoTracking()
                .Include(g => g.DetailKendaraans)
                .FirstOrDefaultAsync(g => g.grup_kendaraan_id == grupKendaraanId);

            // Materialize DetailKendaraans untuk menghindari lazy loading setelah context disposed
            if (grup != null && grup.DetailKendaraans != null)
            {
                _ = grup.DetailKendaraans.Count; // Force load
            }

            return grup;
        }

        /// <summary>
        /// Get all grup kendaraan with usage count
        /// </summary>
        public async Task<List<GrupKendaraanWithUsage>> GetAllGrupWithUsageAsync()
        {
            // Gunakan context tanpa using statement karena DatabaseService mengembalikan singleton
            var context = DatabaseService.GetContext();
            
            // Load data dengan AsNoTracking untuk performa lebih baik
            var grups = await context.GrupKendaraans
                .AsNoTracking()
                .Include(g => g.DetailKendaraans)
                .Include(g => g.Jadwals)
                .Select(g => new GrupKendaraanWithUsage
                {
                    grup_kendaraan_id = g.grup_kendaraan_id,
                    nama_grup_kendaraan = g.nama_grup_kendaraan,
                    created_at = g.created_at,
                    jumlah_detail_kendaraan = g.DetailKendaraans.Count,
                    jumlah_jadwal = g.Jadwals.Count,
                    // Materialize detail_kendaraans ke List untuk menghindari lazy loading setelah context disposed
                    detail_kendaraans = g.DetailKendaraans.OrderBy(dk => dk.jenis_kendaraan)
                        .Select(dk => new DetailKendaraan
                        {
                            detail_kendaraan_id = dk.detail_kendaraan_id,
                            grup_kendaraan_id = dk.grup_kendaraan_id,
                            jenis_kendaraan = dk.jenis_kendaraan,
                            harga_kendaraan = dk.harga_kendaraan
                        }).ToList()
                })
                .OrderByDescending(g => g.jumlah_jadwal)
                .ToListAsync();

            return grups;
        }

        /// <summary>
        /// Delete grup kendaraan (akan cascade delete detail kendaraan)
        /// WARNING: Tidak bisa delete jika masih dipakai oleh jadwal
        /// </summary>
        public async Task<(bool success, string message)> DeleteGrupAsync(int grupKendaraanId)
        {
            // Gunakan context tanpa using statement karena DatabaseService mengembalikan singleton
            var context = DatabaseService.GetContext();
            
            var grup = await context.GrupKendaraans
                .Include(g => g.Jadwals)
                .FirstOrDefaultAsync(g => g.grup_kendaraan_id == grupKendaraanId);

            if (grup == null)
            {
                return (false, "Grup tidak ditemukan");
            }

            if (grup.Jadwals.Any())
            {
                return (false, $"Tidak bisa hapus grup yang masih dipakai {grup.Jadwals.Count} jadwal");
            }

            try
            {
                context.GrupKendaraans.Remove(grup);
                await context.SaveChangesAsync();
                return (true, "Grup berhasil dihapus");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Helper class untuk menampilkan grup dengan usage info
    /// </summary>
    public class GrupKendaraanWithUsage
    {
        public int grup_kendaraan_id { get; set; }
        public string nama_grup_kendaraan { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public int jumlah_detail_kendaraan { get; set; }
        public int jumlah_jadwal { get; set; }
        public List<DetailKendaraan> detail_kendaraans { get; set; } = new List<DetailKendaraan>();

        public string DisplayInfo => $"{nama_grup_kendaraan} ({jumlah_jadwal} jadwal, {jumlah_detail_kendaraan} golongan)";
    }
}
