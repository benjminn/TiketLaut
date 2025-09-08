using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KapalKlik
{
    // Class untuk menghubungkan relationship many-to-many atau detail relationship
    public class DetailTiket
    {
        public int detail_id { get; set; }
        public int tiket_id { get; set; }
        public int jadwal_id { get; set; }
        public int kapal_id { get; set; }
        public int pengguna_id { get; set; }
        public int pembayaran_id { get; set; }

        // Navigation properties
        public Tiket tiket { get; set; } = null!;
        public Jadwal jadwal { get; set; } = null!;
        public Kapal kapal { get; set; } = null!;
        public Pengguna pengguna { get; set; } = null!;
        public Pembayaran pembayaran { get; set; } = null!;
    }

    // Class untuk menghubungkan Jadwal dengan Kapal
    public class JadwalKapal
    {
        public int jadwal_id { get; set; }
        public int kapal_id { get; set; }

        public Jadwal jadwal { get; set; } = null!;
        public Kapal kapal { get; set; } = null!;
    }

    // Class untuk menghubungkan Jadwal dengan Pelabuhan
    public class JadwalPelabuhan
    {
        public int jadwal_id { get; set; }
        public int pelabuhan_asal_id { get; set; }
        public int pelabuhan_tujuan_id { get; set; }

        public Jadwal jadwal { get; set; } = null!;
        public Pelabuhan pelabuhanAsal { get; set; } = null!;
        public Pelabuhan pelabuhanTujuan { get; set; } = null!;
    }
}
