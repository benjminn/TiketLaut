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

        // Get all pelabuhan untuk dropdown
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

        // Search jadwal berdasarkan kriteria
        public async Task<List<Jadwal>> SearchJadwalAsync(
            int pelabuhanAsalId, 
            int pelabuhanTujuanId, 
            DateTime tanggal)
        {
            try
            {
                return await _context.Jadwals
                    .Include(j => j.pelabuhan_asal)
                    .Include(j => j.pelabuhan_tujuan)
                    .Include(j => j.kapal)
                    .Include(j => j.DetailKendaraans)
                    .Where(j => j.pelabuhan_asal_id == pelabuhanAsalId &&
                               j.pelabuhan_tujuan_id == pelabuhanTujuanId &&
                               j.status == "Active" &&
                               j.sisa_kapasitas_penumpang > 0)
                    .OrderBy(j => j.waktu_berangkat)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching jadwal: {ex.Message}");
                return new List<Jadwal>();
            }
        }

        // Get jadwal by ID
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
    }
}