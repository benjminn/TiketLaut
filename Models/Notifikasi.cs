using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Notifikasi
    {
        public int notifikasi_id { get; set; } // Primary Key
        public int pengguna_id { get; set; } // Foreign Key ke Pengguna
        public int admin_id { get; set; } // Foreign Key ke Admin (yang mengirim notifikasi)
        public string jenis_enum_penumpang_update_status { get; set; } = string.Empty;
        public string pesan { get; set; } = string.Empty;
        public DateTime waktu_kirim { get; set; }
        public bool status_baca { get; set; } = false;
        
        // OPSIONAL - hanya diisi jika notifikasi terkait jadwal spesifik
        public int? jadwal_id { get; set; } = null;  // Foreign Key ke Jadwal (OPTIONAL)
        
        // Navigation properties
        public Pengguna pengguna { get; set; } = null!; // Penerima notifikasi
        public Admin admin { get; set; } = null!; // Admin yang mengirim notifikasi
        public Jadwal? jadwal { get; set; } = null;   // Jadwal terkait (OPTIONAL)

        public void kirimNotifikasi()
        {
            // Implementasi kirim notifikasi
            waktu_kirim = DateTime.Now;
        }
    }
}
