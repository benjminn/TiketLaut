using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("Pelabuhan")]  // ? Fix: PascalCase
    public class Pelabuhan
    {
        [Key]
        public int pelabuhan_id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string nama_pelabuhan { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string kota { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string provinsi { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string fasilitas { get; set; } = string.Empty;
        
        // ? FIX: Ubah jadi nullable
        [StringLength(1000)]
        public string? deskripsi { get; set; }  // NULLABLE!

        // ? NEW: Timezone support (WIB, WITA, WIT)
        [Required]
        [StringLength(10)]
        public string timezone { get; set; } = "WIB";  // Default WIB
        
        /// <summary>
        /// Get timezone offset in hours from UTC
        /// WIB = UTC+7, WITA = UTC+8, WIT = UTC+9
        /// </summary>
        [NotMapped]
        public int TimezoneOffsetHours
        {
            get
            {
                return timezone switch
                {
                    "WIB" => 7,
                    "WITA" => 8,
                    "WIT" => 9,
                    _ => 7  // Default WIB
                };
            }
        }

        // Navigation properties
        public List<Jadwal> JadwalAsals { get; set; } = new List<Jadwal>();
        public List<Jadwal> JadwalTujuans { get; set; } = new List<Jadwal>();

        public void tampilkanInfoPelabuhan()
        {
            Console.WriteLine($"=== INFO PELABUHAN {nama_pelabuhan} ===");
            Console.WriteLine($"Kota: {kota}");
            Console.WriteLine($"Provinsi: {provinsi}");
            Console.WriteLine($"Timezone: {timezone} (UTC+{TimezoneOffsetHours})");
            Console.WriteLine($"Fasilitas: {fasilitas}");
            Console.WriteLine($"Deskripsi: {deskripsi ?? "(Tidak ada deskripsi)"}");  // ? Handle null
        }
    }
}
