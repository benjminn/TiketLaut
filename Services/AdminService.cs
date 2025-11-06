using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    public class AdminService
    {
        private readonly AppDbContext _context;

        public AdminService()
        {
            _context = DatabaseService.GetContext();
        }

        /// <summary>
        /// Validate admin login (cek di tabel Admin)
        /// </summary>
        public async Task<Admin?> ValidateAdminLoginAsync(string email, string password)
        {
            try
            {
                return await _context.Admins
                    .FirstOrDefaultAsync(a => a.email == email && a.password == password);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Admin login error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get admin by ID
        /// </summary>
        public async Task<Admin?> GetAdminByIdAsync(int adminId)
        {
            return await _context.Admins.FirstOrDefaultAsync(a => a.admin_id == adminId);
        }

        /// <summary>
        /// Get all admins (untuk SuperAdmin)
        /// </summary>
        public async Task<List<Admin>> GetAllAdminsAsync()
        {
            return await _context.Admins
                .OrderBy(a => a.nama)
                .ToListAsync();
        }

        /// <summary>
        /// Create new admin (hanya SuperAdmin yang bisa)
        /// </summary>
        public async Task<(bool success, string message)> CreateAdminAsync(Admin admin, Admin currentAdmin)
        {
            try
            {
                // Validasi: hanya SuperAdmin yang bisa buat admin baru
                if (!currentAdmin.canCreateAdmin())
                {
                    return (false, "Anda tidak memiliki akses untuk membuat admin baru!");
                }

                // Cek apakah email atau username sudah ada
                var existingEmail = await _context.Admins.AnyAsync(a => a.email == admin.email);
                if (existingEmail)
                {
                    return (false, "Email sudah digunakan!");
                }

                var existingUsername = await _context.Admins.AnyAsync(a => a.username == admin.username);
                if (existingUsername)
                {
                    return (false, "Username sudah digunakan!");
                }

                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();

                return (true, "Admin baru berhasil dibuat!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Update admin
        /// </summary>
        public async Task<(bool success, string message)> UpdateAdminAsync(Admin admin)
        {
            try
            {
                var existing = await _context.Admins.FindAsync(admin.admin_id);
                if (existing == null)
                {
                    return (false, "Admin tidak ditemukan!");
                }

                // Update fields
                existing.nama = admin.nama;
                existing.username = admin.username;
                existing.email = admin.email;
                existing.role = admin.role;

                // Update password hanya jika diisi
                if (!string.IsNullOrEmpty(admin.password))
                {
                    existing.password = admin.password;
                }

                await _context.SaveChangesAsync();
                return (true, "Admin berhasil diupdate!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete admin (hanya SuperAdmin yang bisa)
        /// </summary>
        public async Task<(bool success, string message)> DeleteAdminAsync(int adminId, Admin currentAdmin)
        {
            try
            {
                // Validasi: hanya SuperAdmin yang bisa delete admin
                if (!currentAdmin.canCreateAdmin())
                {
                    return (false, "Anda tidak memiliki akses untuk menghapus admin!");
                }

                // Tidak bisa hapus diri sendiri
                if (adminId == currentAdmin.admin_id)
                {
                    return (false, "Tidak dapat menghapus akun sendiri!");
                }

                var admin = await _context.Admins.FindAsync(adminId);
                if (admin == null)
                {
                    return (false, "Admin tidak ditemukan!");
                }

                _context.Admins.Remove(admin);
                await _context.SaveChangesAsync();

                return (true, "Admin berhasil dihapus!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get statistics untuk dashboard
        /// </summary>
        public async Task<AdminDashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                var stats = new AdminDashboardStats
                {
                    TotalPengguna = await _context.Penggunas.CountAsync(),
                    TotalTiket = await _context.Tikets.CountAsync(),
                    TotalJadwal = await _context.Jadwals.Where(j => j.status == "Active").CountAsync(),
                    TotalKapal = await _context.Kapals.CountAsync(),
                    TotalPelabuhan = await _context.Pelabuhans.CountAsync(),

                    TiketMenungguPembayaran = await _context.Tikets
                        .CountAsync(t => t.status_tiket == "Menunggu Pembayaran"),

                    TiketSukses = await _context.Tikets
                        .CountAsync(t => t.status_tiket == "Aktif"),

                    PembayaranMenungguKonfirmasi = await _context.Pembayarans
                    .CountAsync(p => p.status_bayar == "Menunggu Validasi"),

                    // ? FIXED: Use enum instead of string
                    TotalPendapatanHariIni = await _context.Pembayarans
                    .Where(p => p.status_bayar == "Sukses" &&
                               p.tanggal_bayar.Date == DateTime.Today)
                    .SumAsync(p => (decimal?)p.jumlah_bayar) ?? 0,

                    // ? FIXED: Use enum instead of string  
                    TotalPendapatanBulanIni = await _context.Pembayarans
                    .Where(p => p.status_bayar == "Sukses" &&
                               p.tanggal_bayar.Month == DateTime.Now.Month &&
                               p.tanggal_bayar.Year == DateTime.Now.Year)
                    .SumAsync(p => (decimal?)p.jumlah_bayar) ?? 0


                };

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting stats: {ex.Message}");
                return new AdminDashboardStats();
            }
        }
    }

    /// <summary>
    /// DTO untuk statistik dashboard
    /// </summary>
    public class AdminDashboardStats
    {
        public int TotalPengguna { get; set; }
        public int TotalTiket { get; set; }
        public int TotalJadwal { get; set; }
        public int TotalKapal { get; set; }
        public int TotalPelabuhan { get; set; }
        public int TiketMenungguPembayaran { get; set; }
        public int TiketSukses { get; set; }
        public int PembayaranMenungguKonfirmasi { get; set; }
        public decimal TotalPendapatanHariIni { get; set; }
        public decimal TotalPendapatanBulanIni { get; set; }
    }
}

