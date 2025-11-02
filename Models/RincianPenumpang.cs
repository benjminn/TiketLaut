using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("RincianPenumpang")]  // ? Fixed table name
    public class RincianPenumpang
    {
        [Key]
        public int rincian_id { get; set; }  // ? Changed from rincian_penumpang_id
        
        [Required]
        public int tiket_id { get; set; }
        
        [Required]
        public int penumpang_id { get; set; }

        // Navigation properties
        [ForeignKey("tiket_id")]
        public Tiket tiket { get; set; } = null!;
        
        [ForeignKey("penumpang_id")]
        public Penumpang penumpang { get; set; } = null!;

        public void tampilkanRincianPenumpang()
        {
            Console.WriteLine($"=== RINCIAN PENUMPANG ===");
            Console.WriteLine($"ID Rincian: {rincian_id}");
            Console.WriteLine($"ID Tiket: {tiket_id}");
            Console.WriteLine($"ID Penumpang: {penumpang_id}");

            if (penumpang != null)
            {
                Console.WriteLine($"Nama Penumpang: {penumpang.nama}");
                Console.WriteLine($"Nomor Identitas: {penumpang.nomor_identitas}");
            }

            if (tiket != null)
            {
                Console.WriteLine($"Status Tiket: {tiket.status_tiket}");
                Console.WriteLine($"Total Harga: Rp {tiket.total_harga:N0}");
            }
        }
    }
}
