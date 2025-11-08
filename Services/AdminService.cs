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
        /// Create new admin - simplified version
        /// </summary>
        public async Task<Admin?> CreateAdminAsync(Admin admin)
        {
            try
            {
                // Cek apakah email sudah ada (case insensitive)
                var existingEmail = await _context.Admins
                    .AnyAsync(a => a.email.ToLower() == admin.email.ToLower());
                if (existingEmail)
                {
                    System.Diagnostics.Debug.WriteLine($"Email sudah terdaftar: {admin.email}");
                    return null;
                }

                // Generate username dari email jika tidak ada
                if (string.IsNullOrWhiteSpace(admin.username))
                {
                    admin.username = admin.email.Split('@')[0];
                }

                // Cek apakah username sudah ada (case insensitive)
                var baseUsername = admin.username;
                var counter = 1;
                while (await _context.Admins.AnyAsync(a => a.username.ToLower() == admin.username.ToLower()))
                {
                    admin.username = $"{baseUsername}{counter}";
                    counter++;
                    System.Diagnostics.Debug.WriteLine($"Username conflict, trying: {admin.username}");
                }

                // Set default values
                admin.created_at = DateTime.Now;
                admin.updated_at = DateTime.Now;

                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Admin created successfully: {admin.email}");
                return admin;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Create admin error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Update admin - simplified version
        /// </summary>
        public async Task<bool> UpdateAdminAsync(Admin admin)
        {
            try
            {
                var existing = await _context.Admins.FindAsync(admin.admin_id);
                if (existing == null)
                {
                    return false;
                }

                // Update fields
                existing.nama = admin.nama;
                existing.email = admin.email;
                existing.role = admin.role;
                existing.updated_at = DateTime.Now;

                // Update password hanya jika diisi
                if (!string.IsNullOrEmpty(admin.password))
                {
                    existing.password = admin.password;
                }

                // Update username jika ada
                if (!string.IsNullOrWhiteSpace(admin.username))
                {
                    existing.username = admin.username;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update admin error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete admin - simplified version
        /// </summary>
        public async Task<bool> DeleteAdminAsync(int adminId)
        {
            try
            {
                var admin = await _context.Admins.FindAsync(adminId);
                if (admin == null)
                {
                    return false;
                }

                _context.Admins.Remove(admin);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete admin error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create new admin (hanya SuperAdmin yang bisa) - with validation
        /// </summary>
        public async Task<(bool success, string message)> CreateAdminWithValidationAsync(Admin admin, Admin currentAdmin)
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
        /// Delete admin (hanya SuperAdmin yang bisa) - with validation
        /// </summary>
        public async Task<(bool success, string message)> DeleteAdminWithValidationAsync(int adminId, Admin currentAdmin)
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
                // Get counts - parallel execution for better performance
                var totalPengguna = await _context.Penggunas.CountAsync();
                var totalTiket = await _context.Tikets.CountAsync();
                var totalJadwal = await _context.Jadwals.Where(j => j.status == "Active").CountAsync();
                var totalKapal = await _context.Kapals.CountAsync();
                var totalPelabuhan = await _context.Pelabuhans.CountAsync();

                var tiketMenunggu = await _context.Tikets
                    .CountAsync(t => t.status_tiket == "Menunggu Pembayaran");
                
                var tiketSukses = await _context.Tikets
                    .CountAsync(t => t.status_tiket == "Aktif");

                var pembayaranMenunggu = await _context.Pembayarans
                    .CountAsync(p => p.status_bayar == "Menunggu Validasi");

                // Get pendapatan - Use UTC for PostgreSQL compatibility
                var today = DateTime.UtcNow.Date;
                var pendapatanHariIni = await _context.Pembayarans
                    .Where(p => p.status_bayar == "Sukses" && p.tanggal_bayar.Date == today)
                    .SumAsync(p => (decimal?)p.jumlah_bayar) ?? 0;

                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                var pendapatanBulanIni = await _context.Pembayarans
                    .Where(p => p.status_bayar == "Sukses" &&
                               p.tanggal_bayar.Month == currentMonth &&
                               p.tanggal_bayar.Year == currentYear)
                    .SumAsync(p => (decimal?)p.jumlah_bayar) ?? 0;
                
                // NEW: Get additional insights
                var penggunaBaru7Hari = await _context.Penggunas
                    .Where(p => p.tanggal_daftar >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync();
                
                var tiketHariIni = await _context.Tikets
                    .Where(t => t.tanggal_pemesanan.Date == today)
                    .CountAsync();
                
                var jadwalMingguDepan = await _context.Jadwals
                    .Where(j => j.status == "Active" && 
                               j.waktu_berangkat >= DateTime.UtcNow &&
                               j.waktu_berangkat <= DateTime.UtcNow.AddDays(7))
                    .CountAsync();
                
                var rataRataPendapatanPerHari = pendapatanBulanIni > 0 && currentMonth > 0
                    ? pendapatanBulanIni / DateTime.UtcNow.Day
                    : 0;

                var stats = new AdminDashboardStats
                {
                    TotalPengguna = totalPengguna,
                    TotalTiket = totalTiket,
                    TotalJadwal = totalJadwal,
                    TotalKapal = totalKapal,
                    TotalPelabuhan = totalPelabuhan,
                    TiketMenungguPembayaran = tiketMenunggu,
                    TiketSukses = tiketSukses,
                    PembayaranMenungguKonfirmasi = pembayaranMenunggu,
                    TotalPendapatanHariIni = pendapatanHariIni,
                    TotalPendapatanBulanIni = pendapatanBulanIni,
                    // New insights
                    PenggunaBaru7Hari = penggunaBaru7Hari,
                    TiketHariIni = tiketHariIni,
                    JadwalMingguDepan = jadwalMingguDepan,
                    RataRataPendapatanPerHari = rataRataPendapatanPerHari
                };

                return stats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminService] Error: {ex.Message}");
                // Return empty stats instead of throwing
                return new AdminDashboardStats();
            }
        }

        /// <summary>
        /// Get detail pendapatan per rute & kapal untuk bulan tertentu
        /// </summary>
        public async Task<List<PendapatanPerRuteKapal>> GetPendapatanPerRuteKapalAsync(int bulan, int tahun)
        {
            try
            {
                var pendapatanDetail = await _context.Pembayarans
                    .Where(p => p.status_bayar == "Sukses" &&
                               p.tanggal_bayar.Month == bulan &&
                               p.tanggal_bayar.Year == tahun)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                        .ThenInclude(j => j.pelabuhan_asal)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                        .ThenInclude(j => j.pelabuhan_tujuan)
                    .Include(p => p.tiket)
                        .ThenInclude(t => t.Jadwal)
                        .ThenInclude(j => j.kapal)
                    .ToListAsync();

                var grouped = pendapatanDetail
                    .Where(p => p.tiket != null && 
                               p.tiket.Jadwal != null && 
                               p.tiket.Jadwal.pelabuhan_asal != null &&
                               p.tiket.Jadwal.pelabuhan_tujuan != null &&
                               p.tiket.Jadwal.kapal != null)
                    .GroupBy(p => new
                    {
                        Asal = p.tiket.Jadwal.pelabuhan_asal.nama_pelabuhan,
                        Tujuan = p.tiket.Jadwal.pelabuhan_tujuan.nama_pelabuhan,
                        Kapal = p.tiket.Jadwal.kapal.nama_kapal
                    })
                    .Select(g => new PendapatanPerRuteKapal
                    {
                        PelabuhanAsal = g.Key.Asal,
                        PelabuhanTujuan = g.Key.Tujuan,
                        NamaKapal = g.Key.Kapal,
                        TotalPendapatan = g.Sum(p => p.jumlah_bayar),
                        JumlahTiket = g.Count()
                    })
                    .OrderByDescending(p => p.TotalPendapatan)
                    .ToList();

                return grouped;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdminService] Error GetPendapatanPerRuteKapal: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[AdminService] StackTrace: {ex.StackTrace}");
                return new List<PendapatanPerRuteKapal>();
            }
        }

        /// <summary>
        /// Get detail pendapatan per rute & kapal untuk bulan ini (default)
        /// </summary>
        public async Task<List<PendapatanPerRuteKapal>> GetPendapatanPerRuteKapalAsync()
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            return await GetPendapatanPerRuteKapalAsync(currentMonth, currentYear);
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
        
        // New insights
        public int PenggunaBaru7Hari { get; set; }
        public int TiketHariIni { get; set; }
        public int JadwalMingguDepan { get; set; }
        public decimal RataRataPendapatanPerHari { get; set; }
    }

    /// <summary>
    /// DTO untuk pendapatan per rute & kapal
    /// </summary>
    public class PendapatanPerRuteKapal
    {
        public string PelabuhanAsal { get; set; } = string.Empty;
        public string PelabuhanTujuan { get; set; } = string.Empty;
        public string NamaKapal { get; set; } = string.Empty;
        public decimal TotalPendapatan { get; set; }
        public int JumlahTiket { get; set; }
    }
}

