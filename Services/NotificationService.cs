using System;
using System.Collections.Generic;
using System.Linq;

namespace TiketLaut.Services
{
    /// <summary>
    /// Service untuk menangani sistem notifikasi broadcast dan personal
    /// Mendukung notifikasi untuk perubahan jadwal, pembatalan, dan informasi umum
    /// </summary>
    public class NotificationService
    {
        // Storage untuk history broadcast notifications
        private List<Notifikasi> broadcastHistory = new List<Notifikasi>();

        /// <summary>
        /// Mengirim notifikasi broadcast ke semua pengguna
        /// </summary>
        public void SendBroadcastNotification(Admin admin, string message, string type, Jadwal? relatedSchedule = null)
        {
            var broadcast = new Notifikasi
            {
                notifikasi_id = broadcastHistory.Count + 1,
                admin_id = admin.admin_id,
                pesan = message,
                waktu_kirim = DateTime.Now,
                jadwal_id = relatedSchedule?.jadwal_id
            };

            broadcastHistory.Add(broadcast);

            // Simulasi pengiriman ke semua pengguna
            Console.WriteLine($"[BROADCAST] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Dari: {admin.nama} ({admin.username})");
            Console.WriteLine($"Tipe: {type}");
            Console.WriteLine($"Pesan: {message}");
            
            if (relatedSchedule != null)
            {
                Console.WriteLine($"Terkait Jadwal ID: {relatedSchedule.jadwal_id}");
            }
            
            Console.WriteLine("Status: Terkirim ke semua pengguna");
            Console.WriteLine(new string('-', 50));
        }

        /// <summary>
        /// Mengirim notifikasi untuk perubahan jadwal
        /// </summary>
        public void SendScheduleChangeNotification(Admin admin, Jadwal oldSchedule, Jadwal newSchedule, string reason)
        {
            var message = $"PERUBAHAN JADWAL - Jadwal ID {oldSchedule.jadwal_id} telah diubah. " +
                         $"Waktu baru: {newSchedule.waktu_berangkat} - {newSchedule.waktu_tiba}. " +
                         $"Alasan: {reason}";

            SendBroadcastNotification(admin, message, "Update", newSchedule);

            // Log khusus untuk perubahan jadwal
            Console.WriteLine($"[SCHEDULE CHANGE] Jadwal {oldSchedule.jadwal_id} diubah oleh {admin.nama}");
        }

        /// <summary>
        /// Mendapatkan history broadcast notifications
        /// </summary>
        public List<Notifikasi> GetBroadcastHistory()
        {
            return broadcastHistory.OrderByDescending(x => x.waktu_kirim).ToList();
        }

        /// <summary>
        /// Mengirim notifikasi personal ke pengguna tertentu
        /// </summary>
        public void SendPersonalNotification(Pengguna pengguna, string message, string type = "Info")
        {
            var notification = new Notifikasi
            {
                notifikasi_id = new Random().Next(1000, 9999),
                pengguna_id = pengguna.pengguna_id,
                pesan = message,
                waktu_kirim = DateTime.Now
            };

            Console.WriteLine($"[PERSONAL] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Kepada: {pengguna.nama} ({pengguna.email})");
            Console.WriteLine($"Tipe: {type}");
            Console.WriteLine($"Pesan: {message}");
            Console.WriteLine("Status: Terkirim");
            Console.WriteLine(new string('-', 50));
        }

        /// <summary>
        /// Menampilkan statistik notifikasi
        /// </summary>
        public void ShowNotificationStats()
        {
            Console.WriteLine("=== STATISTIK NOTIFIKASI ===");
            Console.WriteLine($"Total Broadcast Terkirim: {broadcastHistory.Count}");
            Console.WriteLine($"Notifikasi Hari Ini: {broadcastHistory.Count(x => x.waktu_kirim.Date == DateTime.Today)}");
            
            if (broadcastHistory.Any())
            {
                var latest = broadcastHistory.OrderByDescending(x => x.waktu_kirim).First();
                Console.WriteLine($"Broadcast Terakhir: {latest.waktu_kirim:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Pesan Terakhir: {latest.pesan.Substring(0, Math.Min(50, latest.pesan.Length))}...");
            }
        }
    }
}