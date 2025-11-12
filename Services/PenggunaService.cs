using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Ambil semua pengguna
        /// </summary>
        public async Task<List<Pengguna>> GetAllAsync()
        {
            try
            {
                return await _context.Penggunas
                    .OrderBy(p => p.nama)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllAsync error: {ex.Message}");
                return new List<Pengguna>();
            }
        }

        /// <summary>
        /// Update profil pengguna lengkap
        /// </summary>
        public async Task<bool> UpdateProfile(
            int penggunaId,
            string nama,
            string email,
            string nik,
            string jenisKelamin,
            DateOnly tanggalLahir,
            string? alamat,
            string? newPassword = null)
        {
            try
            {
                var pengguna = await _context.Penggunas
                    .FirstOrDefaultAsync(p => p.pengguna_id == penggunaId);

                if (pengguna == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[PenggunaService] User not found: ID {penggunaId}");
                    return false;
                }

                // Cek apakah email baru sudah digunakan user lain
                if (pengguna.email != email)
                {
                    var emailExists = await _context.Penggunas
                        .AnyAsync(p => p.email == email && p.pengguna_id != penggunaId);
                    
                    if (emailExists)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PenggunaService] Email already in use: {email}");
                        return false;
                    }
                }

                // Cek apakah NIK baru sudah digunakan user lain
                if (pengguna.nomor_induk_kependudukan != nik)
                {
                    var nikExists = await _context.Penggunas
                        .AnyAsync(p => p.nomor_induk_kependudukan == nik && p.pengguna_id != penggunaId);
                    
                    if (nikExists)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PenggunaService] NIK already in use: {nik}");
                        return false;
                    }
                }

                // Update data
                pengguna.nama = nama;
                pengguna.email = email;
                pengguna.nomor_induk_kependudukan = nik;
                pengguna.jenis_kelamin = jenisKelamin;
                pengguna.tanggal_lahir = tanggalLahir;
                pengguna.alamat = string.IsNullOrWhiteSpace(alamat) ? null : alamat;

                // Update password jika diisi
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    pengguna.password = newPassword;
                    System.Diagnostics.Debug.WriteLine($"[PenggunaService] Password updated for user ID {penggunaId}");
                }

                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[PenggunaService] Profile updated successfully for user ID {penggunaId}");
                return true;
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                System.Diagnostics.Debug.WriteLine($"[PenggunaService] UpdateProfile DB error: {innerMessage}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PenggunaService] UpdateProfile error: {ex.Message}");
                return false;
            }
        }
    }
}