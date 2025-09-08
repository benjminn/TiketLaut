using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KapalKlik
{
    public class Notifikasi
    {
        public int notifikasi_id { get; set; }
        public JenisNotifikasi jenis { get; set; }
        public string pesan { get; set; } = string.Empty;
        public DateTime waktu_kirim { get; set; }
        public string status_baca { get; set; } = string.Empty;
        public bool is_broadcast { get; set; } = false; // Menandakan apakah ini broadcast
        
        // Navigational properties
        public Admin admin { get; set; } = null!; // Admin yang mengirim notifikasi
        public int admin_id { get; set; }
        public Pengguna? pengguna { get; set; } = null; // Null jika broadcast ke semua
        public int? pengguna_id { get; set; } = null; // Null jika broadcast ke semua
        public Jadwal? jadwal { get; set; } = null; // Jadwal yang berubah (jika ada)
        public int? jadwal_id { get; set; } = null;

        public void kirimNotifikasi()
        {
            // Implementasi kirim notifikasi
            waktu_kirim = DateTime.Now;
            status_baca = "Belum dibaca";
        }

        public void kirimBroadcastNotifikasi()
        {
            // Implementasi kirim broadcast notifikasi ke semua pengguna
            is_broadcast = true;
            waktu_kirim = DateTime.Now;
            status_baca = "Belum dibaca";
        }

        public void tandaiBacaan()
        {
            // Implementasi tandai sebagai sudah dibaca
            status_baca = "Sudah dibaca";
        }
    }
}
