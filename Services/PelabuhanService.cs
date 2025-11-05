using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    public class PelabuhanService
    {
        private readonly AppDbContext _context;

        public PelabuhanService()
        {
            _context = DatabaseService.GetContext();
        }

        public async Task<List<Pelabuhan>> GetAllPelabuhanAsync()
        {
            return await _context.Pelabuhans
                .OrderBy(p => p.nama_pelabuhan)
                .ToListAsync();
        }

        public async Task<Pelabuhan?> GetPelabuhanByIdAsync(int pelabuhanId)
        {
            return await _context.Pelabuhans.FindAsync(pelabuhanId);
        }

        public async Task<(bool success, string message)> CreatePelabuhanAsync(Pelabuhan pelabuhan)
        {
            try
            {
                var exists = await _context.Pelabuhans.AnyAsync(p => p.nama_pelabuhan == pelabuhan.nama_pelabuhan);
                if (exists)
                {
                    return (false, "Nama pelabuhan sudah ada!");
                }

                _context.Pelabuhans.Add(pelabuhan);
                await _context.SaveChangesAsync();
                return (true, "Pelabuhan berhasil ditambahkan!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> UpdatePelabuhanAsync(Pelabuhan pelabuhan)
        {
            try
            {
                var existing = await _context.Pelabuhans.FindAsync(pelabuhan.pelabuhan_id);
                if (existing == null)
                {
                    return (false, "Pelabuhan tidak ditemukan!");
                }

                existing.nama_pelabuhan = pelabuhan.nama_pelabuhan;
                existing.kota = pelabuhan.kota;
                existing.provinsi = pelabuhan.provinsi;
                existing.fasilitas = pelabuhan.fasilitas;
                existing.deskripsi = pelabuhan.deskripsi;

                await _context.SaveChangesAsync();
                return (true, "Pelabuhan berhasil diupdate!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> DeletePelabuhanAsync(int pelabuhanId)
        {
            try
            {
                // Cek apakah pelabuhan sedang digunakan di jadwal
                var isUsed = await _context.Jadwals.AnyAsync(j => 
                    j.pelabuhan_asal_id == pelabuhanId || j.pelabuhan_tujuan_id == pelabuhanId);
                
                if (isUsed)
                {
                    return (false, "Pelabuhan tidak dapat dihapus karena masih digunakan dalam jadwal!");
                }

                var pelabuhan = await _context.Pelabuhans.FindAsync(pelabuhanId);
                if (pelabuhan == null)
                {
                    return (false, "Pelabuhan tidak ditemukan!");
                }

                _context.Pelabuhans.Remove(pelabuhan);
                await _context.SaveChangesAsync();
                return (true, "Pelabuhan berhasil dihapus!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
    }
}
