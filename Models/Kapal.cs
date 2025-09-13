using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Kapal
    {
        public int kapal_id { get; set; }
        public string nama_kapal { get; set; } = string.Empty;
        
        // Kapasitas maksimal kapal
        public int kapasitas_penumpang_max { get; set; }
        public int kapasitas_kendaraan_max { get; set; }
        
        // Alias untuk kompatibilitas
        public int kapasitas_penumpang
        {
            get => kapasitas_penumpang_max;
            set => kapasitas_penumpang_max = value;
        }
        
        public int kapasitas_kendaraan
        {
            get => kapasitas_kendaraan_max;
            set => kapasitas_kendaraan_max = value;
        }
        
        // Kapasitas yang sudah terpakai (tracking real-time)
        public int kapasitas_penumpang_terpakai { get; set; } = 0;
        public int kapasitas_kendaraan_terpakai { get; set; } = 0;
        
        public string fasilitas { get; set; } = string.Empty;
        public string deskripsi { get; set; } = string.Empty;

        // Properti untuk mendapatkan sisa kapasitas
        public int SisaKapasitasPenumpang => kapasitas_penumpang_max - kapasitas_penumpang_terpakai;
        public int SisaKapasitasKendaraan => kapasitas_kendaraan_max - kapasitas_kendaraan_terpakai;

        public void tampilkanInfoKapal()
        {
            Console.WriteLine($"=== INFO KAPAL {nama_kapal} ===");
            Console.WriteLine($"Kapasitas Penumpang: {kapasitas_penumpang_terpakai}/{kapasitas_penumpang_max}");
            Console.WriteLine($"Kapasitas Kendaraan: {kapasitas_kendaraan_terpakai}/{kapasitas_kendaraan_max}");
            Console.WriteLine($"Sisa Penumpang: {SisaKapasitasPenumpang}");
            Console.WriteLine($"Sisa Kendaraan: {SisaKapasitasKendaraan}");
            Console.WriteLine($"Fasilitas: {fasilitas}");
        }

        // Method untuk cek apakah bisa menampung penumpang baru
        public bool CekKapasitasPenumpang(int jumlahPenumpang)
        {
            return SisaKapasitasPenumpang >= jumlahPenumpang;
        }

        // Method untuk cek apakah bisa menampung kendaraan baru
        public bool CekKapasitasKendaraan(int bobotKendaraan)
        {
            return SisaKapasitasKendaraan >= bobotKendaraan;
        }

        // Method untuk booking penumpang
        public bool BookingPenumpang(int jumlahPenumpang)
        {
            if (CekKapasitasPenumpang(jumlahPenumpang))
            {
                kapasitas_penumpang_terpakai += jumlahPenumpang;
                return true;
            }
            return false;
        }

        // Method untuk booking kendaraan
        public bool BookingKendaraan(JenisKendaraan jenisKendaraan, int jumlah = 1)
        {
            int bobotTotal = GetBobotKendaraan(jenisKendaraan) * jumlah;
            
            if (CekKapasitasKendaraan(bobotTotal))
            {
                kapasitas_kendaraan_terpakai += bobotTotal;
                return true;
            }
            return false;
        }

        // Method untuk mendapatkan bobot kendaraan berdasarkan jenis (DEPRECATED - gunakan DetailKendaraan.GetDetailKendaraan)
        public static int GetBobotKendaraan(JenisKendaraan jenisKendaraan)
        {
            var detail = DetailKendaraan.GetDetailKendaraan(jenisKendaraan);
            return detail.Bobot;
        }

        // Method untuk cancel booking (mengembalikan kapasitas)
        public void CancelBookingPenumpang(int jumlahPenumpang)
        {
            kapasitas_penumpang_terpakai = Math.Max(0, kapasitas_penumpang_terpakai - jumlahPenumpang);
        }

        public void CancelBookingKendaraan(JenisKendaraan jenisKendaraan, int jumlah = 1)
        {
            int bobotTotal = GetBobotKendaraan(jenisKendaraan) * jumlah;
            kapasitas_kendaraan_terpakai = Math.Max(0, kapasitas_kendaraan_terpakai - bobotTotal);
        }
    }
}
