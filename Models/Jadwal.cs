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
        
        [Required]
        [Column(TypeName = "timestamp with time zone")]
        public DateTime waktu_berangkat { get; set; }
        
        [Required]
        [Column(TypeName = "timestamp with time zone")]
        public DateTime waktu_tiba { get; set; }
        
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

        // FK to GrupKendaraan (required - every jadwal must have vehicle pricing group)
        [Required]
        public int grup_kendaraan_id { get; set; }

        // Navigation properties
        public Pelabuhan pelabuhan_asal { get; set; } = null!;
        public Pelabuhan pelabuhan_tujuan { get; set; } = null!;
        public Kapal kapal { get; set; } = null!;
        
        [ForeignKey("grup_kendaraan_id")]
        public GrupKendaraan? GrupKendaraan { get; set; }
        
        public List<Tiket> Tikets { get; set; } = new List<Tiket>();
        public List<Notifikasi> Notifikasis { get; set; } = new List<Notifikasi>();

        /// <summary>
        /// Convert UTC time to pelabuhan asal timezone
        /// </summary>
        [NotMapped]
        public DateTime WaktuBerangkatLokal
        {
            get
            {
                if (pelabuhan_asal == null) return waktu_berangkat;
                var offsetHours = pelabuhan_asal.TimezoneOffsetHours;
                return waktu_berangkat.AddHours(offsetHours);
            }
        }

        /// <summary>
        /// Convert UTC time to pelabuhan tujuan timezone
        /// </summary>
        [NotMapped]
        public DateTime WaktuTibaLokal
        {
            get
            {
                if (pelabuhan_tujuan == null) return waktu_tiba;
                var offsetHours = pelabuhan_tujuan.TimezoneOffsetHours;
                return waktu_tiba.AddHours(offsetHours);
            }
        }

        /// <summary>
        /// Get duration text with timezone consideration
        /// </summary>
        [NotMapped]
        public string DurasiWithTimezone
        {
            get
            {
                var duration = waktu_tiba - waktu_berangkat;
                return $"{(int)duration.TotalHours} jam {duration.Minutes} menit";
            }
        }

        public void tampilkanDetailJadwal()
        {
            Console.WriteLine($"=== DETAIL JADWAL {jadwal_id} ===");
            Console.WriteLine($"Rute: {pelabuhan_asal?.nama_pelabuhan} ({pelabuhan_asal?.timezone}) → {pelabuhan_tujuan?.nama_pelabuhan} ({pelabuhan_tujuan?.timezone})");
            Console.WriteLine($"Kapal: {kapal?.nama_kapal}");
            Console.WriteLine($"Kelas Layanan: {kelas_layanan}");
            Console.WriteLine($"Waktu Berangkat: {WaktuBerangkatLokal:HH:mm} {pelabuhan_asal?.timezone}");
            Console.WriteLine($"Waktu Tiba: {WaktuTibaLokal:HH:mm} {pelabuhan_tujuan?.timezone}");
            Console.WriteLine($"Durasi: {DurasiWithTimezone}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Sisa Kapasitas Penumpang: {sisa_kapasitas_penumpang}");
            Console.WriteLine($"Sisa Kapasitas Kendaraan: {sisa_kapasitas_kendaraan}");
        }
    }
}