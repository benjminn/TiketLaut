using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("Pengguna", Schema = "public")]
    public class Pengguna
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("pengguna_id")]
        public int pengguna_id { get; set; }

        [Required]
        [Column("nama")]
        public string nama { get; set; } = string.Empty;

        [Required]
        [Column("email")]
        public string email { get; set; } = string.Empty;

        [Required]
        [Column("password")]
        public string password { get; set; } = string.Empty;

        [Required]
        [MaxLength(16)]
        [Column("nomor_induk_kependudukan")]
        public string nomor_induk_kependudukan { get; set; } = string.Empty;

        [Required]
        [Column("jenis_kelamin")]
        public string jenis_kelamin { get; set; } = string.Empty;

        [Required]
        [Column("tanggal_lahir")]
        public DateOnly tanggal_lahir { get; set; }

        [Required]
        [Column("kewarganegaraan")]
        public string kewarganegaraan { get; set; } = string.Empty;

        [Column("alamat")]
        public string? alamat { get; set; } // nullable - sesuai schema

        [Required]
        [Column("tanggal_daftar")]
        public DateTime tanggal_daftar { get; set; } = DateTime.UtcNow;  // Default UTC

        // Navigation properties
        public List<Penumpang> Penumpangs { get; set; } = new List<Penumpang>();
        public List<Tiket> Tikets { get; set; } = new List<Tiket>();
        public List<Notifikasi> Notifikasis { get; set; } = new List<Notifikasi>();

        public bool login()
        {
            return true;
        }

        public void editProfil()
        {
        }

        public void lihatPemberitahuan()
        {
        }

        public void cariJadwal()
        {
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
            Console.WriteLine($"NIK: {nomor_induk_kependudukan}");  // CHANGED: from "No HP" to "NIK"
            Console.WriteLine($"Umur: {umur} tahun");
            Console.WriteLine($"Jenis Kelamin: {jenis_kelamin}");
            Console.WriteLine($"Tanggal Lahir: {tanggal_lahir}");
            Console.WriteLine($"Kewarganegaraan: {kewarganegaraan}");
            Console.WriteLine($"Alamat: {alamat}");
        }
    }
}