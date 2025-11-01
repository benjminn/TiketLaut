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
        /// Search jadwal dengan kriteria lengkap (UPDATED)
        /// </summary>
        public async Task<List<Jadwal>> SearchJadwalAsync(
            int pelabuhanAsalId,
            int pelabuhanTujuanId,
            string kelasLayanan,
            TimeOnly? jamKeberangkatan = null,
            int jenisKendaraanId = 0)
        {
            try
            {
                var query = _context.Jadwals
                    .Include(j => j.pelabuhan_asal)
                    .Include(j => j.pelabuhan_tujuan)
                    .Include(j => j.kapal)
                    .Include(j => j.DetailKendaraans)
                    .Where(j => j.pelabuhan_asal_id == pelabuhanAsalId &&
                               j.pelabuhan_tujuan_id == pelabuhanTujuanId &&
                               j.kelas_layanan == kelasLayanan &&
                               j.status == "Active" &&
                               j.sisa_kapasitas_penumpang > 0);

                // Filter berdasarkan jam jika dipilih
                if (jamKeberangkatan.HasValue)
                {
                    query = query.Where(j => j.waktu_berangkat >= jamKeberangkatan.Value);
                }

                var jadwals = await query.OrderBy(j => j.waktu_berangkat).ToListAsync();

                // Filter yang punya detail kendaraan sesuai jenis
                if (jenisKendaraanId > 0)
                {
                    jadwals = jadwals.Where(j =>
                        j.DetailKendaraans.Any(dk => dk.jenis_kendaraan == jenisKendaraanId))
                        .ToList();
                }

                return jadwals;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching jadwal: {ex.Message}");
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
                    .Include(j => j.DetailKendaraans)
                    .FirstOrDefaultAsync(j => j.jadwal_id == jadwalId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting jadwal: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get detail kendaraan untuk jadwal tertentu
        /// </summary>
        public async Task<List<DetailKendaraan>> GetDetailKendaraanByJadwalAsync(int jadwalId)
        {
            try
            {
                return await _context.DetailKendaraans
                    .Where(dk => dk.jadwal_id == jadwalId)
                    .OrderBy(dk => dk.jenis_kendaraan)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting detail kendaraan: {ex.Message}");
                return new List<DetailKendaraan>();
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
                    var detailKendaraan = jadwal.DetailKendaraans
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
    }
}