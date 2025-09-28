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
        public string NIK_penumpang { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string keterangan { get; set; } = string.Empty;

        // Navigation properties
        public Tiket tiket { get; set; } = null!;
        public Penumpang penumpang { get; set; } = null!;
    }
}
