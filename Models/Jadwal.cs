using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("Jadwal")]
    public class Jadwal
    {
        [Key]
        public int jadwal_id { get; set; }
        
        [ForeignKey("pelabuhan_asal")]
        public int pelabuhan_asal_id { get; set; }
        
        [ForeignKey("pelabuhan_tujuan")]
        public int pelabuhan_tujuan_id { get; set; }
        
        [ForeignKey("kapal")]
        public int kapal_id { get; set; }
        
        public TimeOnly waktu_berangkat { get; set; }
        
        public TimeOnly waktu_tiba { get; set; }
        
        [Range(0, int.MaxValue)]
        public int sisa_kapasitas_penumpang { get; set; }
        
        [Range(0, int.MaxValue)]
        public int sisa_kapasitas_kendaraan { get; set; }
        
        [Required]
        [StringLength(50)]
        public string status { get; set; } = "Active";

        // ✅ FIELD INI WAJIB ADA
        [Required]
        [StringLength(50)]
        public string kelas_layanan { get; set; } = "Reguler";

        // Navigation properties
        public Pelabuhan pelabuhan_asal { get; set; } = null!;
        public Pelabuhan pelabuhan_tujuan { get; set; } = null!;
        public Kapal kapal { get; set; } = null!;
        
        public List<DetailKendaraan> DetailKendaraans { get; set; } = new List<DetailKendaraan>();
        public List<Tiket> Tikets { get; set; } = new List<Tiket>();
        public List<Notifikasi> Notifikasis { get; set; } = new List<Notifikasi>();

        public void tampilkanDetailJadwal()
        {
            Console.WriteLine($"=== DETAIL JADWAL {jadwal_id} ===");
            Console.WriteLine($"Rute: {pelabuhan_asal?.nama_pelabuhan} → {pelabuhan_tujuan?.nama_pelabuhan}");
            Console.WriteLine($"Kapal: {kapal?.nama_kapal}");
            Console.WriteLine($"Kelas Layanan: {kelas_layanan}");
            Console.WriteLine($"Waktu Berangkat: {waktu_berangkat}");
            Console.WriteLine($"Waktu Tiba: {waktu_tiba}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Sisa Kapasitas Penumpang: {sisa_kapasitas_penumpang}");
            Console.WriteLine($"Sisa Kapasitas Kendaraan: {sisa_kapasitas_kendaraan}");
        }
    }
}