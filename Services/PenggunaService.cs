using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    public class PenggunaService
    {
        private readonly AppDbContext _context;

        public PenggunaService()
        {
            _context = DatabaseService.GetContext();
        }

        public async Task<Pengguna?> ValidateLoginAsync(string email, string password)
        {
            try
            {
                return await _context.Penggunas
                    .FirstOrDefaultAsync(p => p.email == email && p.password == password);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                return null;
            }
        }

        public async Task<(bool success, string message)> RegisterAsync(Pengguna pengguna)
        {
            try
            {
                var exists = await _context.Penggunas.AnyAsync(p => p.email == pengguna.email);
                if (exists) return (false, "Email sudah terdaftar!");

                _context.Penggunas.Add(pengguna);
                await _context.SaveChangesAsync();
                return (true, "Registrasi berhasil!");
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return (false, $"Error database: {innerMessage}");
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }

        public async Task<Pengguna?> GetByIdAsync(int penggunaId)
        {
            return await _context.Penggunas.FirstOrDefaultAsync(p => p.pengguna_id == penggunaId);
        }
    }
}