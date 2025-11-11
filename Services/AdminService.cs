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
        public AdminService()
        {
            // No longer need to initialize _context field
        }
        public async Task<Admin?> ValidateAdminLoginAsync(string email, string password)
        {
            try
            {
                using var context = DatabaseService.GetContext();
                return await context.Admins
                    .FirstOrDefaultAsync(a => a.email == email && a.password == password);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Admin login error: {ex.Message}");
                return null;
            }
        }
        public async Task<Admin?> GetAdminByIdAsync(int adminId)
        {
            using var context = DatabaseService.GetContext();
            return await context.Admins.FirstOrDefaultAsync(a => a.admin_id == adminId);
        }
        public async Task<List<Admin>> GetAllAdminsAsync()
        {
            using var context = DatabaseService.GetContext();
            return await context.Admins
                .OrderBy(a => a.nama)
                .ToListAsync();
        }
        public async Task<Admin?> CreateAdminAsync(Admin admin)
        {
            try
            {
                using var context = DatabaseService.GetContext();
                
                // Cek apakah email sudah ada (case insensitive)
                var existingEmail = await context.Admins
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
                while (await context.Admins.AnyAsync(a => a.username.ToLower() == admin.username.ToLower()))
                {
                    admin.username = $"{baseUsername}{counter}";
                    counter++;
                    System.Diagnostics.Debug.WriteLine($"Username conflict, trying: {admin.username}");
                }
                admin.created_at = DateTime.Now;
                admin.updated_at = DateTime.Now;

                context.Admins.Add(admin);
                await context.SaveChangesAsync();

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
        public async Task<bool> UpdateAdminAsync(Admin admin)
        {
            try
            {
                using var context = DatabaseService.GetContext();
                
                var existing = await context.Admins.FindAsync(admin.admin_id);
                if (existing == null)
                {
                    return false;
                }
                existing.nama = admin.nama;
                existing.email = admin.email;
                existing.role = admin.role;
                existing.updated_at = DateTime.Now;
                if (!string.IsNullOrEmpty(admin.password))
                {
                    existing.password = admin.password;
                }
                if (!string.IsNullOrWhiteSpace(admin.username))
                {
                    existing.username = admin.username;
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update admin error: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> DeleteAdminAsync(int adminId)
        {
            try
            {
                using var context = DatabaseService.GetContext();
                
                var admin = await context.Admins.FindAsync(adminId);
                if (admin == null)
                {
                    return false;
                }

                context.Admins.Remove(admin);
                await context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete admin error: {ex.Message}");
                return false;
            }
        }
        public async Task<(bool success, string message)> CreateAdminWithValidationAsync(Admin admin, Admin currentAdmin)
        {
            try
            {
                // Validasi: hanya SuperAdmin yang bisa buat admin baru
                if (!currentAdmin.canCreateAdmin())
                {
                    return (false, "Anda tidak memiliki akses untuk membuat admin baru!");
                }

                using var context = DatabaseService.GetContext();
                
                // Cek apakah email atau username sudah ada
                var existingEmail = await context.Admins.AnyAsync(a => a.email == admin.email);
                if (existingEmail)
                {
                    return (false, "Email sudah digunakan!");
                }

                var existingUsername = await context.Admins.AnyAsync(a => a.username == admin.username);
                if (existingUsername)
                {
                    return (false, "Username sudah digunakan!");
                }

                context.Admins.Add(admin);
                await context.SaveChangesAsync();

                return (true, "Admin baru berhasil dibuat!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
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

                using var context = DatabaseService.GetContext();
                
                var admin = await context.Admins.FindAsync(adminId);
                if (admin == null)
                {
                    return (false, "Admin tidak ditemukan!");
                }

                context.Admins.Remove(admin);
                await context.SaveChangesAsync();

                return (true, "Admin berhasil dihapus!");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
        public async Task<AdminDashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                using var context = DatabaseService.GetContext();
                
                // Get counts - parallel execution for better performance
                var totalPengguna = await context.Penggunas.CountAsync();
                var totalTiket = await context.Tikets.CountAsync();
                var totalJadwal = await context.Jadwals.Where(j => j.status == "Active").CountAsync();
                var totalKapal = await context.Kapals.CountAsync();
                var totalPelabuhan = await context.Pelabuhans.CountAsync();

                var tiketMenunggu = await context.Tikets
                    .CountAsync(t => t.status_tiket == "Menunggu Pembayaran");
                
                var tiketSukses = await context.Tikets
                    .CountAsync(t => t.status_tiket == "Aktif");

                var pembayaranMenunggu = await context.Pembayarans
                    .CountAsync(p => p.status_bayar == "Menunggu Validasi");

                // Get pendapatan - Use UTC for PostgreSQL compatibility
                var today = DateTime.UtcNow.Date;
                var pendapatanHariIni = await context.Pembayarans
                    .Where(p => (p.status_bayar == "Sukses" || p.status_bayar == "Selesai") && p.tanggal_bayar.Date == today)
                    .SumAsync(p => (decimal?)p.jumlah_bayar) ?? 0;

                var currentMonth = DateTime.UtcNow.Month;
                var currentYear = DateTime.UtcNow.Year;
                var pendapatanBulanIni = await context.Pembayarans
                    .Where(p => (p.status_bayar == "Sukses" || p.status_bayar == "Selesai") &&
                               p.tanggal_bayar.Month == currentMonth &&
                               p.tanggal_bayar.Year == currentYear)
                    .SumAsync(p => (decimal?)p.jumlah_bayar) ?? 0;
                
                // NEW: Get additional insights
                var penggunaBaru7Hari = await context.Penggunas
                    .Where(p => p.tanggal_daftar >= DateTime.UtcNow.AddDays(-7))
                    .CountAsync();
                
                var tiketHariIni = await context.Tikets
                    .Where(t => t.tanggal_pemesanan.Date == today)
                    .CountAsync();
                
                var jadwalMingguDepan = await context.Jadwals
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
        public async Task<List<PendapatanPerRuteKapal>> GetPendapatanPerRuteKapalAsync(int bulan, int tahun)
        {
            try
            {
                using var context = DatabaseService.GetContext();
                
                var pendapatanDetail = await context.Pembayarans
                    .Where(p => (p.status_bayar == "Sukses" || p.status_bayar == "Selesai") &&
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
        public async Task<List<PendapatanPerRuteKapal>> GetPendapatanPerRuteKapalAsync()
        {
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            return await GetPendapatanPerRuteKapalAsync(currentMonth, currentYear);
        }
    }
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
    public class PendapatanPerRuteKapal
    {
        public string PelabuhanAsal { get; set; } = string.Empty;
        public string PelabuhanTujuan { get; set; } = string.Empty;
        public string NamaKapal { get; set; } = string.Empty;
        public decimal TotalPendapatan { get; set; }
        public int JumlahTiket { get; set; }
    }
}

