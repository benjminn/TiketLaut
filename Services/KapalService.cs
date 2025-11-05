using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    public class KapalService
    {
        private readonly AppDbContext _context;

        public KapalService()
        {
            _context = DatabaseService.GetContext();
        }

        public async Task<List<Kapal>> GetAllKapalAsync()
        {
            return await _context.Kapals
                .OrderBy(k => k.nama_kapal)
                .ToListAsync();
        }

        public async Task<Kapal?> GetKapalByIdAsync(int kapalId)
        {
            return await _context.Kapals.FindAsync(kapalId);
        }

        public async Task<(bool success, string message)> CreateKapalAsync(Kapal kapal)
        {
            try
            {
                var exists = await _context.Kapals.AnyAsync(k => k.nama_kapal == kapal.nama_kapal);
                if (exists)
                {
                    return (false, "Nama kapal sudah ada!");
                }

                _context.Kapals.Add(kapal);
                await _context.SaveChangesAsync();
                return (true, "Kapal berhasil ditambahkan!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> UpdateKapalAsync(Kapal kapal)
        {
            try
            {
                var existing = await _context.Kapals.FindAsync(kapal.kapal_id);
                if (existing == null)
                {
                    return (false, "Kapal tidak ditemukan!");
                }

                existing.nama_kapal = kapal.nama_kapal;
                existing.kapasitas_penumpang_max = kapal.kapasitas_penumpang_max;
                existing.kapasitas_kendaraan_max = kapal.kapasitas_kendaraan_max;
                existing.fasilitas = kapal.fasilitas;
                existing.deskripsi = kapal.deskripsi;

                await _context.SaveChangesAsync();
                return (true, "Kapal berhasil diupdate!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeleteKapalAsync(int kapalId)
        {
            try
            {
                // Cek apakah kapal sedang digunakan di jadwal
                var isUsed = await _context.Jadwals.AnyAsync(j => j.kapal_id == kapalId);
                if (isUsed)
                {
                    return (false, "Kapal tidak dapat dihapus karena masih digunakan dalam jadwal!");
                }

                var kapal = await _context.Kapals.FindAsync(kapalId);
                if (kapal == null)
                {
                    return (false, "Kapal tidak ditemukan!");
                }

                _context.Kapals.Remove(kapal);
                await _context.SaveChangesAsync();
                return (true, "Kapal berhasil dihapus!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
    }
}
