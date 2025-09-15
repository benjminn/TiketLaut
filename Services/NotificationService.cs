using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    /// <summary>
    /// Class untuk menangani sistem broadcast notifikasi dari admin
    /// </summary>
    public class NotificationService
    {
        private List<Notifikasi> broadcastHistory = new List<Notifikasi>();
        
        /// <summary>
        /// Mengirim notifikasi broadcast ke semua pengguna
        /// </summary>
        public void SendBroadcastNotification(Admin admin, string message, JenisNotifikasi type, Jadwal? relatedSchedule = null)
        {
            var broadcast = new Notifikasi
            {
                notifikasi_id = GenerateNotificationId(),
                pengguna_id = 0, // 0 untuk broadcast
                jenis_enum_penumpang_update_status = type.ToString(),
                pesan = message,
                waktu_kirim = DateTime.Now,
                status_baca = false
            };
            
            broadcast.kirimNotifikasi();
            broadcastHistory.Add(broadcast);
            
            Console.WriteLine($"📢 BROADCAST: {broadcast.pesan}");
            Console.WriteLine($"   Dikirim oleh: {admin.nama} pada {broadcast.waktu_kirim:yyyy-MM-dd HH:mm:ss}");
        }
        
        /// <summary>
        /// Mengirim notifikasi perubahan jadwal secara khusus
        /// </summary>
        public void SendScheduleChangeNotification(Admin admin, Jadwal oldSchedule, Jadwal newSchedule, string reason)
        {
            string message = $"🚨 PERUBAHAN JADWAL:\n" +
                           $"Rute: Pelabuhan ID {oldSchedule.pelabuhan_asal_id} → {oldSchedule.pelabuhan_tujuan_id}\n" +
                           $"Jadwal LAMA: {oldSchedule.tanggal_berangkat:dd/MM/yyyy} pukul {oldSchedule.waktu_berangkat}\n" +
                           $"Jadwal BARU: {newSchedule.tanggal_berangkat:dd/MM/yyyy} pukul {newSchedule.waktu_berangkat}\n" +
                           $"Alasan: {reason}\n" +
                           $"Mohon cek kembali tiket Anda dan lakukan penyesuaian jika diperlukan.";
            
            SendBroadcastNotification(admin, message, JenisNotifikasi.Update, newSchedule);
        }
        
        /// <summary>
        /// Mendapatkan riwayat notifikasi broadcast
        /// </summary>
        public List<Notifikasi> GetBroadcastHistory()
        {
            return broadcastHistory.OrderByDescending(n => n.waktu_kirim).ToList();
        }
        
        /// <summary>
        /// Generate ID unik untuk notifikasi
        /// </summary>
        private int GenerateNotificationId()
        {
            return broadcastHistory.Count + 1000; // Offset untuk broadcast notifications
        }
    }
}
