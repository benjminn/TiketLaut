using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("Notifikasi")]
    public class Notifikasi
    {
        [Key]                                           // PRIMARY KEY
        public int notifikasi_id { get; set; }          // integer GENERATED ALWAYS AS IDENTITY
        
        [Required]                                      // integer NOT NULL
        public int pengguna_id { get; set; }            // FK to Pengguna
        
        [Required]                                      // character varying NOT NULL
        public string jenis_enum_penumpang_update_status { get; set; } = string.Empty;
        
        [Required]                                      // text NOT NULL
        public string pesan { get; set; } = string.Empty;
        
        [Required]                                      // timestamp with time zone NOT NULL DEFAULT now()
        public DateTime waktu_kirim { get; set; } = DateTime.Now;
        
        [Required]                                      // boolean NOT NULL DEFAULT false
        public bool status_baca { get; set; } = false;
        
        [Required]                                      // integer NOT NULL
        public int admin_id { get; set; }               // FK to Admin
        
        public int? jadwal_id { get; set; }             // integer (nullable)
        
        // Navigation properties
        [ForeignKey("pengguna_id")]
        public Pengguna Pengguna { get; set; } = null!;
        
        [ForeignKey("admin_id")]
        public Admin Admin { get; set; } = null!;
        
        [ForeignKey("jadwal_id")]
        public Jadwal? Jadwal { get; set; }

        public void kirimNotifikasi()
        {
            // Implementasi kirim notifikasi
            waktu_kirim = DateTime.Now;
            status_baca = false;
        }

        public void kirimBroadcastNotifikasi()
        {
            // Implementasi kirim broadcast notifikasi ke semua pengguna
            // Broadcast ditandai dengan pengguna_id = 0
            waktu_kirim = DateTime.Now;
            status_baca = false;
        }

        public void tandaiBacaan()
        {
            // Implementasi tandai sebagai sudah dibaca
            status_baca = true;
        }
        
        public void tampilkanNotifikasi()
        {
            Console.WriteLine($"[{waktu_kirim:dd/MM/yyyy HH:mm}] {pesan}");
            if (!status_baca)
            {
                Console.WriteLine("(Belum dibaca)");
            }
        }
        
        public void updateStatusBaca()
        {
            status_baca = true;
            Console.WriteLine("Status notifikasi diupdate menjadi sudah dibaca.");
        }
    }
}
