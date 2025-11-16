using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TiketLaut.Data;
using TiketLaut.Models;
using System.Windows.Threading;

namespace TiketLaut.Services
{
    public class NotifikasiService
    {
        private readonly AppDbContext _context;

        public NotifikasiService()
        {
            _context = DatabaseService.GetContext();
        }
        public async Task<List<Notifikasi>> GetNotifikasiByPenggunaIdAsync(int penggunaId)
        {
            return await _context.Notifikasis
                .Where(n => n.pengguna_id == penggunaId)
                .OrderByDescending(n => n.waktu_kirim)
                .Include(n => n.Pengguna)
                .Include(n => n.Admin)
                .Include(n => n.Jadwal)
                .ToListAsync();
        }

        public async Task<List<Notifikasi>> GetAllNotifikasiAsync()
        {
            return await _context.Notifikasis
                .OrderByDescending(n => n.waktu_kirim)
                .Include(n => n.Pengguna)
                .Include(n => n.Admin)
                .Include(n => n.Jadwal!)
                    .ThenInclude(j => j.pelabuhan_asal)
                .Include(n => n.Jadwal!)
                    .ThenInclude(j => j.pelabuhan_tujuan)
                .Include(n => n.Jadwal!)
                    .ThenInclude(j => j.kapal)
                .ToListAsync();
        }

        public async Task<List<Notifikasi>> GetUnreadNotifikasiAsync(int penggunaId)
        {
            return await _context.Notifikasis
                .Where(n => n.pengguna_id == penggunaId && !n.status_baca)
                .OrderByDescending(n => n.waktu_kirim)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int penggunaId)
        {
            return await _context.Notifikasis
                .CountAsync(n => n.pengguna_id == penggunaId && !n.status_baca);
        }

