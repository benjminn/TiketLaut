using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("kapal")]
    public class Kapal
    {
        [Key]
        public int kapal_id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string nama_kapal { get; set; } = string.Empty;
        
        public int kapasitas_penumpang_max { get; set; }
        
        public int kapasitas_kendaraan_max { get; set; }
        
        [StringLength(500)]
        public string fasilitas { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string deskripsi { get; set; } = string.Empty;

        // Navigation properties
        public List<Jadwal> Jadwals { get; set; } = new List<Jadwal>();

        public void tampilkanInfoKapal()
        {
            Console.WriteLine($"=== INFO KAPAL {nama_kapal} ===");
            Console.WriteLine($"Kapasitas Penumpang: {kapasitas_penumpang_max} orang");
            Console.WriteLine($"Kapasitas Kendaraan: {kapasitas_kendaraan_max} unit");
            Console.WriteLine($"Fasilitas: {fasilitas}");
            Console.WriteLine($"Deskripsi: {deskripsi}");
        }
    }
}