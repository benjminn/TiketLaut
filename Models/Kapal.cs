using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KapalKlik
{
    public class Kapal
    {
        public int kapal_id { get; set; }
        public string nama_kapal { get; set; } = string.Empty;
        public int kapasitas_penumpang_id { get; set; }
        public int kapasitas_kendaraan { get; set; }
        public string fasilitas { get; set; } = string.Empty;
        public string deskripsi { get; set; } = string.Empty;

        public void tampilkanInfoKapal()
        {
            // Implementasi tampilkan info kapal
        }
    }
}
