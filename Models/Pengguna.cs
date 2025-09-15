using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Pengguna
    {
        public int pengguna_id { get; set; } // Primary Key
        public string nama { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string no_hp { get; set; } = string.Empty;
        public string jenis_kelamin { get; set; } = string.Empty;
        public DateTime tanggal_lahir { get; set; }
        public string kewarganegaraan { get; set; } = string.Empty;
        public string alamat { get; set; } = string.Empty;
        public DateTime tanggal_daftar { get; set; }

        public bool login()
        {
            // Implementasi login pengguna
            return true;
        }

        public void editProfil()
        {
            // Implementasi edit profil
        }

        public void lihatPemberitahuan()
        {
            // Implementasi lihat pemberitahuan
        }

        public void cariJadwal()
        {
            // Implementasi cari jadwal
        }

        public void pesanTiket()
        {
            // Implementasi pesan tiket
        }

        public void lihatRiwayatTiket()
        {
            // Implementasi lihat riwayat tiket
        }
    }
}
