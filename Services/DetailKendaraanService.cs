using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    public class DetailKendaraanService
    {
        private readonly AppDbContext _context;

        public DetailKendaraanService()
        {
            _context = DatabaseService.GetContext();
        }

        /// <summary>
        /// Get all detail kendaraan
        /// </summary>
        public async Task<List<DetailKendaraan>> GetAllDetailKendaraanAsync()
        {
            try
            {
                return await _context.DetailKendaraans
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
        /// Get detail kendaraan by ID
        /// </summary>
        public async Task<DetailKendaraan?> GetDetailKendaraanByIdAsync(int detailKendaraanId)
        {
            try
            {
                return await _context.DetailKendaraans
                    .FirstOrDefaultAsync(dk => dk.detail_kendaraan_id == detailKendaraanId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting detail kendaraan: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get detail kendaraan by jenis
        /// </summary>
        public async Task<List<DetailKendaraan>> GetDetailKendaraanByJenisAsync(JenisKendaraan jenis)
        {
            try
            {
                return await _context.DetailKendaraans
                    .Where(dk => dk.jenis_kendaraan == (int)jenis)
                    .OrderBy(dk => dk.harga_kendaraan)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting detail kendaraan by jenis: {ex.Message}");
                return new List<DetailKendaraan>();
            }
        }

        /// <summary>
        /// Create new detail kendaraan
        /// </summary>
        public async Task<(bool success, string message, int? id)> CreateDetailKendaraanAsync(DetailKendaraan detailKendaraan)
        {
            try
            {
                _context.DetailKendaraans.Add(detailKendaraan);
                await _context.SaveChangesAsync();
                return (true, "Detail kendaraan berhasil ditambahkan!", detailKendaraan.detail_kendaraan_id);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Update detail kendaraan
        /// </summary>
        public async Task<(bool success, string message)> UpdateDetailKendaraanAsync(DetailKendaraan detailKendaraan)
        {
            try
            {
                _context.DetailKendaraans.Update(detailKendaraan);
                await _context.SaveChangesAsync();
                return (true, "Detail kendaraan berhasil diupdate!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete detail kendaraan
        /// </summary>
        public async Task<(bool success, string message)> DeleteDetailKendaraanAsync(int detailKendaraanId)
        {
            try
            {
                // Check if detail kendaraan is part of a grup that is used by jadwal
                var detailKendaraan = await _context.DetailKendaraans
                    .Include(dk => dk.GrupKendaraan)
                    .ThenInclude(g => g!.Jadwals)
                    .FirstOrDefaultAsync(dk => dk.detail_kendaraan_id == detailKendaraanId);

                if (detailKendaraan?.GrupKendaraan?.Jadwals.Any() == true)
                {
                    return (false, "Detail kendaraan masih digunakan oleh jadwal melalui grup! Hapus atau update jadwal terlebih dahulu.");
                }

                if (detailKendaraan == null)
                {
                    return (false, "Detail kendaraan tidak ditemukan");
                }

                _context.DetailKendaraans.Remove(detailKendaraan);
                await _context.SaveChangesAsync();
                return (true, "Detail kendaraan berhasil dihapus!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Find or create detail kendaraan by jenis and harga (untuk reusability)
        /// </summary>
        public async Task<(bool success, string message, int? detailKendaraanId)> FindOrCreateDetailKendaraanAsync(
            JenisKendaraan jenis, 
            decimal harga)
        {
            try
            {
                // Try to find existing with same jenis and harga
                var existing = await _context.DetailKendaraans
                    .FirstOrDefaultAsync(dk => 
                        dk.jenis_kendaraan == (int)jenis && 
                        dk.harga_kendaraan == harga);

                if (existing != null)
                {
                    return (true, "Menggunakan detail kendaraan yang sudah ada", existing.detail_kendaraan_id);
                }

                // Create new
                var newDetail = DetailKendaraan.Create(jenis, harga);
                var result = await CreateDetailKendaraanAsync(newDetail);
                return result;
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Assign detail kendaraan to jadwal (DEPRECATED - use GrupKendaraan instead)
        /// </summary>
        [Obsolete("Use GrupKendaraanService instead. Jadwal now uses grup_kendaraan_id.")]
        public async Task<(bool success, string message)> AssignDetailKendaraanToJadwalAsync(
            int jadwalId, 
            int detailKendaraanId)
        {
            return await Task.FromResult((false, "Method deprecated. Use GrupKendaraanService instead."));
        }

        /// <summary>
        /// Remove detail kendaraan from jadwal (DEPRECATED - use GrupKendaraan instead)
        /// </summary>
        [Obsolete("Use GrupKendaraanService instead. Jadwal now uses grup_kendaraan_id.")]
        public async Task<(bool success, string message)> RemoveDetailKendaraanFromJadwalAsync(int jadwalId)
        {
            return await Task.FromResult((false, "Method deprecated. Use GrupKendaraanService instead."));
        }
    }
}