        public async Task<bool> MarkAsReadAsync(int notifikasiId)
        {
            var notif = await _context.Notifikasis.FindAsync(notifikasiId);

            if (notif == null)
                return false;

            notif.status_baca = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int penggunaId)
        {
            var unreadNotifs = await _context.Notifikasis
                .Where(n => n.pengguna_id == penggunaId && !n.status_baca)
                .ToListAsync();

            foreach (var notif in unreadNotifs)
            {
                notif.status_baca = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Notifikasi?> GetNotifikasiByIdAsync(int notifikasiId)
        {
            return await _context.Notifikasis
                .Include(n => n.Pengguna)
                .Include(n => n.Admin)
                .Include(n => n.Jadwal)
                .FirstOrDefaultAsync(n => n.notifikasi_id == notifikasiId);
        }

        public async Task<Notifikasi> CreateNotifikasiAsync(
            int penggunaId,
            string jenisNotifikasi,
            string judul,
            string pesan,
            bool olehSystem = false,
            int? adminId = null,
            int? jadwalId = null,
            int? pembayaranId = null, // Sudah ada
            int? tiketId = null) // Sudah ada
        {
            var notifikasi = new Notifikasi
            {
                pengguna_id = penggunaId,
                jenis_notifikasi = jenisNotifikasi,
                judul_notifikasi = judul,
                pesan = pesan,
                waktu_kirim = DateTime.UtcNow, // Gunakan DateTime.Now di sini
                status_baca = false,
                oleh_system = olehSystem,
                admin_id = adminId,
                jadwal_id = jadwalId,
                pembayaran_id = pembayaranId, // Sudah ada
                tiket_id = tiketId // Sudah ada
            };

            _context.Notifikasis.Add(notifikasi);
            await _context.SaveChangesAsync();

            return notifikasi;
        }

        public async Task<Notifikasi> SendKeberangkatanNotificationAsync(
    int penggunaId,
    string kapalNama,
    string ruteAsal,
    string ruteTujuan,
    DateTime waktuBerangkat,
    int? jadwalId = null,
    int? tiketId = null) // <-- TAMBAHKAN tiketId
        {
            var waktuBerangkatWIB = waktuBerangkat.AddHours(7);
            var tanggal = waktuBerangkatWIB.ToString("d MMMM yyyy");
            var jam = waktuBerangkatWIB.ToString("HH:mm");

            var judul = "Kapal Anda akan berangkat dalam 24 jam!";
            var pesan = $"⏰  {judul}\n\n" + // Tambah spasi
                          $"Kapal {kapalNama} jurusan {ruteAsal} - {ruteTujuan} akan berangkat besok, {tanggal} pukul {jam} WIB. " +
                          $"Pastikan Anda tiba di pelabuhan 1 jam sebelum keberangkatan";

            return await CreateNotifikasiAsync(
                penggunaId,
                "pengingat",
                judul,
                pesan,
                olehSystem: true,
                jadwalId: jadwalId,
                tiketId: tiketId); // <-- TAMBAHKAN tiketId
        }

        public async Task<Notifikasi> SendKeberangkatan2JamNotificationAsync(
            int penggunaId,
            string tiketKode,
            string kapalNama,
            string pelabuhanAsal,
            DateTime waktuBerangkat,
            int? jadwalId = null,
            int? tiketId = null) // <-- TAMBAHKAN tiketId
        {
            var waktuBerangkatWIB = waktuBerangkat.AddHours(7);
            var jam = waktuBerangkatWIB.ToString("HH:mm");

            var judul = "Kapal akan berangkat dalam 2 jam";
            var pesan = $"⏰  {judul}\n\n" + // Tambah spasi
                          $"Tiket #{tiketKode}. Kapal {kapalNama} berangkat pukul {jam} WIB dari Pelabuhan {pelabuhanAsal}. " +
                          $"Pastikan Anda tiba maksimal 30 menit sebelum keberangkatan!";

            return await CreateNotifikasiAsync(
                penggunaId,
                "pengingat",
                judul,
                pesan,
                olehSystem: true,
                jadwalId: jadwalId,
                tiketId: tiketId); // <-- TAMBAHKAN tiketId
        }

        public async Task<Notifikasi> SendPenundaanNotificationAsync(
            int penggunaId,
            string kapalNama,
            DateTime jadwalBaru,
            string alasanDelay,
            int? jadwalId = null)
        {
            var jam = jadwalBaru.ToString("HH:mm");

            var judul = "Jadwal keberangkatan ditunda";
            var pesan = $"⚠️  {judul}\n\n" +
                       $"Kapal {kapalNama} mengalami penundaan keberangkatan. " +
                       $"Jadwal baru: {jam} WIB ({alasanDelay}). Mohon maaf atas ketidaknyamanannya.";

            return await CreateNotifikasiAsync(
                penggunaId,
                "pemberitahuan",
                judul,
                pesan,
                olehSystem: false,
                jadwalId: jadwalId);
        }

        public async Task<Notifikasi> SendPembatalanNotificationAsync(
            int penggunaId,
            string kapalNama,
            string ruteAsal,
            string ruteTujuan,
            DateTime tanggalBerangkat,
            string alasan,
            int? jadwalId = null)
        {
            var tanggal = tanggalBerangkat.ToString("d MMMM yyyy");
            var jam = tanggalBerangkat.ToString("HH:mm");

            var judul = "Jadwal keberangkatan dibatalkan";
            var pesan = $"❌  {judul}\n\n" +
                       $"Jadwal keberangkatan kapal {kapalNama} jurusan {ruteAsal} - {ruteTujuan} " +
                       $"tanggal {tanggal} pukul {jam} WIB dibatalkan karena {alasan}. " +
                       $"Silakan hubungi customer service untuk refund atau reschedule.";

            return await CreateNotifikasiAsync(
                penggunaId,
                "pembatalan",
                judul,
                pesan,
                olehSystem: false,
                jadwalId: jadwalId);
        }

        public async Task<Notifikasi> SendPembayaranBerhasilNotificationAsync(
            int penggunaId,
            string tiketKode,
            string ruteAsal,
            string ruteTujuan,
            int? jadwalId = null,
            int? tiketId = null) // <-- TAMBAHKAN tiketId
        {
            var judul = "Pembayaran berhasil dikonfirmasi";
            var pesan = $"💳  {judul}\n\n" + // (Saya tambahkan spasi setelah emoji)
                          $"Pembayaran untuk tiket #{tiketKode} jurusan {ruteAsal} - {ruteTujuan} " +
                          $"telah divalidasi. Tiket Anda sudah aktif dan siap digunakan.";

            return await CreateNotifikasiAsync(
                penggunaId,
                "pembayaran",
                judul,
                pesan,
                olehSystem: true, // <-- REVISI DARI false MENJADI true
                jadwalId: jadwalId,
                tiketId: tiketId); // <-- TAMBAHKAN tiketId
        }

        public async Task<Notifikasi> SendMenungguValidasiNotificationAsync(
            int penggunaId,
            int? jadwalId = null,
            int? pembayaranId = null, // <-- TAMBAHKAN
            int? tiketId = null) // <-- TAMBAHKAN
        {
            var judul = "Menunggu validasi pembayaran";
            var pesan = $"💳  {judul}\n\n" + // (Saya tambahkan spasi setelah emoji)
                          $"Pembayaran Anda sedang diverifikasi oleh admin. " +
                          $"Proses ini memakan waktu maksimal 1x24 jam.";

            return await CreateNotifikasiAsync(
                penggunaId,
                "pembayaran",
                judul,
                pesan,
                olehSystem: true, // <-- REVISI DARI false MENJADI true
                jadwalId: jadwalId,
                pembayaranId: pembayaranId, // <-- TAMBAHKAN
                tiketId: tiketId); // <-- TAMBAHKAN
        }

        public async Task<Notifikasi> SendSegeraBayarNotificationAsync(
            int penggunaId,
            string bookingKode,
            int jamTersisa,
            int? jadwalId = null,
            int? tiketId = null) // <-- TAMBAHKAN
        {
            var judul = "Segera melakukan dan konfirmasi pembayaran!";
            var pesan = $"⚠️  {judul}\n\n" + // (Saya tambahkan spasi setelah emoji)
                          $"Pembayaran untuk booking #{bookingKode} akan kadaluarsa dalam {jamTersisa} jam. " +
                          $"Segera selesaikan pembayaran Anda.";

            return await CreateNotifikasiAsync(
                penggunaId,
                "pemberitahuan", // Ini adalah 'Pemberitahuan' (Ikon Warning !)
                judul,
                pesan,
                olehSystem: true,
                jadwalId: jadwalId,
                tiketId: tiketId); // <-- TAMBAHKAN
        }

        public async Task<Notifikasi> SendTipsPerjalananNotificationAsync(
            int penggunaId,
            int? jadwalId = null)
        {
            var judul = "Tips perjalanan";
            var pesan = $"📋  {judul}\n\n" +
                       $"Datang 1 jam sebelum keberangkatan. Persiapkan kendaraan Anda jika membawa mobil/motor. " +
                       $"Patuhi instruksi petugas pelabuhan.";

            return await CreateNotifikasiAsync(
                penggunaId,
                "umum",
                judul,
                pesan,
                olehSystem: false,
                jadwalId: jadwalId);
        }

        public async Task<bool> DeleteNotifikasiAsync(int notifikasiId)
        {
            var notif = await _context.Notifikasis.FindAsync(notifikasiId);

            if (notif == null)
                return false;

            _context.Notifikasis.Remove(notif);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteOldNotificationsAsync(int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var oldNotifs = await _context.Notifikasis
                .Where(n => n.waktu_kirim < cutoffDate)
                .ToListAsync();

            _context.Notifikasis.RemoveRange(oldNotifs);
            await _context.SaveChangesAsync();

            return oldNotifs.Count;
        }

        public Notifikasi? GetNotifikasiById(int notifikasiId)
        {
            return _context.Notifikasis
                .Include(n => n.Pengguna)
                .Include(n => n.Admin)
                .Include(n => n.Jadwal)
                .FirstOrDefault(n => n.notifikasi_id == notifikasiId);
        }

        public AppDbContext GetContext()
        {
            return _context;
        }

        /// <summary>
        /// (VERSI ADMIN) Cek semua tiket dari SEMUA pengguna.
        /// Dipanggil oleh AdminDashboard.
        /// </summary>
        public async Task CekDanKirimNotifikasiJadwalOtomatisAsync()
        {
            System.Diagnostics.Debug.WriteLine("[NotifikasiService] Menjalankan pengecekan notifikasi (untuk SEMUA user)...");

            var tiketsBerangkat = await _context.Tikets
                .Where(t => t.status_tiket == "Aktif" && t.Jadwal != null && t.Jadwal.waktu_berangkat > DateTime.UtcNow)
                .Include(t => t.Pengguna)
                .Include(t => t.Jadwal).ThenInclude(j => j.kapal)
                .Include(t => t.Jadwal).ThenInclude(j => j.pelabuhan_asal)
                .Include(t => t.Jadwal).ThenInclude(j => j.pelabuhan_tujuan)
                .ToListAsync();

            if (!tiketsBerangkat.Any())
            {
                System.Diagnostics.Debug.WriteLine("[NotifikasiService] Tidak ada tiket aktif yang perlu dicek.");
                return;
            }

            await CekJadwalTiket(tiketsBerangkat);
        }

        /// <summary>
        /// (VERSI USER) Cek semua tiket HANYA untuk satu pengguna.
        /// Dipanggil oleh LoginWindow.
        /// </summary>
        public async Task CekDanKirimNotifikasiJadwalOtomatisAsync(int penggunaId)
        {
            System.Diagnostics.Debug.WriteLine($"[NotifikasiService] Menjalankan pengecekan notifikasi (untuk user: {penggunaId})...");

            var tiketsBerangkat = await _context.Tikets
                .Where(t => t.pengguna_id == penggunaId && t.status_tiket == "Aktif" && t.Jadwal != null && t.Jadwal.waktu_berangkat > DateTime.UtcNow)
                .Include(t => t.Pengguna)
                .Include(t => t.Jadwal).ThenInclude(j => j.kapal)
                .Include(t => t.Jadwal).ThenInclude(j => j.pelabuhan_asal)
                .Include(t => t.Jadwal).ThenInclude(j => j.pelabuhan_tujuan)
                .ToListAsync();

            if (!tiketsBerangkat.Any())
            {
                System.Diagnostics.Debug.WriteLine($"[NotifikasiService] Tidak ada tiket aktif untuk user: {penggunaId}.");
                return;
            }

            await CekJadwalTiket(tiketsBerangkat);
        }

        /// <summary>
        /// Catch-up missed notifications that should have been sent when app was closed
        /// </summary>
        public async Task CatchUpMissedNotificationsAsync()
        {
            System.Diagnostics.Debug.WriteLine("[NOTIF SERVICE] 🔍 Checking for missed notifications...");

            var now = DateTime.UtcNow;

            // Get all active tickets yang mungkin ketinggalan notifikasi
            var tiketsBerangkat = await _context.Tikets
                .Where(t => t.status_tiket == "Aktif" && t.Jadwal != null && t.Jadwal.waktu_berangkat > now)
                .Include(t => t.Pengguna)
                .Include(t => t.Jadwal).ThenInclude(j => j.kapal)
                .Include(t => t.Jadwal).ThenInclude(j => j.pelabuhan_asal)
                .Include(t => t.Jadwal).ThenInclude(j => j.pelabuhan_tujuan)
                .ToListAsync();

            int missedCount = 0;

            foreach (var tiket in tiketsBerangkat)
            {
                var jadwal = tiket.Jadwal;
                if (jadwal == null) continue;

                var waktuBerangkat = jadwal.waktu_berangkat;

                // ✅ CEK H-24 MISSED: Jika sudah lewat H-25 jam tapi belum ada notif H-24
                var h24Threshold = now.AddHours(-25); // 25 jam yang lalu
                if (waktuBerangkat > now.AddHours(23) && waktuBerangkat > h24Threshold)
                {
                    bool sudahAda24Jam = await _context.Notifikasis.AnyAsync(n =>
                        n.tiket_id == tiket.tiket_id &&
                        n.jenis_notifikasi == "pengingat" &&
                        n.judul_notifikasi.Contains("24 jam"));

                    if (!sudahAda24Jam)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CATCH-UP] 📧 Sending MISSED H-24 for Tiket #{tiket.tiket_id}");

                        await SendKeberangkatanNotificationAsync(
                            tiket.pengguna_id,
                            jadwal.kapal?.nama_kapal ?? "Kapal",
                            jadwal.pelabuhan_asal?.nama_pelabuhan ?? "Pelabuhan Asal",
                            jadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "Pelabuhan Tujuan",
                            waktuBerangkat,
                            jadwal.jadwal_id,
                            tiket.tiket_id);

                        missedCount++;
                    }
                }

                // ✅ CEK H-2 MISSED: Jika sudah lewat H-3 jam tapi belum ada notif H-2  
                var h2Threshold = now.AddHours(-3); // 3 jam yang lalu
                if (waktuBerangkat > now.AddHours(1.5) && waktuBerangkat > h2Threshold)
                {
                    bool sudahAda2Jam = await _context.Notifikasis.AnyAsync(n =>
                        n.tiket_id == tiket.tiket_id &&
                        n.jenis_notifikasi == "pengingat" &&
                        n.judul_notifikasi.Contains("2 jam"));

                    if (!sudahAda2Jam)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CATCH-UP] 📧 Sending MISSED H-2 for Tiket #{tiket.tiket_id}");

                        await SendKeberangkatan2JamNotificationAsync(
                            tiket.pengguna_id,
                            tiket.kode_tiket,
                            jadwal.kapal?.nama_kapal ?? "Kapal",
                            jadwal.pelabuhan_asal?.nama_pelabuhan ?? "Pelabuhan Asal",
                            waktuBerangkat,
                            jadwal.jadwal_id,
                            tiket.tiket_id);

                        missedCount++;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CATCH-UP] 📊 Sent {missedCount} missed notifications.");
        }

        /// <summary>
        /// (MESIN UTAMA) Logika inti untuk membandingkan waktu dan mengirim notif
        /// </summary>
        private async Task CekJadwalTiket(List<Tiket> daftarTiket)
        {
            var now = DateTime.UtcNow;

            // ✅ RENTANG WAKTU LEBIH LEBAR untuk menghindari miss notification
            // H-24: Toleransi ±1 jam (23-25 jam dari sekarang)
            var batasWaktu24Jam_Atas = now.AddHours(25);
            var batasWaktu24Jam_Bawah = now.AddHours(23);

            // H-2: Toleransi ±30 menit (1.5-2.5 jam dari sekarang)
            var batasWaktu2Jam_Atas = now.AddHours(2.5);
            var batasWaktu2Jam_Bawah = now.AddHours(1.5);

            System.Diagnostics.Debug.WriteLine($"[NOTIF SERVICE] 📊 Checking {daftarTiket.Count} active tickets...");
            System.Diagnostics.Debug.WriteLine($"[NOTIF SERVICE] ⏰ Now: {now.AddHours(7):yyyy-MM-dd HH:mm:ss} WIB"); // +7 untuk WIB
            System.Diagnostics.Debug.WriteLine($"[NOTIF SERVICE] 📅 H-24 Range: {batasWaktu24Jam_Bawah.AddHours(7):HH:mm} - {batasWaktu24Jam_Atas.AddHours(7):HH:mm} WIB");
            System.Diagnostics.Debug.WriteLine($"[NOTIF SERVICE] 📅 H-2 Range: {batasWaktu2Jam_Bawah.AddHours(7):HH:mm} - {batasWaktu2Jam_Atas.AddHours(7):HH:mm} WIB");

            int count24Jam = 0;
            int count2Jam = 0;

            foreach (var tiket in daftarTiket)
            {
                var jadwal = tiket.Jadwal;
                if (jadwal == null) continue;

                var waktuBerangkat = jadwal.waktu_berangkat;

                // ========== CEK H-24 JAM ==========
                if (waktuBerangkat > batasWaktu24Jam_Bawah && waktuBerangkat <= batasWaktu24Jam_Atas)
                {
                    // ✅ PERBAIKAN: Notifikasi → Notifikasis (plural)
                    bool sudahKirim24Jam = await _context.Notifikasis
                        .AnyAsync(n =>
                            n.tiket_id == tiket.tiket_id &&
                            n.jenis_notifikasi == "pengingat" &&
                            n.judul_notifikasi.Contains("24 jam"));

                    if (!sudahKirim24Jam)
                    {
                        System.Diagnostics.Debug.WriteLine($"[NOTIF SERVICE] 📧 Sending H-24 for Tiket #{tiket.tiket_id} (User ID: {tiket.pengguna_id})");

                        await SendKeberangkatanNotificationAsync(
                            tiket.pengguna_id,
                            jadwal.kapal?.nama_kapal ?? "Kapal",
                            // ✅ PERBAIKAN: nama → nama_pelabuhan
                            jadwal.pelabuhan_asal?.nama_pelabuhan ?? "Pelabuhan Asal",
                            jadwal.pelabuhan_tujuan?.nama_pelabuhan ?? "Pelabuhan Tujuan",
                            waktuBerangkat,
                            jadwal.jadwal_id,
                            tiket.tiket_id
                        );

                        count24Jam++;
                    }
                }

                // ========== CEK H-2 JAM ==========
                if (waktuBerangkat > batasWaktu2Jam_Bawah && waktuBerangkat <= batasWaktu2Jam_Atas)
                {
                    // ✅ PERBAIKAN: Notifikasi → Notifikasis (plural)
                    bool sudahKirim2Jam = await _context.Notifikasis
                        .AnyAsync(n =>
                            n.tiket_id == tiket.tiket_id &&
                            n.jenis_notifikasi == "pengingat" &&
                            n.judul_notifikasi.Contains("2 jam"));

                    if (!sudahKirim2Jam)
                    {
                        System.Diagnostics.Debug.WriteLine($"[NOTIF SERVICE] 📧 Sending H-2 for Tiket #{tiket.tiket_id} (User ID: {tiket.pengguna_id})");

                        await SendKeberangkatan2JamNotificationAsync(
                            tiket.pengguna_id,
                            tiket.kode_tiket,
                            jadwal.kapal?.nama_kapal ?? "Kapal",
                            jadwal.pelabuhan_asal?.nama_pelabuhan ?? "Pelabuhan Asal",
                            waktuBerangkat,
                            jadwal.jadwal_id,
                            tiket.tiket_id
                        );

                        count2Jam++;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[NOTIF SERVICE] 📊 Summary: {count24Jam} H-24 sent, {count2Jam} H-2 sent.");
        }
    }
}
