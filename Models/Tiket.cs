using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("Tiket")]
    public class Tiket
    {
        [Key]                                           // PRIMARY KEY
        public int tiket_id { get; set; }               // integer GENERATED ALWAYS AS IDENTITY
        
        [Required]                                      // integer NOT NULL
        public int pengguna_id { get; set; }            // FK to Pengguna
        
        [Required]                                      // integer NOT NULL  
        public int jadwal_id { get; set; }              // FK to Jadwal
        
        [Required]                                      // character varying NOT NULL UNIQUE
        public string kode_tiket { get; set; } = string.Empty;
        
        [Required]                                      // integer NOT NULL CHECK >= 0
        public int jumlah_penumpang { get; set; }
        
        [Required]                                      // numeric NOT NULL CHECK > 0
        public decimal total_harga { get; set; }
        
        [Required]                                      // timestamp with time zone NOT NULL DEFAULT now()
        public DateTime tanggal_pemesanan { get; set; } = DateTime.Now;
        
        [Required]                                      // character varying NOT NULL
        public string status_tiket { get; set; } = string.Empty;
        
        [Required]                                      // character varying NOT NULL
        public string jenis_kendaraan_enum { get; set; } = string.Empty;
        
        // Navigation properties
        [ForeignKey("pengguna_id")]
        public Pengguna Pengguna { get; set; } = null!;
        
        [ForeignKey("jadwal_id")]
        public Jadwal Jadwal { get; set; } = null!;

        
        public List<RincianPenumpang> RincianPenumpangs { get; set; } = new List<RincianPenumpang>();
        
        public List<Pembayaran> Pembayarans { get; set; } = new List<Pembayaran>();

        public bool buatTiket()
        {
            // Generate kode tiket unik
            kode_tiket = $"TKT{DateTime.Now:yyyyMMdd}{tiket_id:D6}";
            tanggal_pemesanan = DateTime.Now;
            status_tiket = "Booked";
            return true;
        }

        public void konfirmasiPembayaran()
        {
            status_tiket = "Paid";
            Console.WriteLine($"Pembayaran tiket {kode_tiket} telah dikonfirmasi.");
        }

        public void batalkanTiket()
        {
            status_tiket = "Cancelled";
            Console.WriteLine($"Tiket {kode_tiket} telah dibatalkan.");

        }

        public void tampilkanDetailTiket()
        {
            Console.WriteLine($"=== DETAIL TIKET {kode_tiket} ===");
            Console.WriteLine($"Tanggal Pemesanan: {tanggal_pemesanan:dd/MM/yyyy HH:mm}");
            Console.WriteLine($"Status: {status_tiket}");
            Console.WriteLine($"Jumlah Penumpang: {jumlah_penumpang}");
            Console.WriteLine($"Jenis Kendaraan: {jenis_kendaraan_enum}");
            Console.WriteLine($"TOTAL HARGA: Rp {total_harga:N0}");
        }

        public void cetakTiket()
        {
            Console.WriteLine("=== TIKET KAPAL LAUT ===");
            tampilkanDetailTiket();
            Console.WriteLine("Simpan tiket ini sebagai bukti pembayaran.");

        }
    }
}