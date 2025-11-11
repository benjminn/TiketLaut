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

        // Delete tiket
        public async Task<(bool success, string message)> DeleteTiketAsync(int tiketId)
        {
            try
            {
                var tiket = await _context.Tikets.FindAsync(tiketId);
                if (tiket == null)
                {
                    return (false, "Tiket tidak ditemukan");
                }

                _context.Tikets.Remove(tiket);
                await _context.SaveChangesAsync();

                return (true, "Tiket berhasil dihapus");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
        public async Task<(bool success, string message)> UpdateTiketAsync(Tiket tiket)
        {
            try
            {
                var existingTiket = await _context.Tikets.FindAsync(tiket.tiket_id);
                if (existingTiket == null)
                {
                    return (false, "Tiket tidak ditemukan");
                }
                existingTiket.status_tiket = tiket.status_tiket;
                existingTiket.plat_nomor = tiket.plat_nomor;
                existingTiket.jenis_kendaraan_enum = tiket.jenis_kendaraan_enum;
                existingTiket.total_harga = tiket.total_harga;

                await _context.SaveChangesAsync();
                return (true, "Tiket berhasil diperbarui");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
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

        // Get tikets by filter
        public async Task<List<Tiket>> GetTiketsByFilterAsync(
            string? searchKode = null,
            int? jadwalId = null,
            int? userId = null,
            string? status = null,
            DateTime? tanggal = null)
        {
            var query = _context.Tikets
                .Include(t => t.Pengguna)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.pelabuhan_asal)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.pelabuhan_tujuan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchKode))
            {
                query = query.Where(t => t.kode_tiket.ToLower().Contains(searchKode.ToLower()));
            }

            if (jadwalId.HasValue && jadwalId > 0)
            {
                query = query.Where(t => t.jadwal_id == jadwalId);
            }

            if (userId.HasValue && userId > 0)
            {
                query = query.Where(t => t.pengguna_id == userId);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "Semua Status")
            {
                query = query.Where(t => t.status_tiket == status);
            }

            if (tanggal.HasValue)
            {
                var date = tanggal.Value.Date;
                query = query.Where(t => t.tanggal_pemesanan.Date == date);
            }

            return await query
                .OrderByDescending(t => t.tanggal_pemesanan)
                .ToListAsync();
        }
    }
}
