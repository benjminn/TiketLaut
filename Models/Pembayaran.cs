using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("pembayaran")]
    public class Pembayaran
    {
        [Key]
        public int pembayaran_id { get; set; }
        
        [ForeignKey("tiket")]
        public int tiket_id { get; set; }
        
        [StringLength(50)]

        public string metode_pembayaran { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(15,2)")]
        public decimal jumlah_bayar { get; set; }
        
        public DateTime tanggal_bayar { get; set; }
        
        [StringLength(50)]
        public string status_bayar { get; set; } = string.Empty;

        // Navigation properties
        public Tiket tiket { get; set; } = null!;

        public void prosesPembayaran()
        {
            status_bayar = "Processed";
            tanggal_bayar = DateTime.Now;
            Console.WriteLine($"Pembayaran {pembayaran_id} senilai Rp {jumlah_bayar:N0} telah diproses.");
        }

        public void konfirmasiPembayaran()
        {
            status_bayar = "Confirmed";
            Console.WriteLine($"Pembayaran {pembayaran_id} telah dikonfirmasi.");
        }
    }
}
