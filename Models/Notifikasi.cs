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
        public string jenis_enum_penumpang_update_status { get; set; } = string.Empty;
        public string pesan { get; set; } = string.Empty;
        public DateTime waktu_kirim { get; set; }
        public bool status_baca { get; set; } = false;
        public int kirimNotifikasiId { get; set; }

        public void kirimNotifikasi()
        {
            // Implementasi kirim notifikasi
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
