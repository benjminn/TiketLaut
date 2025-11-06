using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    public class TiketService
    {
        private readonly AppDbContext _context;

        public TiketService()
        {
            _context = DatabaseService.GetContext();
        }

        // Get tiket by ID with all related data
        public async Task<Tiket?> GetTiketByIdAsync(int tiketId)
        {
            return await _context.Tikets
                .Include(t => t.Pengguna)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.pelabuhan_asal)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.pelabuhan_tujuan)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.kapal)
                .FirstOrDefaultAsync(t => t.tiket_id == tiketId);
        }

        // Get all tikets
        public async Task<List<Tiket>> GetAllTiketsAsync()
        {
            return await _context.Tikets
                .Include(t => t.Pengguna)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.pelabuhan_asal)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.pelabuhan_tujuan)
                .OrderByDescending(t => t.tanggal_pemesanan)
                .ToListAsync();
        }

        // Get tikets by user ID
        public async Task<List<Tiket>> GetTiketsByUserIdAsync(int userId)
        {
            return await _context.Tikets
                .Include(t => t.Pengguna)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.pelabuhan_asal)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.pelabuhan_tujuan)
                .Where(t => t.pengguna_id == userId)
                .OrderByDescending(t => t.tanggal_pemesanan)
                .ToListAsync();
        }

        // Update tiket status
        public async Task<(bool success, string message)> UpdateTiketStatusAsync(int tiketId, string newStatus)
        {
            try
            {
                var tiket = await _context.Tikets.FindAsync(tiketId);
                if (tiket == null)
                {
                    return (false, "Tiket tidak ditemukan");
                }

                tiket.status_tiket = newStatus;
                await _context.SaveChangesAsync();

                return (true, "Status tiket berhasil diupdate");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
    }
}
