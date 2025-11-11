using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    public class JadwalService
    {
        private readonly AppDbContext _context;

        public JadwalService()
        {
            _context = DatabaseService.GetContext();
        }
        public async Task<List<Pelabuhan>> GetAllPelabuhanAsync()
        {
            try
            {
                return await _context.Pelabuhans
                    .OrderBy(p => p.nama_pelabuhan)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting pelabuhan: {ex.Message}");
                return new List<Pelabuhan>();
            }
        }
        public async Task<List<Jadwal>> SearchJadwalAsync(
            int pelabuhanAsalId,
            int pelabuhanTujuanId,
            string kelasLayanan,
            DateTime? tanggalKeberangkatan = null,
            int jenisKendaraanId = 0,
            int? jamKeberangkatan = null)
        {
            try
            {
                                System.Diagnostics.Debug.WriteLine($"[JadwalService.SearchJadwal] INPUT:");
                System.Diagnostics.Debug.WriteLine($"  - Pelabuhan Asal ID: {pelabuhanAsalId}");
                System.Diagnostics.Debug.WriteLine($"  - Pelabuhan Tujuan ID: {pelabuhanTujuanId}");
                System.Diagnostics.Debug.WriteLine($"  - Kelas Layanan: {kelasLayanan}");
                System.Diagnostics.Debug.WriteLine($"  - Tanggal: {tanggalKeberangkatan?.ToString("yyyy-MM-dd") ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"  - Jam: {jamKeberangkatan?.ToString() ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"  - Jenis Kendaraan ID: {jenisKendaraanId}");

                                var query = _context.Jadwals
                    .Include(j => j.pelabuhan_asal)                            .Include(j => j.pelabuhan_tujuan)                          .Include(j => j.kapal)                                     .Include(j => j.GrupKendaraan!)
                        .ThenInclude(g => g.DetailKendaraans)
                    .Where(j => j.pelabuhan_asal_id == pelabuhanAsalId &&
                               j.pelabuhan_tujuan_id == pelabuhanTujuanId &&
                               j.kelas_layanan == kelasLayanan &&
                               j.status == "Active" &&
                               j.sisa_kapasitas_penumpang > 0);

                                var countBeforeDate = await query.CountAsync();
                System.Diagnostics.Debug.WriteLine($"[JadwalService] Found {countBeforeDate} jadwal(s) before date filter");

                if (tanggalKeberangkatan.HasValue)
                {
                    var localDate = tanggalKeberangkatan.Value.Date;
                    
                    if (jamKeberangkatan.HasValue)
                    {
                        var localStartDateTime = DateTime.SpecifyKind(localDate.AddHours(jamKeberangkatan.Value), DateTimeKind.Local);
                        var localEndDateTime = localStartDateTime.Date.AddDays(1);
                        
                        var startDateUtc = localStartDateTime.ToUniversalTime();
                        var endDateUtc = localEndDateTime.ToUniversalTime();
                        
                        query = query.Where(j => j.waktu_berangkat >= startDateUtc && j.waktu_berangkat < endDateUtc);
                        
                        System.Diagnostics.Debug.WriteLine($"[JadwalService] Date + Hour filter (>= jam {jamKeberangkatan.Value}):");
                        System.Diagnostics.Debug.WriteLine($"  Local: {localStartDateTime:yyyy-MM-dd HH:mm} to {localEndDateTime:yyyy-MM-dd HH:mm}");
                        System.Diagnostics.Debug.WriteLine($"  UTC:   {startDateUtc:yyyy-MM-dd HH:mm} to {endDateUtc:yyyy-MM-dd HH:mm}");
                    }
                    else
                    {
                        var localStartDateTime = DateTime.SpecifyKind(localDate, DateTimeKind.Local);
                        var localEndDateTime = localStartDateTime.AddDays(1);
                        
                        var startDateUtc = localStartDateTime.ToUniversalTime();
                        var endDateUtc = localEndDateTime.ToUniversalTime();
                        
                        query = query.Where(j => j.waktu_berangkat >= startDateUtc && j.waktu_berangkat < endDateUtc);
                        
                        System.Diagnostics.Debug.WriteLine($"[JadwalService] Date only filter:");
                        System.Diagnostics.Debug.WriteLine($"  Local: {localStartDateTime:yyyy-MM-dd HH:mm} to {localEndDateTime:yyyy-MM-dd HH:mm}");
                        System.Diagnostics.Debug.WriteLine($"  UTC:   {startDateUtc:yyyy-MM-dd HH:mm} to {endDateUtc:yyyy-MM-dd HH:mm}");
                    }
                }

                var jadwals = await query
                    .OrderBy(j => j.waktu_berangkat)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"[JadwalService] Found {jadwals.Count} jadwal(s) after date filter");

                if (jenisKendaraanId > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[JadwalService] Applying vehicle filter for jenis_kendaraan = {jenisKendaraanId}");
                    
                    // Filter only jadwal that has matching detail kendaraan in grup
                    jadwals = jadwals.Where(j =>
                        j.GrupKendaraan != null && 
                        j.GrupKendaraan.DetailKendaraans != null &&
                        j.GrupKendaraan.DetailKendaraans.Any(dk => dk.jenis_kendaraan == jenisKendaraanId))
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"[JadwalService] Found {jadwals.Count} jadwal(s) after vehicle filter");
                }

                                foreach (var jadwal in jadwals)
                {
                    System.Diagnostics.Debug.WriteLine($"[JadwalService] Jadwal {jadwal.jadwal_id}:");
                    System.Diagnostics.Debug.WriteLine($"  - Asal: {jadwal.pelabuhan_asal?.nama_pelabuhan ?? "NULL"}");
                    System.Diagnostics.Debug.WriteLine($"  - Tujuan: {jadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "NULL"}");
                    System.Diagnostics.Debug.WriteLine($"  - Kapal: {jadwal.kapal?.nama_kapal ?? "NULL"}");
                    System.Diagnostics.Debug.WriteLine($"  - Kelas: {jadwal.kelas_layanan}");
                    System.Diagnostics.Debug.WriteLine($"  - Waktu Berangkat: {jadwal.waktu_berangkat:yyyy-MM-dd HH:mm}");
                    System.Diagnostics.Debug.WriteLine($"  - Grup: {jadwal.GrupKendaraan?.grup_kendaraan_id ?? 0} ({jadwal.GrupKendaraan?.DetailKendaraans?.Count ?? 0} detail)");
                }

                return jadwals;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JadwalService] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[JadwalService] StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[JadwalService] InnerException: {ex.InnerException.Message}");
                }
                return new List<Jadwal>();
            }
        }
        public async Task<Jadwal?> GetJadwalByIdAsync(int jadwalId)
        {
            try
            {
                return await _context.Jadwals
                    .Include(j => j.pelabuhan_asal)
                    .Include(j => j.pelabuhan_tujuan)
                    .Include(j => j.kapal)
                    .Include(j => j.GrupKendaraan)
                        .ThenInclude(g => g!.DetailKendaraans)
                    .FirstOrDefaultAsync(j => j.jadwal_id == jadwalId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting jadwal: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Jadwal>> GetAllAsync()
        {
            return await _context.Jadwals.Include(j => j.pelabuhan_asal)
                                        .Include(j => j.pelabuhan_tujuan)
                                        .ToListAsync();
        }
        public async Task<List<DetailKendaraan>> GetDetailKendaraanByJadwalAsync(int jadwalId)
        {
            try
            {
                var jadwal = await _context.Jadwals
                    .Include(j => j.GrupKendaraan)
                        .ThenInclude(g => g!.DetailKendaraans)
                    .FirstOrDefaultAsync(j => j.jadwal_id == jadwalId);
                
                return jadwal?.GrupKendaraan?.DetailKendaraans?.OrderBy(dk => dk.jenis_kendaraan).ToList() 
                    ?? new List<DetailKendaraan>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting detail kendaraan: {ex.Message}");
                return new List<DetailKendaraan>();
            }
        }
        public async Task<DetailKendaraan?> GetDetailKendaraanByJenisAsync(int jadwalId, JenisKendaraan jenis)
        {
            try
            {
                var jadwal = await _context.Jadwals
                    .Include(j => j.GrupKendaraan)
                        .ThenInclude(g => g!.DetailKendaraans)
                    .FirstOrDefaultAsync(j => j.jadwal_id == jadwalId);
                
                return jadwal?.GrupKendaraan?.DetailKendaraans?
                    .FirstOrDefault(dk => dk.jenis_kendaraan == (int)jenis);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting detail kendaraan by jenis: {ex.Message}");
                return null;
            }
        }
        public async Task<bool> CheckAvailabilityAsync(int jadwalId, int jumlahPenumpang, int jenisKendaraanId)
        {
            try
            {
                var jadwal = await GetJadwalByIdAsync(jadwalId);
                if (jadwal == null) return false;
                if (jadwal.sisa_kapasitas_penumpang < jumlahPenumpang)
                    return false;
                if (jenisKendaraanId > 0)
                {
                    var detailKendaraan = jadwal.GrupKendaraan?.DetailKendaraans
                        .FirstOrDefault(dk => dk.jenis_kendaraan == jenisKendaraanId);

                    if (detailKendaraan == null) return false;

                    // Hitung bobot yang dibutuhkan
                    int bobotDibutuhkan = detailKendaraan.bobot_unit;
                    if (jadwal.sisa_kapasitas_kendaraan < bobotDibutuhkan)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking availability: {ex.Message}");
                return false;
            }
        }
        public async Task<List<Jadwal>> GetJadwalByKapalIdAsync(int kapalId)
        {
            try
            {
                return await _context.Jadwals
                    .Include(j => j.pelabuhan_asal)
                    .Include(j => j.pelabuhan_tujuan)
                    .Include(j => j.kapal)
                    .Where(j => j.kapal_id == kapalId)
                    .OrderByDescending(j => j.waktu_berangkat)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting jadwal by kapal: {ex.Message}");
                return new List<Jadwal>();
            }
        }
        public async Task<List<Jadwal>> GetJadwalByPelabuhanIdAsync(int pelabuhanId)
        {
            try
            {
                return await _context.Jadwals
                    .Include(j => j.pelabuhan_asal)
                    .Include(j => j.pelabuhan_tujuan)
                    .Include(j => j.kapal)
                    .Where(j => j.pelabuhan_asal_id == pelabuhanId || j.pelabuhan_tujuan_id == pelabuhanId)
                    .OrderByDescending(j => j.waktu_berangkat)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting jadwal by pelabuhan: {ex.Message}");
                return new List<Jadwal>();
            }
        }
        public async Task<List<Jadwal>> GetAllJadwalAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[JadwalService] Loading all jadwal...");
                
                var jadwals = await _context.Jadwals
                    .Include(j => j.pelabuhan_asal)
                    .Include(j => j.pelabuhan_tujuan)
                    .Include(j => j.kapal)
                    .Where(j => j.waktu_berangkat != default(DateTime) && j.waktu_tiba != default(DateTime))  // Filter out NULL/default timestamps
                    .OrderByDescending(j => j.waktu_berangkat)
                    .ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"[JadwalService] Loaded {jadwals.Count} valid jadwal (filtered out NULL timestamps)");
                
                // Debug: Show first few records
                foreach (var jadwal in jadwals.Take(3))
                {
                    System.Diagnostics.Debug.WriteLine($"  - ID {jadwal.jadwal_id}: {jadwal.pelabuhan_asal?.nama_pelabuhan} → {jadwal.pelabuhan_tujuan?.nama_pelabuhan}, " +
                        $"Berangkat: {jadwal.waktu_berangkat:yyyy-MM-dd HH:mm}, Tiba: {jadwal.waktu_tiba:yyyy-MM-dd HH:mm}");
                }
                
                return jadwals;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JadwalService] ERROR getting all jadwal: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[JadwalService] StackTrace: {ex.StackTrace}");
                return new List<Jadwal>();
            }
        }
        public async Task<List<Kapal>> GetAllKapalAsync()
        {
            try
            {
                return await _context.Kapals
                    .OrderBy(k => k.nama_kapal)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting kapal: {ex.Message}");
                return new List<Kapal>();
            }
        }
        public async Task<(bool success, string message)> CreateJadwalAsync(Jadwal jadwal)
        {
            try
            {
                var kapal = await _context.Kapals.FindAsync(jadwal.kapal_id);
                if (kapal == null)
                {
                    return (false, "Kapal tidak ditemukan!");
                }
                if (jadwal.waktu_tiba <= jadwal.waktu_berangkat)
                {
                    return (false, "Waktu tiba harus setelah waktu berangkat!");
                }
                var hasConflict = await CheckTimeConflictAsync(
                    jadwal.kapal_id,
                    jadwal.waktu_berangkat,
                    jadwal.waktu_tiba,
                    null);

                if (hasConflict)
                {
                    return (false, "Jadwal bertabrakan dengan jadwal lain pada kapal ini! Silakan pilih waktu yang berbeda.");
                }

                // Set sisa kapasitas sama dengan max capacity
                jadwal.sisa_kapasitas_penumpang = kapal.kapasitas_penumpang_max;
                jadwal.sisa_kapasitas_kendaraan = kapal.kapasitas_kendaraan_max;

                // Ensure UTC
                jadwal.waktu_berangkat = DateTime.SpecifyKind(jadwal.waktu_berangkat, DateTimeKind.Utc);
                jadwal.waktu_tiba = DateTime.SpecifyKind(jadwal.waktu_tiba, DateTimeKind.Utc);

                _context.Jadwals.Add(jadwal);
                await _context.SaveChangesAsync();
                return (true, "Jadwal berhasil ditambahkan!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
        private async Task<bool> CheckTimeConflictAsync(
            int kapalId,
            DateTime waktuBerangkat,
            DateTime waktuTiba,
            int? excludeJadwalId)
        {
            var query = _context.Jadwals
                .Where(j => j.kapal_id == kapalId);

            if (excludeJadwalId.HasValue)
            {
                query = query.Where(j => j.jadwal_id != excludeJadwalId.Value);
            }

            var existingJadwals = await query.ToListAsync();

            foreach (var existing in existingJadwals)
            {
                if (waktuBerangkat >= existing.waktu_berangkat && waktuBerangkat < existing.waktu_tiba)
                    return true;
                if (waktuTiba > existing.waktu_berangkat && waktuTiba <= existing.waktu_tiba)
                    return true;
                if (waktuBerangkat <= existing.waktu_berangkat && waktuTiba >= existing.waktu_tiba)
                    return true;
            }

            return false;
        }
        public async Task<(bool success, string message, int count)> BulkCreateJadwalAsync(List<Jadwal> jadwals)
        {
            try
            {
                if (jadwals == null || jadwals.Count == 0)
                {
                    return (false, "Tidak ada jadwal untuk ditambahkan!", 0);
                }
                var kapalIds = jadwals.Select(j => j.kapal_id).Distinct().ToList();
                var kapals = await _context.Kapals
                    .Where(k => kapalIds.Contains(k.kapal_id))
                    .ToDictionaryAsync(k => k.kapal_id);

                var validJadwals = new List<Jadwal>();
                var conflictCount = 0;
                foreach (var jadwal in jadwals)
                {
                    if (!kapals.ContainsKey(jadwal.kapal_id))
                    {
                        return (false, $"Kapal dengan ID {jadwal.kapal_id} tidak ditemukan!", 0);
                    }
                    if (jadwal.waktu_tiba <= jadwal.waktu_berangkat)
                    {
                        conflictCount++;
                        continue; // Skip jadwal ini
                    }
                    var hasConflict = await CheckTimeConflictAsync(
                        jadwal.kapal_id,
                        jadwal.waktu_berangkat,
                        jadwal.waktu_tiba,
                        null);

                    if (hasConflict)
                    {
                        conflictCount++;
                        continue; // Skip jadwal yang conflict
                    }
                    bool batchConflict = false;
                    foreach (var validJadwal in validJadwals)
                    {
                        if (validJadwal.kapal_id == jadwal.kapal_id)
                        {
                            if ((jadwal.waktu_berangkat >= validJadwal.waktu_berangkat && jadwal.waktu_berangkat < validJadwal.waktu_tiba) ||
                                (jadwal.waktu_tiba > validJadwal.waktu_berangkat && jadwal.waktu_tiba <= validJadwal.waktu_tiba) ||
                                (jadwal.waktu_berangkat <= validJadwal.waktu_berangkat && jadwal.waktu_tiba >= validJadwal.waktu_tiba))
                            {
                                batchConflict = true;
                                break;
                            }
                        }
                    }

                    if (batchConflict)
                    {
                        conflictCount++;
                        continue;
                    }

                    var kapal = kapals[jadwal.kapal_id];
                    jadwal.sisa_kapasitas_penumpang = kapal.kapasitas_penumpang_max;
                    jadwal.sisa_kapasitas_kendaraan = kapal.kapasitas_kendaraan_max;

                    // Ensure UTC
                    jadwal.waktu_berangkat = DateTime.SpecifyKind(jadwal.waktu_berangkat, DateTimeKind.Utc);
                    jadwal.waktu_tiba = DateTime.SpecifyKind(jadwal.waktu_tiba, DateTimeKind.Utc);

                    validJadwals.Add(jadwal);
                }

                if (validJadwals.Count == 0)
                {
                    return (false, $"Semua jadwal bertabrakan atau tidak valid! ({conflictCount} conflict)", 0);
                }

                _context.Jadwals.AddRange(validJadwals);
                await _context.SaveChangesAsync();

                var message = $"Berhasil menambahkan {validJadwals.Count} jadwal!";
                if (conflictCount > 0)
                {
                    message += $" ({conflictCount} jadwal dilewati karena conflict)";
                }

                return (true, message, validJadwals.Count);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", 0);
            }
        }
        public async Task<(bool success, string message)> UpdateJadwalAsync(Jadwal jadwal)
        {
            try
            {
                var existing = await _context.Jadwals.FindAsync(jadwal.jadwal_id);
                if (existing == null)
                {
                    return (false, "Jadwal tidak ditemukan!");
                }
                if (jadwal.waktu_tiba <= jadwal.waktu_berangkat)
                {
                    return (false, "Waktu tiba harus setelah waktu berangkat!");
                }
                var hasConflict = await CheckTimeConflictAsync(
                    jadwal.kapal_id,
                    jadwal.waktu_berangkat,
                    jadwal.waktu_tiba,
                    jadwal.jadwal_id);

                if (hasConflict)
                {
                    return (false, "Jadwal bertabrakan dengan jadwal lain untuk kapal yang sama!");
                }

                existing.pelabuhan_asal_id = jadwal.pelabuhan_asal_id;
                existing.pelabuhan_tujuan_id = jadwal.pelabuhan_tujuan_id;
                existing.kapal_id = jadwal.kapal_id;
                existing.waktu_berangkat = DateTime.SpecifyKind(jadwal.waktu_berangkat, DateTimeKind.Utc);
                existing.waktu_tiba = DateTime.SpecifyKind(jadwal.waktu_tiba, DateTimeKind.Utc);
                existing.kelas_layanan = jadwal.kelas_layanan;
                existing.status = jadwal.status;

                await _context.SaveChangesAsync();
                return (true, "Jadwal berhasil diupdate!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
        public async Task<(bool success, string message)> DeleteJadwalAsync(int jadwalId)
        {
            try
            {
                // Cek apakah jadwal sedang digunakan dalam tiket
                var isUsed = await _context.Tikets.AnyAsync(t => t.jadwal_id == jadwalId);
                
                if (isUsed)
                {
                    return (false, "Jadwal tidak dapat dihapus karena sudah ada tiket yang dibeli!");
                }

                var jadwal = await _context.Jadwals.FindAsync(jadwalId);
                if (jadwal == null)
                {
                    return (false, "Jadwal tidak ditemukan!");
                }

                _context.Jadwals.Remove(jadwal);
                await _context.SaveChangesAsync();
                return (true, "Jadwal berhasil dihapus!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
        public async Task<(bool success, string message, int count)> BulkDeleteJadwalAsync(List<int> jadwalIds)
        {
            try
            {
                if (jadwalIds == null || jadwalIds.Count == 0)
                {
                    return (false, "Tidak ada jadwal untuk dihapus!", 0);
                }

                // Cek apakah ada jadwal yang digunakan dalam tiket
                var usedJadwals = await _context.Tikets
                    .Where(t => jadwalIds.Contains(t.jadwal_id))
                    .Select(t => t.jadwal_id)
                    .Distinct()
                    .ToListAsync();

                if (usedJadwals.Count > 0)
                {
                    return (false, $"{usedJadwals.Count} jadwal tidak dapat dihapus karena sudah ada tiket yang dibeli!", 0);
                }

                var jadwals = await _context.Jadwals
                    .Where(j => jadwalIds.Contains(j.jadwal_id))
                    .ToListAsync();

                if (jadwals.Count == 0)
                {
                    return (false, "Tidak ada jadwal yang ditemukan!", 0);
                }

                _context.Jadwals.RemoveRange(jadwals);
                await _context.SaveChangesAsync();
                return (true, $"Berhasil menghapus {jadwals.Count} jadwal!", jadwals.Count);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", 0);
            }
        }
        public async Task<List<Tiket>> GetTiketsByJadwalIdAsync(int jadwalId)
        {
            try
            {
                return await _context.Tikets
                    .Include(t => t.Pengguna)
                    .Where(t => t.jadwal_id == jadwalId)
                    .OrderByDescending(t => t.tanggal_pemesanan)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting tikets: {ex.Message}");
                return new List<Tiket>();
            }
        }
    }
}
