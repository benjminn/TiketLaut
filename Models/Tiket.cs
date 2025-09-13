using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiketLaut
{
    public class Tiket
    {
        public int tiket_id { get; set; }
        public double total_harga { get; private set; }
        public DateTime tanggal_pemesanan { get; set; }
        public string jenis_kendaraan { get; set; } = string.Empty;
        public StatusTiket status { get; set; }
        
        // Data penumpang
        public int jumlah_penumpang { get; set; } = 1;
        
        // Navigation properties
        public List<BookingKendaraan> booking_kendaraans { get; set; } = new List<BookingKendaraan>();
        
        // Foreign keys
        public int jadwal_id { get; set; }
        public int pengguna_id { get; set; }
        
        // Navigation properties
        public Jadwal jadwal { get; set; } = null!;
        public Pengguna pengguna { get; set; } = null!;

        public bool buatTiket()
        {
            // Validasi kapasitas kapal
            if (jadwal?.kapal != null)
            {
                // Cek kapasitas penumpang
                if (!jadwal.kapal.CekKapasitasPenumpang(jumlah_penumpang))
                {
                    Console.WriteLine("Kapasitas penumpang tidak mencukupi!");
                    return false;
                }

                // Cek kapasitas kendaraan
                int totalBobotKendaraan = booking_kendaraans.Sum(bk => bk.total_bobot);
                if (totalBobotKendaraan > 0 && !jadwal.kapal.CekKapasitasKendaraan(totalBobotKendaraan))
                {
                    Console.WriteLine("Kapasitas kendaraan tidak mencukupi!");
                    return false;
                }

                // Book kapasitas jika tersedia
                if (jadwal.kapal.BookingPenumpang(jumlah_penumpang))
                {
                    foreach (var bookingKendaraan in booking_kendaraans)
                    {
                        if (!jadwal.kapal.BookingKendaraan(bookingKendaraan.jenis_kendaraan, bookingKendaraan.jumlah))
                        {
                            // Rollback jika gagal booking kendaraan
                            jadwal.kapal.CancelBookingPenumpang(jumlah_penumpang);
                            return false;
                        }
                    }
                    
                    status = StatusTiket.Pending;
                    tanggal_pemesanan = DateTime.Now;
                    HitungTotalHarga();
                    return true;
                }
            }
            return false;
        }

        // Method untuk menambah kendaraan ke tiket
        public bool TambahKendaraan(JenisKendaraan jenisKendaraan, int jumlah = 1, string platNomor = "", string merkKendaraan = "")
        {
            var bookingKendaraan = new BookingKendaraan
            {
                tiket_id = this.tiket_id,
                jenis_kendaraan = jenisKendaraan,
                jumlah = jumlah,
                plat_nomor = platNomor,
                merk_kendaraan = merkKendaraan,
                tiket = this
            };
            
            bookingKendaraan.HitungTotalBobot();
            booking_kendaraans.Add(bookingKendaraan);
            
            HitungTotalHarga();
            return true;
        }

        // Method untuk menghitung total harga berdasarkan sistem baru
        public void HitungTotalHarga()
        {
            if (jadwal == null)
            {
                total_harga = 0;
                return;
            }

            // Jika ada kendaraan, hitung berdasarkan golongan kendaraan
            if (booking_kendaraans.Any())
            {
                total_harga = 0;
                foreach (var booking in booking_kendaraans)
                {
                    // Harga kendaraan sudah include 1 penumpang
                    total_harga += (double)jadwal.GetHargaByJenisKendaraan(booking.jenis_kendaraan) * booking.jumlah;
                }
                
                // Tambahan penumpang di atas 1 (karena harga kendaraan sudah include 1 penumpang)
                if (jumlah_penumpang > 1)
                {
                    total_harga += (double)jadwal.harga_penumpang * (jumlah_penumpang - 1);
                }
            }
            else
            {
                // Jika jalan kaki, hitung berdasarkan jumlah penumpang
                total_harga = (double)jadwal.harga_penumpang * jumlah_penumpang;
            }
        }

        public void tampilkanDetailTiket()
        {
            Console.WriteLine($"=== DETAIL TIKET {tiket_id} ===");
            Console.WriteLine($"Tanggal Pemesanan: {tanggal_pemesanan:dd/MM/yyyy HH:mm}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"Jumlah Penumpang: {jumlah_penumpang}");
            
            if (jadwal != null)
            {
                Console.WriteLine($"Kelas: {jadwal.kelas}");
                Console.WriteLine($"Tanggal Keberangkatan: {jadwal.tanggal_berangkat:dd/MM/yyyy}");
                Console.WriteLine($"Jam Keberangkatan: {jadwal.waktu_berangkat}");
            }
            
            if (booking_kendaraans.Any())
            {
                Console.WriteLine("Kendaraan:");
                foreach (var booking in booking_kendaraans)
                {
                    var detail = DetailKendaraan.GetDetailKendaraan(booking.jenis_kendaraan);
                    decimal hargaKendaraan = jadwal?.GetHargaByJenisKendaraan(booking.jenis_kendaraan) ?? 0;
                    
                    Console.WriteLine($"  - {detail.Deskripsi} (x{booking.jumlah})");
                    Console.WriteLine($"    Harga: Rp {hargaKendaraan:N0} (sudah termasuk 1 penumpang)");
                }
                
                // Tambahan penumpang
                if (jumlah_penumpang > 1)
                {
                    Console.WriteLine($"Tambahan Penumpang: {jumlah_penumpang - 1} orang");
                    Console.WriteLine($"Harga per penumpang: Rp {jadwal?.harga_penumpang ?? 0:N0}");
                }
            }
            else
            {
                Console.WriteLine($"Penumpang Jalan Kaki: {jumlah_penumpang} orang");
                Console.WriteLine($"Harga per penumpang: Rp {jadwal?.harga_penumpang ?? 0:N0}");
            }
            
            Console.WriteLine($"TOTAL HARGA: Rp {total_harga:N0}");
        }

        public void cetakTiket()
        {
            Console.WriteLine("=== TIKET KAPAL LAUT ===");
            tampilkanDetailTiket();
            Console.WriteLine("Simpan tiket ini sebagai bukti pembayaran.");
        }

        public void konfirmasiPembayaran()
        {
            if (status == StatusTiket.Pending)
            {
                status = StatusTiket.Successful;
                Console.WriteLine($"Pembayaran tiket {tiket_id} telah dikonfirmasi.");
            }
        }

        public void batalkanTiket()
        {
            if (status != StatusTiket.Cancelled && jadwal?.kapal != null)
            {
                // Kembalikan kapasitas kapal
                jadwal.kapal.CancelBookingPenumpang(jumlah_penumpang);
                
                foreach (var booking in booking_kendaraans)
                {
                    jadwal.kapal.CancelBookingKendaraan(booking.jenis_kendaraan, booking.jumlah);
                }
                
                status = StatusTiket.Cancelled;
                Console.WriteLine($"Tiket {tiket_id} telah dibatalkan.");
            }
        }
    }
}
