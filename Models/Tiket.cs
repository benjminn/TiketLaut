using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Tiket
    {
        public int tiket_id { get; set; } // Primary Key
        public int jadwal_id { get; set; } // Foreign Key ke Jadwal
        public double total_harga { get; set; }
        public DateTime tanggal_pemesanan { get; set; }
        public StatusTiket status { get; set; }

        // Properties kendaraan - digabung dari BookingKendaraan
        public JenisKendaraan? jenis_kendaraan_enum { get; set; } // null = jalan kaki
        public string? plat_nomor { get; set; } = string.Empty;

        // Navigation properties
        public Jadwal jadwal { get; set; } = null!;
        public List<RincianPenumpang> rincianPenumpangs { get; set; } = new List<RincianPenumpang>();

        // Computed properties berdasarkan RincianPenumpang dan kendaraan
        public int JumlahPenumpang => rincianPenumpangs?.Count ?? 0;
        public int PenggunaId => rincianPenumpangs?.FirstOrDefault()?.penumpang?.pengguna_id ?? 0;
        public Pengguna pengguna { get; set; } = null!;
        public bool AdaKendaraan => jenis_kendaraan_enum.HasValue && jenis_kendaraan_enum != JenisKendaraan.Jalan_Kaki;
        public string JenisKendaraanDisplay => jenis_kendaraan_enum?.ToString() ?? "Jalan Kaki";

        public bool buatTiket()
        {
            // Validasi sederhana - hanya set status dan tanggal
            status = StatusTiket.Pending;
            tanggal_pemesanan = DateTime.Now;
            HitungTotalHarga();
            Console.WriteLine("Tiket berhasil dibuat!");
            return true;
        }

        // Method untuk menghitung total harga berdasarkan jenis kendaraan atau penumpang
        public void HitungTotalHarga()
        {
            if (jadwal == null)
            {
                total_harga = 0;
                return;
            }

            decimal totalHarga = 0;

            // Jika ada kendaraan, hitung berdasarkan jenis kendaraan
            if (AdaKendaraan && jenis_kendaraan_enum.HasValue)
            {
                totalHarga = jadwal.GetHargaByJenisKendaraan(jenis_kendaraan_enum.Value);
            }
            else
            {
                // Jika jalan kaki, hitung berdasarkan jumlah penumpang
                totalHarga = jadwal.harga_penumpang * JumlahPenumpang;
            }

            total_harga = (double)totalHarga;
        }

        // Method untuk set kendaraan pada tiket
        public void SetKendaraan(JenisKendaraan jenisKendaraan, string platNomor = "")
        {
            jenis_kendaraan_enum = jenisKendaraan;
            plat_nomor = platNomor;
            HitungTotalHarga(); // Recalculate harga setelah set kendaraan
        }

        // Method untuk set sebagai pejalan kaki
        public void SetJalanKaki()
        {
            jenis_kendaraan_enum = JenisKendaraan.Jalan_Kaki;
            plat_nomor = string.Empty;
            HitungTotalHarga(); // Recalculate harga
        }

        public void tampilkanDetailTiket()
        {
            Console.WriteLine($"=== DETAIL TIKET {tiket_id} ===");
            Console.WriteLine($"Tanggal Pemesanan: {tanggal_pemesanan:dd/MM/yyyy HH:mm}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Jumlah Penumpang: {JumlahPenumpang}");

            // Tampilkan detail kendaraan jika ada
            if (AdaKendaraan && jenis_kendaraan_enum.HasValue)
            {
                var harga = jadwal?.GetHargaByJenisKendaraan(jenis_kendaraan_enum.Value) ?? 0;
                var detail = DetailKendaraan.GetDetailKendaraan(jenis_kendaraan_enum.Value);

                Console.WriteLine("?? KENDARAAN:");
                Console.WriteLine($"  - Jenis: {jenis_kendaraan_enum}");
                Console.WriteLine($"  - Plat Nomor: {plat_nomor}");
                Console.WriteLine($"  - Bobot: {detail.Bobot}");
                Console.WriteLine($"  - Spesifikasi: {detail.SpesifikasiUkuran}");
                Console.WriteLine($"  - Harga: Rp {harga:N0}");
            }
            else
            {
                Console.WriteLine("?? Jenis: Pejalan Kaki");
            }

            Console.WriteLine($"Total Harga: Rp {total_harga:N0}");

            if (jadwal != null)
            {
                Console.WriteLine($"Kelas: {jadwal.kelas}");
                Console.WriteLine($"Tanggal Keberangkatan: {jadwal.tanggal_berangkat:dd/MM/yyyy}");
                Console.WriteLine($"Jam Keberangkatan: {jadwal.waktu_berangkat}");
            }

            if (pengguna != null)
            {
                Console.WriteLine($"Pemesan: {pengguna.nama}");
            }
        }

        public bool konfirmasiTiket()
        {
            if (status == StatusTiket.Pending)
            {
                status = StatusTiket.Successful;
                Console.WriteLine("Tiket berhasil dikonfirmasi!");
                return true;
            }

            Console.WriteLine("Tiket tidak bisa dikonfirmasi!");
            return false;
        }

        public bool batalkanTiket()
        {
            if (status == StatusTiket.Pending || status == StatusTiket.Successful)
            {
                status = StatusTiket.Cancelled;
                Console.WriteLine("Tiket berhasil dibatalkan!");
                return true;
            }

            Console.WriteLine("Tiket tidak bisa dibatalkan!");
            return false;
        }

        public void tampilkanNotifikasi()
        {
            Console.WriteLine($"Notifikasi untuk tiket {tiket_id}:");
            Console.WriteLine($"Status saat ini: {status}");

            switch (status)
            {
                case StatusTiket.Pending:
                    Console.WriteLine("Tiket Anda sedang menunggu konfirmasi pembayaran.");
                    break;
                case StatusTiket.Successful:
                    Console.WriteLine("Tiket Anda telah dikonfirmasi. Silakan datang sesuai jadwal.");
                    break;
                case StatusTiket.Cancelled:
                    Console.WriteLine("Tiket Anda telah dibatalkan.");
                    break;
                case StatusTiket.Tersedia:
                    Console.WriteLine("Tiket masih tersedia untuk dipesan.");
                    break;
            }
        }
    }
}