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

        /// <summary>
        /// Cek apakah user dengan Google email sudah terdaftar
        /// </summary>
        public async Task<Pengguna?> GetByGoogleEmailAsync(string googleEmail)
        {
            try
            {
                return await _context.Penggunas
                    .FirstOrDefaultAsync(p => p.email == googleEmail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetByGoogleEmailAsync error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Register user baru dari Google OAuth dengan info tambahan
        /// Password di-set sebagai "GOOGLE_OAUTH" karena login via Google tidak pakai password
        /// </summary>
        public async Task<(bool success, string message, Pengguna? pengguna)> RegisterGoogleUserAsync(
            string googleEmail,
            string namaLengkap,
            string nik,
            DateOnly tanggalLahir)
        {
            try
            {
                // Cek apakah email sudah terdaftar
                var exists = await _context.Penggunas.AnyAsync(p => p.email == googleEmail);
                if (exists)
                {
                    return (false, "Email sudah terdaftar!", null);
                }

                // Cek apakah NIK sudah terdaftar
                var nikExists = await _context.Penggunas.AnyAsync(p => p.nomor_induk_kependudukan == nik);
                if (nikExists)
                {
                    return (false, "NIK sudah terdaftar!", null);
                }

                // Buat user baru dengan default values
                var pengguna = new Pengguna
                {
                    nama = namaLengkap,
                    email = googleEmail,
                    password = "GOOGLE_OAUTH", // Marker untuk Google OAuth user
                    nomor_induk_kependudukan = nik,
                    tanggal_lahir = tanggalLahir,
                    jenis_kelamin = "Laki-laki", // Default - bisa diupdate di profil (max 10 char: "Perempuan" = 9, "Laki-laki" = 9)
                    kewarganegaraan = "Indonesia", // Default - bisa diupdate di profil
                    alamat = null, // Optional - bisa diupdate di profil
                    tanggal_daftar = DateTime.UtcNow
                };

                _context.Penggunas.Add(pengguna);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[PenggunaService] Google user registered: {googleEmail}");
                return (true, "Registrasi berhasil!", pengguna);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                System.Diagnostics.Debug.WriteLine($"[PenggunaService] RegisterGoogleUserAsync DB error: {innerMessage}");
                return (false, $"Error database: {innerMessage}", null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PenggunaService] RegisterGoogleUserAsync error: {ex.Message}");
                return (false, $"Terjadi kesalahan: {ex.Message}", null);
            }
        }
    }
}