using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("Penumpang")]
    public class Penumpang
    {
        [Key]                                           // PRIMARY KEY
        public int penumpang_id { get; set; }           // integer GENERATED ALWAYS AS IDENTITY
        
        [Required]                                      // integer NOT NULL
        public int pengguna_id { get; set; }            // FK to Pengguna
        
        [Required]                                      // character varying NOT NULL
        public string nama { get; set; } = string.Empty;
        
        [Required]                                      // integer NOT NULL
        public int NIK_penumpang { get; set; }
        
        // Navigation properties
        [ForeignKey("pengguna_id")]
        public Pengguna Pengguna { get; set; } = null!;
        
        public List<RincianPenumpang> RincianPenumpangs { get; set; } = new List<RincianPenumpang>();
    }
}
