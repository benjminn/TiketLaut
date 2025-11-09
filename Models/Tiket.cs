using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("Tiket")]
    public class Tiket
    {
        [Key]
        public int tiket_id { get; set; }
        
        [Required]
        public int pengguna_id { get; set; }
        
        [Required]
        public int jadwal_id { get; set; }
        
        [Required]
        public string kode_tiket { get; set; } = string.Empty;
        
        [Required]
        public int jumlah_penumpang { get; set; }
        
        [Required]
        public decimal total_harga { get; set; }
        
        [Required]
        public DateTime tanggal_pemesanan { get; set; } = DateTime.Now;
        
        [Required]
        public string status_tiket { get; set; } = string.Empty;
        
        [Required]
        public string jenis_kendaraan_enum { get; set; } = string.Empty;
        
        public string? plat_nomor { get; set; }
        
        // Data Pemesan (PIC yang melakukan pemesanan) - NULLABLE untuk backward compatibility
        public string? nama_pemesan { get; set; }
        public string? nomor_hp_pemesan { get; set; }
        public string? email_pemesan { get; set; }
        
        // Navigation properties
        [ForeignKey("pengguna_id")]
        public Pengguna Pengguna { get; set; } = null!;
        
        [ForeignKey("jadwal_id")]
        public Jadwal Jadwal { get; set; } = null!;
        
        public List<RincianPenumpang> RincianPenumpangs { get; set; } = new List<RincianPenumpang>();
        
        public List<Pembayaran> Pembayarans { get; set; } = new List<Pembayaran>();

        public bool buatTiket()
        {
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
            Console.WriteLine($"Plat Nomor: {plat_nomor ?? "N/A"}");
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