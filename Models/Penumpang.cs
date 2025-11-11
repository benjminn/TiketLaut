using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("Penumpang")]
    public class Penumpang
    {
        [Key]
        public int penumpang_id { get; set; }
        
        [Required]
        public int pengguna_id { get; set; }
        
        [Required]
        public string nama { get; set; } = string.Empty;
        
        [Required]
        public long nomor_identitas { get; set; }          
        [Required]
        public string jenis_identitas { get; set; } = string.Empty;          
        [Required]
        public string jenis_kelamin { get; set; } = string.Empty;          
        // Navigation properties
        [ForeignKey("pengguna_id")]
        public Pengguna Pengguna { get; set; } = null!;
        
        public List<RincianPenumpang> RincianPenumpangs { get; set; } = new List<RincianPenumpang>();
    }
}
