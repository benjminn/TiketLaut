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

        /// <summary>
        /// Get all pelabuhan untuk dropdown
        /// </summary>
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

        /// <summary>
        /// Search jadwal dengan kriteria lengkap (UPDATED - WITH DateTime)
        /// </summary>
        public async Task<List<Jadwal>> SearchJadwalAsync(
            int pelabuhanAsalId,
            int pelabuhanTujuanId,
            string kelasLayanan,
            DateTime? tanggalKeberangkatan = null,
            int jenisKendaraanId = 0)
        {
            try
            {
                // ? DEBUG: Log input parameters
                System.Diagnostics.Debug.WriteLine($"[JadwalService.SearchJadwal] INPUT:");
                System.Diagnostics.Debug.WriteLine($"  - Pelabuhan Asal ID: {pelabuhanAsalId}");
                System.Diagnostics.Debug.WriteLine($"  - Pelabuhan Tujuan ID: {pelabuhanTujuanId}");
                System.Diagnostics.Debug.WriteLine($"  - Kelas Layanan: {kelasLayanan}");
                System.Diagnostics.Debug.WriteLine($"  - Tanggal: {tanggalKeberangkatan?.ToString("yyyy-MM-dd") ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"  - Jenis Kendaraan ID: {jenisKendaraanId}");

                // ? PASTIKAN .Include() untuk navigation properties
                var query = _context.Jadwals
                    .Include(j => j.pelabuhan_asal)        // ? WAJIB!
                    .Include(j => j.pelabuhan_tujuan)      // ? WAJIB!
                    .Include(j => j.kapal)                 // ? WAJIB!
                    .Include(j => j.GrupKendaraan!)         // Include grup kendaraan
                        .ThenInclude(g => g.DetailKendaraans)  // Include all detail kendaraan in grup
                    .Where(j => j.pelabuhan_asal_id == pelabuhanAsalId &&
                               j.pelabuhan_tujuan_id == pelabuhanTujuanId &&
                               j.kelas_layanan == kelasLayanan &&
                               j.status == "Active" &&
                               j.sisa_kapasitas_penumpang > 0);

                // ? DEBUG: Check total jadwal before date filter
                var countBeforeDate = await query.CountAsync();
                System.Diagnostics.Debug.WriteLine($"[JadwalService] Found {countBeforeDate} jadwal(s) before date filter");

                if (tanggalKeberangkatan.HasValue)
                {
                    // ? FIX: Convert local date to UTC range properly
                    // User memilih tanggal dalam local timezone, kita perlu convert ke UTC range
                    // Contoh: User pilih 01/11/2025 (WIB) → cari 31/10/2025 17:00 UTC sampai 01/11/2025 17:00 UTC
                    var localDate = tanggalKeberangkatan.Value.Date;
                    var localStartDateTime = DateTime.SpecifyKind(localDate, DateTimeKind.Local);
                    var localEndDateTime = localStartDateTime.AddDays(1);
                    
                    // Convert to UTC
                    var startDateUtc = localStartDateTime.ToUniversalTime();
                    var endDateUtc = localEndDateTime.ToUniversalTime();
                    
                    query = query.Where(j => j.waktu_berangkat >= startDateUtc && j.waktu_berangkat < endDateUtc);
                    
                    System.Diagnostics.Debug.WriteLine($"[JadwalService] Date filter:");
                    System.Diagnostics.Debug.WriteLine($"  Local: {localStartDateTime:yyyy-MM-dd HH:mm} to {localEndDateTime:yyyy-MM-dd HH:mm}");
                    System.Diagnostics.Debug.WriteLine($"  UTC:   {startDateUtc:yyyy-MM-dd HH:mm} to {endDateUtc:yyyy-MM-dd HH:mm}");
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

                // ? DEBUG: Cek apakah navigation property ter-load
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

        /// <summary>
        /// Get jadwal by ID
        /// </summary>
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

        /// <summary>
        /// Get all detail kendaraan untuk jadwal tertentu (returns all 13 DetailKendaraan in grup)
        /// </summary>
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

        /// <summary>
        /// Get specific detail kendaraan by jenis from jadwal's grup
        /// </summary>
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

        /// <summary>
        /// Check ketersediaan kapasitas
        /// </summary>
        public async Task<bool> CheckAvailabilityAsync(int jadwalId, int jumlahPenumpang, int jenisKendaraanId)
        {
            try
            {
                var jadwal = await GetJadwalByIdAsync(jadwalId);
                if (jadwal == null) return false;

                // Check kapasitas penumpang
                if (jadwal.sisa_kapasitas_penumpang < jumlahPenumpang)
                    return false;

                // Check kapasitas kendaraan jika bukan jalan kaki
                if (jenisKendaraanId > 0)
                {
                    // Get detail kendaraan from grup by jenis
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

        /// <summary>
        /// Get all jadwal by kapal ID untuk detail kapal
        /// </summary>
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

        /// <summary>
        /// Get all jadwal by pelabuhan ID (asal atau tujuan) untuk detail pelabuhan
        /// </summary>
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

        /// <summary>
        /// Get all jadwal untuk admin management (FIXED - filter NULL timestamps)
        /// </summary>
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

        /// <summary>
        /// Get all kapal untuk dropdown
        /// </summary>
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

        /// <summary>
        /// Create single jadwal with time conflict validation
        /// </summary>
        public async Task<(bool success, string message)> CreateJadwalAsync(Jadwal jadwal)
        {
            try
            {
                // Validasi kapal capacity
                var kapal = await _context.Kapals.FindAsync(jadwal.kapal_id);
                if (kapal == null)
                {
                    return (false, "Kapal tidak ditemukan!");
                }

                // Validasi waktu tiba harus setelah waktu berangkat
                if (jadwal.waktu_tiba <= jadwal.waktu_berangkat)
                {
                    return (false, "Waktu tiba harus setelah waktu berangkat!");
                }

                // Validasi time conflict untuk kapal yang sama
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

        /// <summary>
        /// Check time conflict untuk kapal tertentu
        /// </summary>
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
                // Check overlap: new schedule starts during existing schedule
                if (waktuBerangkat >= existing.waktu_berangkat && waktuBerangkat < existing.waktu_tiba)
                    return true;

                // Check overlap: new schedule ends during existing schedule
                if (waktuTiba > existing.waktu_berangkat && waktuTiba <= existing.waktu_tiba)
                    return true;

                // Check overlap: new schedule completely covers existing schedule
                if (waktuBerangkat <= existing.waktu_berangkat && waktuTiba >= existing.waktu_tiba)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Bulk create jadwal - untuk multiple waktu dan tanggal dengan conflict validation
        /// </summary>
        public async Task<(bool success, string message, int count)> BulkCreateJadwalAsync(List<Jadwal> jadwals)
        {
            try
            {
                if (jadwals == null || jadwals.Count == 0)
                {
                    return (false, "Tidak ada jadwal untuk ditambahkan!", 0);
                }

                // Validasi kapal untuk semua jadwal
                var kapalIds = jadwals.Select(j => j.kapal_id).Distinct().ToList();
                var kapals = await _context.Kapals
                    .Where(k => kapalIds.Contains(k.kapal_id))
                    .ToDictionaryAsync(k => k.kapal_id);

                var validJadwals = new List<Jadwal>();
                var conflictCount = 0;

                // Validasi setiap jadwal
                foreach (var jadwal in jadwals)
                {
                    if (!kapals.ContainsKey(jadwal.kapal_id))
                    {
                        return (false, $"Kapal dengan ID {jadwal.kapal_id} tidak ditemukan!", 0);
                    }

                    // Validasi waktu tiba harus setelah waktu berangkat
                    if (jadwal.waktu_tiba <= jadwal.waktu_berangkat)
                    {
                        conflictCount++;
                        continue; // Skip jadwal ini
                    }

                    // Check time conflict
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

                    // Check conflict dengan jadwal lain dalam batch ini
                    bool batchConflict = false;
                    foreach (var validJadwal in validJadwals)
                    {
                        if (validJadwal.kapal_id == jadwal.kapal_id)
                        {
                            // Check overlap
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

        /// <summary>
        /// Update jadwal with time conflict validation
        /// </summary>
        public async Task<(bool success, string message)> UpdateJadwalAsync(Jadwal jadwal)
        {
            try
            {
                var existing = await _context.Jadwals.FindAsync(jadwal.jadwal_id);
                if (existing == null)
                {
                    return (false, "Jadwal tidak ditemukan!");
                }

                // Validasi waktu tiba harus setelah waktu berangkat
                if (jadwal.waktu_tiba <= jadwal.waktu_berangkat)
                {
                    return (false, "Waktu tiba harus setelah waktu berangkat!");
                }

                // Check time conflict dengan jadwal lain (exclude jadwal ini)
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

        /// <summary>
        /// Delete single jadwal
        /// </summary>
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

        /// <summary>
        /// Bulk delete jadwal
        /// </summary>
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

        /// <summary>
        /// Get all tikets for a specific jadwal
        /// </summary>
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