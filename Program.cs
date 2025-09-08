using System;

namespace KapalKlik
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== APLIKASI TIKET KAPAL LAUT ===");
            Console.WriteLine("Selamat datang di aplikasi KapalKlik!");
            
            // Contoh penggunaan class-class yang telah dibuat
            DemostrateClasses();
            
            Console.WriteLine("\nTekan sembarang tombol untuk keluar...");
            Console.ReadKey();
        }
        
        static void DemostrateClasses()
        {
            // Membuat instance Admin
            var admin = new Admin
            {
                admin_id = 1,
                nama = "Admin System",
                username = "admin",
                email = "admin@kapalklik.com",
                password = "admin123"
            };
            
            // Membuat instance Pengguna
            var pengguna = new Pengguna
            {
                pengguna_id = 1,
                nama = "John Doe",
                email = "john@email.com",
                umur = 25,
                jenis_kelamin = "Laki-laki",
                tanggal_lahir = new DateTime(1999, 5, 15),
                kewarganegaraan = "Indonesia",
                alamat = "Jakarta"
            };
            
            // Membuat instance Pelabuhan
            var pelabuhanAsal = new Pelabuhan
            {
                pelabuhan_id = 1,
                nama_pelabuhan = "Pelabuhan Merak",
                kota = "Cilegon",
                provinsi = "Banten",
                fasilitas = "Parkir, Toilet, Mushola, Kantin",
                deskripsi = "Pelabuhan utama penyeberangan Jawa-Sumatera"
            };
            
            var pelabuhanTujuan = new Pelabuhan
            {
                pelabuhan_id = 2,
                nama_pelabuhan = "Pelabuhan Bakauheni",
                kota = "Lampung Selatan",
                provinsi = "Lampung",
                fasilitas = "Parkir, Toilet, Mushola, Kantin, Ruang Tunggu",
                deskripsi = "Pelabuhan penyeberangan di ujung selatan Sumatera"
            };
            
            // Membuat instance Kapal
            var kapal = new Kapal
            {
                kapal_id = 1,
                nama_kapal = "KMP Legundi",
                kapasitas_penumpang_id = 500,
                kapasitas_kendaraan = 50,
                fasilitas = "AC, Mushola, Kantin, Ruang VIP",
                deskripsi = "Kapal ferry modern dengan fasilitas lengkap"
            };
            
            // Membuat instance Jadwal
            var jadwal = new Jadwal
            {
                jadwal_id = 1,
                pelabuhan_asal_id = pelabuhanAsal.pelabuhan_id,
                pelabuhan_tujuan_id = pelabuhanTujuan.pelabuhan_id,
                kelas = "Ekonomi",
                tanggal_berangkat = DateTime.Today.AddDays(1),
                waktu_berangkat = new TimeSpan(8, 0, 0),
                waktu_tiba = new TimeSpan(10, 30, 0),
                status = StatusTiket.Successful
            };
            
            // Membuat instance Tiket
            var tiket = new Tiket
            {
                tiket_id = 1,
                total_harga = 25000,
                tanggal_pemesanan = DateTime.Now,
                jenis_kendaraan = JenisKendaraan.Sepeda_Motor,
                status = StatusTiket.Successful
            };
            
            // Membuat instance Pembayaran
            var pembayaran = new Pembayaran
            {
                pembayaran_id = 1,
                metode_pembayaran = "Transfer Bank",
                jumlah_bayar = tiket.total_harga,
                tanggal_bayar = DateTime.Now
            };
            
            // Membuat instance Notifikasi broadcast dari admin
            Console.WriteLine("\n=== DEMO NOTIFIKASI BROADCAST ===");
            
            // Admin mengirim notifikasi broadcast umum
            admin.kirimNotifikasiBroadcast(
                "Selamat datang di KapalKlik! Nikmati kemudahan booking tiket online.", 
                JenisNotifikasi.Update
            );
            
            // Admin mengirim notifikasi perubahan jadwal
            admin.kirimNotifikasiPerubahanJadwal(jadwal, "Cuaca buruk");
            
            // Contoh notifikasi personal (bukan broadcast)
            var notifikasiPersonal = new Notifikasi
            {
                notifikasi_id = 2,
                jenis = JenisNotifikasi.Pengingatkan,
                pesan = "Jangan lupa keberangkatan Anda besok pukul 08:00",
                admin = admin,
                admin_id = admin.admin_id,
                pengguna = pengguna,
                pengguna_id = pengguna.pengguna_id,
                is_broadcast = false
            };
            notifikasiPersonal.kirimNotifikasi();
            
            // Menampilkan informasi
            Console.WriteLine($"Admin: {admin.nama} berhasil login: {admin.login()}");
            Console.WriteLine($"Pengguna: {pengguna.nama} terdaftar dengan email: {pengguna.email}");
            Console.WriteLine($"Kapal: {kapal.nama_kapal} dengan kapasitas {kapal.kapasitas_penumpang_id} penumpang");
            Console.WriteLine($"Rute: {pelabuhanAsal.nama_pelabuhan} -> {pelabuhanTujuan.nama_pelabuhan}");
            Console.WriteLine($"Jadwal: {jadwal.tanggal_berangkat:dd/MM/yyyy} pukul {jadwal.waktu_berangkat}");
            Console.WriteLine($"Tiket: {tiket.tiket_id} dengan harga Rp {tiket.total_harga:N0}");
            Console.WriteLine($"Pembayaran: {pembayaran.metode_pembayaran} sebesar Rp {pembayaran.jumlah_bayar:N0}");
            Console.WriteLine($"Notifikasi Personal: {notifikasiPersonal.pesan}");
            Console.WriteLine($"Status Broadcast: Admin dapat mengirim notifikasi ke semua pengguna");
        }
    }
}
