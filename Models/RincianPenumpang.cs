using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class RincianPenumpang
    {
        public int rincian_penumpang_id { get; set; }
        
        // Foreign key ke Penumpang
        public int NIK_penumpang { get; set; }
        
        // Navigation property
        public Penumpang penumpang { get; set; } = null!;
        
        // Additional properties yang mungkin diperlukan
        public string keterangan { get; set; } = string.Empty;
        public DateTime tanggal_registrasi { get; set; } = DateTime.Now;
    }
}
