using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Notifikasi
    {
        public int notifikasi_id { get; set; }
        public JenisNotifikasi jenis_enum_penumpang_update_status { get; set; }
        public string pesan { get; set; } = string.Empty;
        public DateTime waktu_kirim { get; set; }
        public bool status_baca { get; set; } = false;
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
            status_baca = false;
        }

        public void kirimBroadcastNotifikasi()
        {
            // Implementasi kirim broadcast notifikasi ke semua pengguna
            is_broadcast = true;
            waktu_kirim = DateTime.Now;
            status_baca = false;
        }

        public void tandaiBacaan()
        {
            // Implementasi tandai sebagai sudah dibaca
            status_baca = true;
        }
        
        public void tampilkanNotifikasi()
        {
            Console.WriteLine($"[{waktu_kirim:dd/MM/yyyy HH:mm}] {pesan}");
            if (!status_baca)
            {
                Console.WriteLine("(Belum dibaca)");
            }
        }
        
        public void updateStatusBaca()
        {
            status_baca = true;
            Console.WriteLine("Status notifikasi diupdate menjadi sudah dibaca.");
        }
    }
}
