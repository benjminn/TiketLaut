using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiketLaut
{
    [Table("DetailKendaraan")]
    public class DetailKendaraan
    {
        [Key]                                           // PRIMARY KEY
        public int detail_kendaraan_id { get; set; }    // integer GENERATED ALWAYS AS IDENTITY
        
        [Required]                                      // integer NOT NULL (enum as int)
        public int jenis_kendaraan { get; set; }        // Enum as integer
        
        [Required]                                      // numeric NOT NULL
        public decimal harga_kendaraan { get; set; }
        
        [Required]                                      // integer NOT NULL
        public int bobot_unit { get; set; }
        
        [Required]                                      // text NOT NULL
        public string deskripsi { get; set; } = string.Empty;
        
        [Required]                                      // text NOT NULL
        public string spesifikasi_ukuran { get; set; } = string.Empty;
        
        // Foreign key to GrupKendaraan
        [Required]
        public int grup_kendaraan_id { get; set; }
        
        // Navigation property - belongs to one grup
        [ForeignKey("grup_kendaraan_id")]
        public GrupKendaraan? GrupKendaraan { get; set; }
        
        // Method untuk membuat detail kendaraan (reusable)
        public static DetailKendaraan Create(JenisKendaraan jenis, decimal harga)
        {
            var specs = GetSpecificationByJenis(jenis);
            
            return new DetailKendaraan
            {
                jenis_kendaraan = (int)jenis,  // Cast enum to int
                harga_kendaraan = harga,
                bobot_unit = specs.Bobot,
                deskripsi = specs.Deskripsi,
                spesifikasi_ukuran = specs.SpesifikasiUkuran
            };
        }
        
        // Static method untuk mendapatkan spesifikasi dasar berdasarkan golongan ASDP
        public static (int Bobot, string Deskripsi, string SpesifikasiUkuran) GetSpecificationByJenis(JenisKendaraan jenis)
        {
            return jenis switch
            {
                JenisKendaraan.Jalan_Kaki => (0, "Pejalan kaki tanpa kendaraan", "Tidak ada kendaraan"),
                JenisKendaraan.Golongan_I => (1, "Sepeda", "Sepeda biasa"),
                JenisKendaraan.Golongan_II => (2, "Sepeda motor <500cc dan gerobak dorong", "Kapasitas mesin kurang dari 500cc"),
                JenisKendaraan.Golongan_III => (3, "Sepeda motor besar >500cc dan kendaraan roda tiga", "Kapasitas mesin lebih dari 500cc, kendaraan roda tiga"),
                JenisKendaraan.Golongan_IV_A => (4, "Mobil jeep, sedan, minibus", "Panjang sampai dengan 5 meter"),
                JenisKendaraan.Golongan_IV_B => (5, "Mobil barang bak muatan terbuka/tertutup, mobil kabin ganda", "Panjang sampai dengan 5 meter"),
                JenisKendaraan.Golongan_V_A => (6, "Mobil bus penumpang", "Panjang 5 meter sampai dengan 7 meter"),
                JenisKendaraan.Golongan_V_B => (7, "Mobil barang (truk/tangki) ukuran sedang", "Panjang 5 meter sampai dengan 7 meter"),
                JenisKendaraan.Golongan_VI_A => (8, "Mobil bus penumpang", "Panjang 7 meter sampai dengan 10 meter"),
                JenisKendaraan.Golongan_VI_B => (10, "Mobil bagang (truk/tangki) sedang dan mobil penarik tanpa gandengan", "Panjang 7 meter sampai dengan 10 meter"),
                JenisKendaraan.Golongan_VII => (12, "Mobil barang (truk) tronton, mobil tangki, mobil penarik berikut gandengan", "Panjang 10 meter sampai dengan 12 meter"),
                JenisKendaraan.Golongan_VIII => (16, "Mobil barang (truk) tronton, mobil tangki, kendaraan alat berat, mobil penarik berikut gandengan", "Panjang 12 meter sampai dengan 16 meter"),
                JenisKendaraan.Golongan_IX => (20, "Mobil barang (truk) tronton, mobil tangki, kendaraan alat berat, mobil penarik berikut gandengan", "Panjang lebih dari 16 meter"),
                _ => (1, "Kendaraan tidak dikenal", "Tidak diketahui")
            };
        }

        /// <summary>
        /// Mendapatkan kapasitas maksimal penumpang berdasarkan jenis kendaraan
        /// Sesuai dengan standar ASDP Indonesia Ferry
        /// </summary>
        public static int GetMaksimalPenumpang(JenisKendaraan jenis)
        {
            return jenis switch
            {
                JenisKendaraan.Jalan_Kaki => 100,           // Pejalan Kaki - Tidak ada batasan praktis
                JenisKendaraan.Golongan_I => 3,             // Sepeda - Maks. 3 penumpang
                JenisKendaraan.Golongan_II => 3,            // Motor <500cc - Maks. 3 penumpang
                JenisKendaraan.Golongan_III => 3,           // Motor >500cc - Maks. 3 penumpang
                JenisKendaraan.Golongan_IV_A => 10,         // Mobil Penumpang - Maks. 10 penumpang
                JenisKendaraan.Golongan_IV_B => 5,          // Truk Pickup - Maks. 5 penumpang
                JenisKendaraan.Golongan_V_A => 35,          // Bus Sedang - Maks. 35 penumpang
                JenisKendaraan.Golongan_V_B => 3,           // Truk Sedang - Maks. 3 penumpang
                JenisKendaraan.Golongan_VI_A => 65,         // Bus Besar - Maks. 65 penumpang
                JenisKendaraan.Golongan_VI_B => 3,          // Truk Besar - Maks. 3 penumpang
                JenisKendaraan.Golongan_VII => 3,           // Truk Tronton - Maks. 3 penumpang
                JenisKendaraan.Golongan_VIII => 3,          // Truk Tronton <16m - Maks. 3 penumpang
                JenisKendaraan.Golongan_IX => 3,            // Truk Tronton >16m - Maks. 3 penumpang
                _ => 1
            };
        }

        /// <summary>
        /// Mendapatkan kapasitas maksimal penumpang berdasarkan index (untuk backward compatibility)
        /// </summary>
        public static int GetMaksimalPenumpangByIndex(int jenisKendaraanIndex)
        {
            if (jenisKendaraanIndex < 0 || jenisKendaraanIndex > 12)
                return 1;
                
            return GetMaksimalPenumpang((JenisKendaraan)jenisKendaraanIndex);
        }

        // Method helper untuk backward compatibility dengan sistem lama
        public static DetailKendaraan GetDetailKendaraan(JenisKendaraan jenis)
        {
            var specs = GetSpecificationByJenis(jenis);
            return new DetailKendaraan
            {
                jenis_kendaraan = (int)jenis,  // Cast enum to int
                bobot_unit = specs.Bobot,
                deskripsi = specs.Deskripsi,
                spesifikasi_ukuran = specs.SpesifikasiUkuran,
                harga_kendaraan = 0 // Akan diset per jadwal
            };
        }

        // Property untuk backward compatibility
        public int Bobot => bobot_unit;
        public string Deskripsi => deskripsi;
        public string SpesifikasiUkuran => spesifikasi_ukuran;
        public JenisKendaraan JenisKendaraan => (JenisKendaraan)jenis_kendaraan;  // Cast int to enum

        public override string ToString()
        {
            if (jenis_kendaraan == (int)JenisKendaraan.Jalan_Kaki)  // Cast enum to int for comparison
            {
                return $"{(JenisKendaraan)jenis_kendaraan} - {deskripsi}";
            }
            return $"{(JenisKendaraan)jenis_kendaraan} (Bobot: {bobot_unit}) - {deskripsi} | {spesifikasi_ukuran}";
        }
    }
}