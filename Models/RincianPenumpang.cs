using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("rincian_penumpang")]
    public class RincianPenumpang
    {
        [Key]
        public int rincian_penumpang_id { get; set; }
        
        [ForeignKey("tiket")]
        public int tiket_id { get; set; }
        
        [ForeignKey("penumpang")]
        public int penumpang_id { get; set; }

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
                Console.WriteLine($"Status Tiket: {tiket.status_tiket}");
                Console.WriteLine($"Total Harga: Rp {tiket.total_harga:N0}");
            }
        }
    }
}
