using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Penumpang
    {
        public int penumpang_id { get; set; } // Primary Key  
        public int pengguna_id { get; set; } // Foreign Key ke Pengguna (user yang menambahkan)
        public string nama { get; set; } = string.Empty;
        public string NIK_penumpang { get; set; } = string.Empty;
        
        // Navigation property
        public Pengguna pengguna { get; set; } = null!;

        public void tampilkanInfoPenumpang()
        {
            Console.WriteLine($"=== INFO PENUMPANG ===");
            Console.WriteLine($"ID: {penumpang_id}");
            Console.WriteLine($"Nama: {nama}");
            Console.WriteLine($"NIK: {NIK_penumpang}");
            Console.WriteLine($"Ditambahkan oleh: {pengguna?.nama ?? "Unknown"}");
        }
    }
}
