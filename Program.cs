//using System;

//namespace TiketLaut
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            Console.WriteLine("=== APLIKASI TIKET KAPAL LAUT ===");
//            Console.WriteLine("Selamat datang di aplikasi KapalKlik!");
//            Console.WriteLine();

//            try
//            {
//                // Demo penggunaan models yang sudah disesuaikan dengan schema
//                DemoModels();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error: {ex.Message}");
//            }

//            Console.WriteLine("\nTekan any key untuk keluar...");
//            Console.ReadKey();
//        }

//        static void DemoModels()
//        {
//            // Membuat instance Admin
//            var admin = new Admin
//            {
//                admin_id = 1,
//                nama = "Admin System",
//                username = "admin",
//                email = "admin@kapalklik.com",
//                password = "admin123",
//                role = "SuperAdmin"
//            };
            
//            // Membuat instance Pengguna
//            var pengguna = new Pengguna
//            {
//                pengguna_id = 1,
//                nama = "John Doe",
//                email = "john@email.com",
//                password = "password123",
//                jenis_kelamin = "Laki-laki",
//                tanggal_lahir = new DateOnly(1999, 5, 15),
//                kewarganegaraan = "Indonesia",
//                no_hp = "081234567890",
//                alamat = "Jakarta"
//            };
            
//            // Membuat instance Pelabuhan Asal
//            var pelabuhanAsal = new Pelabuhan
//            {
//                pelabuhan_id = 1,
//                nama_pelabuhan = "Pelabuhan Merak",
//                kota = "Cilegon",
//                provinsi = "Banten",
//                fasilitas = "Terminal penumpang, parkir kendaraan, mushola",
//                deskripsi = "Pelabuhan utama penyeberangan Jawa-Sumatera"
//            };
            
//            // Membuat instance Pelabuhan Tujuan
//            var pelabuhanTujuan = new Pelabuhan
//            {
//                pelabuhan_id = 2,
//                nama_pelabuhan = "Pelabuhan Bakauheni",
//                kota = "Lampung Selatan",
//                provinsi = "Lampung",
//                fasilitas = "Terminal penumpang, area tunggu ber-AC, food court",
//                deskripsi = "Pintu gerbang Pulau Sumatera"
//            };
            
//            // Membuat instance Kapal
//            var kapal = new Kapal
//            {
//                kapal_id = 1,
//                nama_kapal = "KMP Legundi",
//                kapasitas_penumpang_max = 500,
//                kapasitas_kendaraan_max = 50,
//                fasilitas = "AC, Mushola, Kantin, Ruang VIP",
//                deskripsi = "Kapal ferry modern dengan fasilitas lengkap"
//            };
            
//            // Membuat instance Jadwal
//            var jadwal = new Jadwal
//            {
//                jadwal_id = 1,
//                pelabuhan_asal_id = pelabuhanAsal.pelabuhan_id,
//                pelabuhan_tujuan_id = pelabuhanTujuan.pelabuhan_id,
//                kapal_id = kapal.kapal_id,
//                waktu_berangkat = new TimeOnly(8, 0, 0),
//                waktu_tiba = new TimeOnly(10, 30, 0),
//                sisa_kapasitas_penumpang = 450, // Max 500, sudah terisi 50
//                sisa_kapasitas_kendaraan = 48,  // Max 50, sudah terisi 2
//                status = "Active"
//            };
            
//            // Membuat instance DetailKendaraan untuk jadwal
//            var detailKendaraan = new DetailKendaraan
//            {
//                detail_kendaraan_id = 1,
//                jadwal_id = jadwal.jadwal_id,
//                jenis_kendaraan = (int)JenisKendaraan.Jalan_Kaki,
//                harga_kendaraan = 150000,
//                bobot_unit = 1,
//                deskripsi = "Penumpang pejalan kaki",
//                spesifikasi_ukuran = "1 orang dewasa"
//            };
            
//            // Membuat instance Tiket
//            var tiket = new Tiket
//            {
//                tiket_id = 1,
//                pengguna_id = pengguna.pengguna_id,
//                jadwal_id = jadwal.jadwal_id,
//                total_harga = 150000,
//                tanggal_pemesanan = DateTime.Now,
//                jumlah_penumpang = 1,
//                jenis_kendaraan_enum = "Jalan Kaki",
//                status_tiket = "Booked",
//                kode_tiket = "TKT20240101000001"
//            };

//            // Membuat instance Pembayaran
//            var pembayaran = new Pembayaran
//            {
//                pembayaran_id = 1,
//                tiket_id = tiket.tiket_id,
//                metode_pembayaran = "Transfer Bank",
//                jumlah_bayar = tiket.total_harga,
//                tanggal_bayar = DateTime.Now,
//                status_bayar = "Confirmed"
//            };

//            // Demo output
//            Console.WriteLine("=== DEMO APLIKASI TIKET KAPAL LAUT ===");
//            Console.WriteLine();

//            pengguna.tampilkanProfil();
//            Console.WriteLine();

//            pelabuhanAsal.tampilkanInfoPelabuhan();
//            Console.WriteLine();

//            pelabuhanTujuan.tampilkanInfoPelabuhan();
//            Console.WriteLine();

//            kapal.tampilkanInfoKapal();
//            Console.WriteLine();

//            jadwal.tampilkanDetailJadwal();
//            Console.WriteLine();

//            tiket.tampilkanDetailTiket();
//            Console.WriteLine();

//            Console.WriteLine($"Kapal: {kapal.nama_kapal} dengan kapasitas {kapal.kapasitas_penumpang_max} penumpang");
//            Console.WriteLine($"Rute: {pelabuhanAsal.nama_pelabuhan} → {pelabuhanTujuan.nama_pelabuhan}");
//            Console.WriteLine($"Status Pembayaran: {pembayaran.status_bayar}");
//            Console.WriteLine();
//            Console.WriteLine("=== SEMUA MODELS BERHASIL DISESUAIKAN DENGAN SCHEMA ===");
//        }
//    }
//}