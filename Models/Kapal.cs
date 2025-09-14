using System;

namespace TiketLaut
{
    public class Kapal
    {
        public int kapal_id { get; set; } // Primary Key
        public string nama_kapal { get; set; } = string.Empty;
        public int kapasitas_penumpang_max { get; set; }
        public int kapasitas_kendaraan_max { get; set; }
        public string fasilitas { get; set; } = string.Empty;
        public string deskripsi { get; set; } = string.Empty;

        public void tampilkanInfoKapal()
        {
            Console.WriteLine($"=== INFO KAPAL {nama_kapal} ===");
        }
    }
}