using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KapalKlik
{
    public class Pelabuhan
    {
        public int pelabuhan_id { get; set; }
        public string nama_pelabuhan { get; set; } = string.Empty;
        public string kota { get; set; } = string.Empty;
        public string provinsi { get; set; } = string.Empty;
        public string fasilitas { get; set; } = string.Empty;
        public string deskripsi { get; set; } = string.Empty;

        public void tampilkanInfoPelabuhan()
        {
            // Implementasi tampilkan info pelabuhan
        }
    }
}
