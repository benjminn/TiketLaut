using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("pengguna")]
    public class Pengguna
    {
        [Key]
        public int pengguna_id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string nama { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string password { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string no_hp { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string jenis_kelamin { get; set; } = string.Empty;
        
        public DateOnly tanggal_lahir { get; set; }
        
        [StringLength(50)]
        public string kewarganegaraan { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string alamat { get; set; } = string.Empty;
        
        public DateTime tanggal_daftar { get; set; } = DateTime.Now;

        // Navigation properties
        public List<Penumpang> Penumpangs { get; set; } = new List<Penumpang>();
        public List<Tiket> Tikets { get; set; } = new List<Tiket>();
        public List<Notifikasi> Notifikasis { get; set; } = new List<Notifikasi>();

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
            Console.WriteLine("Memproses pemesanan tiket...");
        }

        public void lihatRiwayatTiket()
        {
            Console.WriteLine($"Menampilkan riwayat tiket untuk {nama}");
        }

        public void tampilkanProfil()
        {
            var umur = DateTime.Today.Year - tanggal_lahir.Year;
            if (DateTime.Today < tanggal_lahir.ToDateTime(TimeOnly.MinValue).AddYears(umur))
                umur--;
                
            Console.WriteLine($"=== PROFIL PENGGUNA {nama} ===");
            Console.WriteLine($"Email: {email}");
            Console.WriteLine($"No HP: {no_hp}");
            Console.WriteLine($"Umur: {umur} tahun");
            Console.WriteLine($"Jenis Kelamin: {jenis_kelamin}");
            Console.WriteLine($"Tanggal Lahir: {tanggal_lahir}");
            Console.WriteLine($"Kewarganegaraan: {kewarganegaraan}");
            Console.WriteLine($"Alamat: {alamat}");
        }
    }
}
