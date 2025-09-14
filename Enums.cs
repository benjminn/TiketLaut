using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    // Enum untuk jenis kendaraan sesuai golongan PT ASDP Indonesia Ferry (Persero)
    public enum JenisKendaraan
    {
        Jalan_Kaki,           // Tanpa kendaraan (pejalan kaki)
        Golongan_I,           // Sepeda
        Golongan_II,          // Sepeda motor <500cc dan gerobak dorong
        Golongan_III,         // Sepeda motor besar >500cc dan kendaraan roda tiga
        Golongan_IV_A,        // Kendaraan bermotor untuk penumpang berupa mobil jeep, sedan, minibus (panjang ≤5m)
        Golongan_IV_B,        // Mobil barang berupa mobil bak muatan terbuka, mobil bak muatan tertutup, mobil barang kabin ganda (panjang ≤5m)
        Golongan_V_A,         // Kendaraan bermotor untuk penumpang berupa mobil bus (panjang 5-7m)
        Golongan_V_B,         // Mobil barang (truk/tangki) ukuran sedang (panjang 5-7m)
        Golongan_VI_A,        // Kendaraan bermotor untuk penumpang berupa mobil bus (panjang 7-10m)
        Golongan_VI_B,        // Mobil barang (truk/tangki) ukuran sedang (panjang 7-10m) dan mobil penarik tanpa gandengan
        Golongan_VII,         // Mobil barang (truk) tronton, mobil tangki, mobil penarik berikut gandengan (panjang 10-12m)
        Golongan_VIII,        // Mobil barang (truk) tronton, mobil tangki, kendaraan alat berat, mobil penarik berikut gandengan (panjang 12-16m)
        Golongan_IX           // Mobil barang (truk) tronton, mobil tangki, kendaraan alat berat, mobil penarik berikut gandengan (panjang >16m)
    }

    // Enum untuk status tiket
    public enum StatusTiket
    {
        Tersedia,
        Successful,
        Pending,
        Cancelled
    }

    // Enum untuk jenis notifikasi
    public enum JenisNotifikasi
    {
        Info,           // Informasi umum
        Peringatan,     // Peringatan penting
        Pengingatkan,   // Pengingat jadwal, pembayaran, dll
        Update,         // Update jadwal, status tiket
        Status,         // Perubahan status tiket
        Pembayaran,     // Notifikasi terkait pembayaran
        Jadwal,         // Perubahan jadwal keberangkatan
        Pembatalan      // Pembatalan jadwal atau tiket
    }
}