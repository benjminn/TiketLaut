using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Pembayaran
    {
        public int pembayaran_id { get; set; } // Primary Key
        public int tiket_id { get; set; } // Foreign Key ke Tiket
        public string metode_pembayaran { get; set; } = string.Empty;
        public double jumlah_bayar { get; set; }
        public DateTime tanggal_bayar { get; set; }

        public void prosesPembayaran()
        {
            // Implementasi proses pembayaran
        }

        public void konfirmasiPembayaran()
        {
            // Implementasi konfirmasi pembayaran
        }
    }
}
