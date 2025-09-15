using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class RincianPenumpang
    {
        public int rincian_penumpang_id { get; set; } // Primary Key
        public int tiket_id { get; set; } // Foreign Key ke Tiket
        public int penumpang_id { get; set; } // Foreign Key ke Penumpang
        
        // Navigation properties
        public Tiket tiket { get; set; } = null!;
        public Penumpang penumpang { get; set; } = null!;

        public void tampilkanRincianPenumpang()
        {
            Console.WriteLine($"=== RINCIAN PENUMPANG ===");
            Console.WriteLine($"ID Rincian: {rincian_penumpang_id}");
            Console.WriteLine($"ID Tiket: {tiket_id}");
            Console.WriteLine($"ID Penumpang: {penumpang_id}");
            
            if (penumpang != null)
            {
                Console.WriteLine($"Nama Penumpang: {penumpang.nama}");
                Console.WriteLine($"NIK: {penumpang.NIK_penumpang}");
            }
            
            if (tiket != null)
            {
                Console.WriteLine($"Status Tiket: {tiket.status}");
                Console.WriteLine($"Total Harga: Rp {tiket.total_harga:N0}");
            }
        }
    }
}
