using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KapalKlik
{
    public class Tiket
    {
        public int tiket_id { get; set; }
        public double total_harga { get; set; }
        public DateTime tanggal_pemesanan { get; set; }
        public JenisKendaraan jenis_kendaraan { get; set; }
        public StatusTiket status { get; set; }

        public bool buatTiket()
        {
            // Implementasi buat tiket
            return true;
        }

        public void tampilkanDetailTiket()
        {
            // Implementasi tampilkan detail tiket
        }

        public void cetakTiket()
        {
            // Implementasi cetak tiket
        }

        public void konfirmasiPembayaran()
        {
            // Implementasi konfirmasi pembayaran
        }

        public void batalkanTiket()
        {
            // Implementasi batalkan tiket
        }
    }
}
