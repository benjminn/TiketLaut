using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Data;
using TiketLaut.Services;

namespace TiketLaut
{
    public class GrupKendaraanService
    {
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
                var existingGrup = await FindExistingGrupAsync(context, hargaPerGolongan);
                if (existingGrup != null)
                {
                    await transaction.CommitAsync();
                    return (true, $"Grup sudah ada (reused): {existingGrup.nama_grup_kendaraan}", existingGrup);
                }
                var newGrup = new GrupKendaraan
                {
                    nama_grup_kendaraan = namaGrup,
                    created_at = DateTime.UtcNow
                };

                context.GrupKendaraans.Add(newGrup);
                await context.SaveChangesAsync();
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
