using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KapalKlik
{
    public class Jadwal
    {
        public int jadwal_id { get; set; }
        public int pelabuhan_asal_id { get; set; }
        public int pelabuhan_tujuan_id { get; set; }
        public string kelas { get; set; } = string.Empty;
        public DateTime tanggal_berangkat { get; set; }
        public TimeSpan waktu_berangkat { get; set; }
        public TimeSpan waktu_tiba { get; set; }
        public int sisa_kapasitas_penumpang_id { get; set; }
        public int sisa_kapasitas_kendaraan_id { get; set; }
        public JenisKendaraan jenis_kendaraan { get; set; }
        public StatusTiket status { get; set; }

        public void tampilkanJadwal()
        {
            // Implementasi tampilkan jadwal
        }

        public void getDurasiPerjalanan()
        {
            // Implementasi get durasi perjalanan
        }

        public void konfirmasiKetersediaan()
        {
            // Implementasi konfirmasi ketersediaan
        }

        public void kurangKapasitaspenumpang()
        {
            // Implementasi kurang kapasitas penumpang
        }

        public void pertambahanStatusJadwal()
        {
            // Implementasi pertambahan status jadwal
        }

        public void hitungKapasitaspenumpang()
        {
            // Implementasi hitung kapasitas penumpang
        }
    }
}
