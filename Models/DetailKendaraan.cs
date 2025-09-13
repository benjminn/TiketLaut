using System;
using System.Collections.Generic;

namespace TiketLaut
{
    // Class untuk detail kendaraan per jadwal - harga berbeda per rute
    public class DetailKendaraan
    {
        public int detail_kendaraan_id { get; set; }
        
        // Foreign Key ke Jadwal (BUKAN ke Tiket!)
        public int jadwal_id { get; set; }
        public Jadwal jadwal { get; set; } = null!;
        
        // Jenis kendaraan
        public JenisKendaraan jenis_kendaraan { get; set; }
        
        // Harga untuk jenis kendaraan ini di jadwal ini
        public decimal harga_kendaraan { get; set; }
        
        // Bobot dan spesifikasi
        public int bobot_unit { get; set; }
        public string deskripsi { get; set; } = string.Empty;
        public string spesifikasi_ukuran { get; set; } = string.Empty;
        
        // Method untuk membuat detail kendaraan per jadwal
        public static DetailKendaraan CreateForJadwal(int jadwalId, JenisKendaraan jenis, decimal harga)
        {
            var specs = GetSpecificationByJenis(jenis);
            
            return new DetailKendaraan
            {
                jadwal_id = jadwalId,
                jenis_kendaraan = jenis,
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
                JenisKendaraan.Golongan_VI_B => (10, "Mobil barang (truk/tangki) sedang dan mobil penarik tanpa gandengan", "Panjang 7 meter sampai dengan 10 meter"),
                JenisKendaraan.Golongan_VII => (12, "Mobil barang (truk) tronton, mobil tangki, mobil penarik berikut gandengan", "Panjang 10 meter sampai dengan 12 meter"),
                JenisKendaraan.Golongan_VIII => (16, "Mobil barang (truk) tronton, mobil tangki, kendaraan alat berat, mobil penarik berikut gandengan", "Panjang 12 meter sampai dengan 16 meter"),
                JenisKendaraan.Golongan_IX => (20, "Mobil barang (truk) tronton, mobil tangki, kendaraan alat berat, mobil penarik berikut gandengan", "Panjang lebih dari 16 meter"),
                _ => (1, "Kendaraan tidak dikenal", "Tidak diketahui")
            };
        }

        // Method helper untuk backward compatibility dengan sistem lama
        public static DetailKendaraan GetDetailKendaraan(JenisKendaraan jenis)
        {
            var specs = GetSpecificationByJenis(jenis);
            return new DetailKendaraan
            {
                jenis_kendaraan = jenis,
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
        public JenisKendaraan JenisKendaraan => jenis_kendaraan;

        public override string ToString()
        {
            if (jenis_kendaraan == JenisKendaraan.Jalan_Kaki)
            {
                return $"{jenis_kendaraan} - {deskripsi}";
            }
            return $"{jenis_kendaraan} (Bobot: {bobot_unit}) - {deskripsi} | {spesifikasi_ukuran}";
        }
    }

    // Class untuk tracking kendaraan yang di-booking di tiket
    public class BookingKendaraan
    {
        public int booking_kendaraan_id { get; set; }
        public int tiket_id { get; set; }
        public JenisKendaraan jenis_kendaraan { get; set; }
        public int jumlah { get; set; } = 1;
        public int total_bobot { get; set; }
        public string plat_nomor { get; set; } = string.Empty;
        public string merk_kendaraan { get; set; } = string.Empty;

        // Navigation properties
        public Tiket tiket { get; set; } = null!;

        // Method untuk menghitung total bobot (tanpa tarif karena sekarang per jadwal)
        public void HitungTotalBobot()
        {
            var detail = DetailKendaraan.GetDetailKendaraan(jenis_kendaraan);
            total_bobot = detail.Bobot * jumlah;
        }

        public override string ToString()
        {
            var detail = DetailKendaraan.GetDetailKendaraan(jenis_kendaraan);
            return $"{jenis_kendaraan} x{jumlah} - Bobot: {total_bobot} | {detail.SpesifikasiUkuran}";
        }
    }
}