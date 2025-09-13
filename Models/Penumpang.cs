using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Penumpang
    {
        public int NIK_penumpang { get; set; }
        public string nama { get; set; } = string.Empty;
        public int no_hp { get; set; }

        // Navigational properties untuk relationship
        public List<RincianPenumpang> rincianPenumpangs { get; set; } = new List<RincianPenumpang>();
    }
}
