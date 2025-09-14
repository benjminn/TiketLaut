using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Jadwal
    {
        public int jadwal_id { get; set; } // Primary Key
        public int pelabuhan_asal_id { get; set; } // Foreign Key ke Pelabuhan
        public int pelabuhan_tujuan_id { get; set; } // Foreign Key ke Pelabuhan 
        public int kapal_id { get; set; } // Foreign Key ke Kapal
        public string kelas { get; set; } = string.Empty;
        public DateTime tanggal_berangkat { get; set; }
        public TimeSpan waktu_berangkat { get; set; }
        public TimeSpan waktu_tiba { get; set; }
        public StatusTiket status { get; set; }
        
        // Harga tiket untuk pejalan kaki (per penumpang)
        public decimal harga_penumpang { get; set; }
        
        // Harga tiket berdasarkan golongan kendaraan (sudah termasuk penumpang)
        public decimal harga_golongan_I { get; set; }       // Sepeda
        public decimal harga_golongan_II { get; set; }      // Motor <500cc
        public decimal harga_golongan_III { get; set; }     // Motor >500cc, roda 3
        public decimal harga_golongan_IV_A { get; set; }    // Mobil penumpang ≤5m
        public decimal harga_golongan_IV_B { get; set; }    // Mobil barang ≤5m
        public decimal harga_golongan_V_A { get; set; }     // Bus 5-7m
        public decimal harga_golongan_V_B { get; set; }     // Truk 5-7m
        public decimal harga_golongan_VI_A { get; set; }    // Bus 7-10m
        public decimal harga_golongan_VI_B { get; set; }    // Truk 7-10m
        public decimal harga_golongan_VII { get; set; }     // Truk tronton 10-12m
        public decimal harga_golongan_VIII { get; set; }    // Truk tronton 12-16m
        public decimal harga_golongan_IX { get; set; }      // Truk tronton >16m

        // Navigation properties
        public Pelabuhan pelabuhan_asal { get; set; } = null!;
        public Pelabuhan pelabuhan_tujuan { get; set; } = null!;
        public Kapal kapal { get; set; } = null!;
        
        // Collection DetailKendaraan untuk jadwal ini - HUBUNGAN UTAMA!
        public List<DetailKendaraan> detail_kendaraans { get; set; } = new List<DetailKendaraan>();

        // Calculated properties
        public TimeSpan DurasiPerjalanan => waktu_tiba - waktu_berangkat;
        public int SisaKapasitasPenumpang => kapal?.kapasitas_penumpang_max ?? 0;
        public int SisaKapasitasKendaraan => kapal?.kapasitas_kendaraan_max ?? 0;

        // Method untuk mendapatkan harga berdasarkan jenis kendaraan
        public decimal GetHargaByJenisKendaraan(JenisKendaraan jenisKendaraan)
        {
            return jenisKendaraan switch
            {
                JenisKendaraan.Jalan_Kaki => harga_penumpang,
                JenisKendaraan.Golongan_I => harga_golongan_I,
                JenisKendaraan.Golongan_II => harga_golongan_II,
                JenisKendaraan.Golongan_III => harga_golongan_III,
                JenisKendaraan.Golongan_IV_A => harga_golongan_IV_A,
                JenisKendaraan.Golongan_IV_B => harga_golongan_IV_B,
                JenisKendaraan.Golongan_V_A => harga_golongan_V_A,
                JenisKendaraan.Golongan_V_B => harga_golongan_V_B,
                JenisKendaraan.Golongan_VI_A => harga_golongan_VI_A,
                JenisKendaraan.Golongan_VI_B => harga_golongan_VI_B,
                JenisKendaraan.Golongan_VII => harga_golongan_VII,
                JenisKendaraan.Golongan_VIII => harga_golongan_VIII,
                JenisKendaraan.Golongan_IX => harga_golongan_IX,
                _ => 0
            };
        }

        public void tampilkanJadwal()
        {
            Console.WriteLine($"=== JADWAL {jadwal_id} ===");
            Console.WriteLine($"Kapal: {kapal?.nama_kapal}");
            Console.WriteLine($"Rute: {pelabuhan_asal?.nama_pelabuhan} → {pelabuhan_tujuan?.nama_pelabuhan}");
            Console.WriteLine($"Tanggal: {tanggal_berangkat:dd/MM/yyyy}");
            Console.WriteLine($"Waktu: {waktu_berangkat} - {waktu_tiba}");
            Console.WriteLine($"Durasi: {DurasiPerjalanan}");
            Console.WriteLine($"Kelas: {kelas}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine("\n📋 DAFTAR HARGA:");
            Console.WriteLine($"  Pejalan Kaki: Rp {harga_penumpang:N0}");
            Console.WriteLine($"  Golongan I (Sepeda): Rp {harga_golongan_I:N0}");
            Console.WriteLine($"  Golongan II (Motor <500cc): Rp {harga_golongan_II:N0}");
            Console.WriteLine($"  Golongan III (Motor >500cc): Rp {harga_golongan_III:N0}");
            Console.WriteLine($"  Golongan IV-A (Mobil ≤5m): Rp {harga_golongan_IV_A:N0}");
            Console.WriteLine($"  Golongan IV-B (Truk kecil ≤5m): Rp {harga_golongan_IV_B:N0}");
            Console.WriteLine($"  Golongan V-A (Bus 5-7m): Rp {harga_golongan_V_A:N0}");
            Console.WriteLine($"  Golongan V-B (Truk 5-7m): Rp {harga_golongan_V_B:N0}");
            Console.WriteLine($"  Golongan VI-A (Bus 7-10m): Rp {harga_golongan_VI_A:N0}");
            Console.WriteLine($"  Golongan VI-B (Truk 7-10m): Rp {harga_golongan_VI_B:N0}");
            Console.WriteLine($"  Golongan VII (Truk 10-12m): Rp {harga_golongan_VII:N0}");
            Console.WriteLine($"  Golongan VIII (Truk 12-16m): Rp {harga_golongan_VIII:N0}");
            Console.WriteLine($"  Golongan IX (Truk >16m): Rp {harga_golongan_IX:N0}");
            Console.WriteLine($"\n🚢 Sisa Kapasitas Penumpang: {SisaKapasitasPenumpang}");
            Console.WriteLine($"📦 Sisa Kapasitas Kendaraan: {SisaKapasitasKendaraan}");
        }

        // Method untuk initialize DetailKendaraan berdasarkan pricing jadwal ini
        public void InitializeDetailKendaraan()
        {
            detail_kendaraans.Clear();
            
            // Create DetailKendaraan untuk setiap jenis kendaraan dengan harga dari jadwal ini
            var jenisKendaraanList = Enum.GetValues<JenisKendaraan>();
            
            foreach (var jenis in jenisKendaraanList)
            {
                var harga = GetHargaByJenisKendaraan(jenis);
                var detail = DetailKendaraan.CreateForJadwal(jadwal_id, jenis, harga);
                detail_kendaraans.Add(detail);
            }
        }
        
        // Method untuk mendapatkan DetailKendaraan spesifik untuk jenis kendaraan di jadwal ini
        public DetailKendaraan? GetDetailKendaraanByJenis(JenisKendaraan jenis)
        {
            return detail_kendaraans.FirstOrDefault(dk => dk.jenis_kendaraan == jenis);
        }
        
        // Method untuk update harga kendaraan tertentu di jadwal ini
        public void UpdateHargaKendaraan(JenisKendaraan jenis, decimal hargaBaru)
        {
            // Update harga di property jadwal
            switch (jenis)
            {
                case JenisKendaraan.Jalan_Kaki: harga_penumpang = hargaBaru; break;
                case JenisKendaraan.Golongan_I: harga_golongan_I = hargaBaru; break;
                case JenisKendaraan.Golongan_II: harga_golongan_II = hargaBaru; break;
                case JenisKendaraan.Golongan_III: harga_golongan_III = hargaBaru; break;
                case JenisKendaraan.Golongan_IV_A: harga_golongan_IV_A = hargaBaru; break;
                case JenisKendaraan.Golongan_IV_B: harga_golongan_IV_B = hargaBaru; break;
                case JenisKendaraan.Golongan_V_A: harga_golongan_V_A = hargaBaru; break;
                case JenisKendaraan.Golongan_V_B: harga_golongan_V_B = hargaBaru; break;
                case JenisKendaraan.Golongan_VI_A: harga_golongan_VI_A = hargaBaru; break;
                case JenisKendaraan.Golongan_VI_B: harga_golongan_VI_B = hargaBaru; break;
                case JenisKendaraan.Golongan_VII: harga_golongan_VII = hargaBaru; break;
                case JenisKendaraan.Golongan_VIII: harga_golongan_VIII = hargaBaru; break;
                case JenisKendaraan.Golongan_IX: harga_golongan_IX = hargaBaru; break;
            }
            
            // Update harga di DetailKendaraan collection
            var detail = GetDetailKendaraanByJenis(jenis);
            if (detail != null)
            {
                detail.harga_kendaraan = hargaBaru;
            }
        }

        public TimeSpan getDurasiPerjalanan()
        {
            return DurasiPerjalanan;
        }

        public bool konfirmasiKetersediaan(int jumlahPenumpang = 1, JenisKendaraan? jenisKendaraan = null)
        {
            if (kapal == null) return false;

            // Untuk pejalan kaki, cek kapasitas penumpang
            if (!jenisKendaraan.HasValue || jenisKendaraan == JenisKendaraan.Jalan_Kaki)
            {
                return jumlahPenumpang <= (kapal?.kapasitas_penumpang_max ?? 0);
            }

            // Untuk kendaraan, cek kapasitas kendaraan berdasarkan bobot
            var detailKendaraan = DetailKendaraan.GetDetailKendaraan(jenisKendaraan.Value);
            return detailKendaraan.Bobot <= (kapal?.kapasitas_kendaraan_max ?? 0);
        }

        // Method untuk mendapatkan estimasi harga total
        public decimal EstimasiHargaTotal(int jumlahPenumpang, JenisKendaraan? jenisKendaraan = null)
        {
            decimal total = 0;

            // Jika ada kendaraan, hitung berdasarkan golongan kendaraan (sudah include semua penumpang)
            if (jenisKendaraan.HasValue && jenisKendaraan != JenisKendaraan.Jalan_Kaki)
            {
                total = GetHargaByJenisKendaraan(jenisKendaraan.Value);
            }
            else
            {
                // Jika jalan kaki, hitung berdasarkan jumlah penumpang
                total = harga_penumpang * jumlahPenumpang;
            }

            return total;
        }

        // Method untuk set harga standar (helper untuk demo)
        public void SetHargaStandar()
        {
            harga_penumpang = 15000;
            harga_golongan_I = 20000;      // Sepeda
            harga_golongan_II = 25000;     // Motor <500cc  
            harga_golongan_III = 30000;    // Motor >500cc
            harga_golongan_IV_A = 85000;   // Mobil ≤5m
            harga_golongan_IV_B = 95000;   // Truk kecil ≤5m
            harga_golongan_V_A = 125000;   // Bus 5-7m
            harga_golongan_V_B = 145000;   // Truk 5-7m
            harga_golongan_VI_A = 165000;  // Bus 7-10m
            harga_golongan_VI_B = 185000;  // Truk 7-10m
            harga_golongan_VII = 225000;   // Truk 10-12m
            harga_golongan_VIII = 285000;  // Truk 12-16m
            harga_golongan_IX = 345000;    // Truk >16m
        }

        // Method untuk validasi jadwal
        public bool ValidasiJadwal()
        {
            // Validasi basic
            if (tanggal_berangkat < DateTime.Today)
            {
                Console.WriteLine("Tanggal keberangkatan tidak boleh di masa lalu");
                return false;
            }

            if (waktu_tiba <= waktu_berangkat)
            {
                Console.WriteLine("Waktu tiba harus setelah waktu berangkat");
                return false;
            }

            if (pelabuhan_asal_id == pelabuhan_tujuan_id)
            {
                Console.WriteLine("Pelabuhan asal dan tujuan tidak boleh sama");
                return false;
            }

            if (harga_penumpang <= 0)
            {
                Console.WriteLine("Harga tiket harus lebih dari 0");
                return false;
            }

            return true;
        }
    }
}
