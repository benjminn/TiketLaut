using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("pelabuhan")]
    public class Pelabuhan
    {
        [Key]
        public int pelabuhan_id { get; set; }
        
        [Required]
        [StringLength(100)]

        public string nama_pelabuhan { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string kota { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string provinsi { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string fasilitas { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string deskripsi { get; set; } = string.Empty;

        // Navigation properties
        public List<Jadwal> JadwalAsals { get; set; } = new List<Jadwal>();
        public List<Jadwal> JadwalTujuans { get; set; } = new List<Jadwal>();

        public void tampilkanInfoPelabuhan()
        {
            Console.WriteLine($"=== INFO PELABUHAN {nama_pelabuhan} ===");
            Console.WriteLine($"Kota: {kota}");
            Console.WriteLine($"Provinsi: {provinsi}");
            Console.WriteLine($"Fasilitas: {fasilitas}");
            Console.WriteLine($"Deskripsi: {deskripsi}");
        }
    }
}
