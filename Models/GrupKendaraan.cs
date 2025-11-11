using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("GrupKendaraan")]
    public class GrupKendaraan
    {
        [Key]
        public int grup_kendaraan_id { get; set; }

        [Required]
        [MaxLength(255)]
        public string nama_grup_kendaraan { get; set; } = string.Empty; // e.g., "Set Harga November 2025", "Promo Lebaran"

        public DateTime created_at { get; set; } = DateTime.UtcNow;

        // Navigation properties
        // One grup has many detail kendaraan (exactly 13)
        public ICollection<DetailKendaraan> DetailKendaraans { get; set; } = new List<DetailKendaraan>();

        // Many jadwals can use one grup
        public ICollection<Jadwal> Jadwals { get; set; } = new List<Jadwal>();

        public override string ToString()
        {
            // Hindari lazy loading dari DetailKendaraans.Count yang bisa menyebabkan disposed context error
            // Gunakan null-safe count untuk menghindari exception
            var count = DetailKendaraans?.Count ?? 0;
            return $"Grup #{grup_kendaraan_id}: {nama_grup_kendaraan} ({count} golongan)";
        }
    }
}
