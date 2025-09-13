# 🚢 KapalKlik - Aplikasi Tiket Kapal Laut

## 👥 Tim Pengembang - Ikan Asap Pak Be
- **Ketua Kelompok:** Benjamin Sigit - 23/514737/TK/56513
- **Anggota 1:** Rafeyfa Asyla - 23/512856/TK/56361  
- **Anggota 2:** Benjamin Sigit - 23/514737/TK/565133
- **Anggota 3:** Chaira Nastya Warestri - 23/514942/TK/56550

Nggak perlu lagi antre panjang di pelabuhan atau bingung cari jadwal kapal. Dengan KapalKlik (TiketLaut), kamu bisa cek jadwal, pilih rute, booking tiket, sampai bayar, all in one click!

## 📋 Deskripsi Aplikasi
**Nama Aplikasi:** KapalKlik (Tiket Kapal Laut)  
**Kategori:** Ticketing  
**Tipe Aplikasi:** WPF (Console App untuk demo)  
**Repository:** https://github.com/benjminn/TiketLaut

## 🎯 Permasalahan yang Dipecahkan
1. **Antre Panjang:** Penumpang harus antre lama di pelabuhan untuk membeli tiket secara langsung
2. **Kurang Informasi:** Kurangnya informasi tentang lokasi pelabuhan, akses transportasi, dan fasilitas pendukung
3. **Update Real-time:** Penumpang tidak mendapatkan informasi cepat tentang keterlambatan atau perubahan jadwal
4. **Reminder:** Banyak penumpang yang lupa jadwal keberangkatan

## ✨ Solusi & Fitur
1. **🎫 Pemesanan Tiket Online:** Pembelian tiket dari mana saja tanpa antre, dengan pilihan jadwal, pelabuhan asal & tujuan, harga, kelas layanan, serta pemilihan kursi real-time
2. **ℹ️ Informasi Pelabuhan:** Menyediakan foto lokasi pelabuhan, deskripsi fasilitas, serta informasi lengkap mengenai akses transportasi
3. **📱 Status & Pemberitahuan:** Admin mengirim notifikasi broadcast kepada semua pengguna ketika terjadi perubahan jadwal atau informasi penting lainnya. Sistem juga memberikan pengingat otomatis sebelum keberangkatan

## 🏗️ Struktur Class Diagram

### 📁 Models (Classes)

#### 👨‍💼 Admin
```csharp
public class Admin
{
    public int admin_id { get; set; }
    public string nama { get; set; }
    public string username { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    
    // Methods
    public bool login()
    public void kelolaPembayaran()
    public void kelolaTiket()
    public void kelolaJadwal()
    public void kelolaPengguna()
    public void kelolaKapal()
    public void kelolaPelabuhan()
    public void kirimNotifikasiBroadcast()
    public void kirimNotifikasiPerubahanJadwal()
}
```

#### 👤 Pengguna
```csharp
public class Pengguna
{
    public int pengguna_id { get; set; }
    public string nama { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public int umur { get; set; }
    public string jenis_kelamin { get; set; }
    public DateTime tanggal_lahir { get; set; }
    public string kewarganegaraan { get; set; }
    public string alamat { get; set; }
    public DateTime tanggal_daftar { get; set; }
    public DateTime registrasi { get; set; }
    
    // Methods
    public bool login()
    public void editProfil()
    public void lihatPemberitahuan()
    public void cariJadwal()
    public void pesanTiket()
    public void lihatRiwayatTiket()
}
```

#### 🚢 Kapal
```csharp
public class Kapal
{
    public int kapal_id { get; set; }
    public string nama_kapal { get; set; }
    public int kapasitas_penumpang_id { get; set; }
    public int kapasitas_kendaraan { get; set; }
    public string fasilitas { get; set; }
    public string deskripsi { get; set; }
    
    // Methods
    public void tampilkanInfoKapal()
}
```

#### 🏢 Pelabuhan
```csharp
public class Pelabuhan
{
    public int pelabuhan_id { get; set; }
    public string nama_pelabuhan { get; set; }
    public string kota { get; set; }
    public string provinsi { get; set; }
    public string fasilitas { get; set; }
    public string deskripsi { get; set; }
    
    // Methods
    public void tampilkanInfoPelabuhan()
}
```

#### 📅 Jadwal
```csharp
public class Jadwal
{
    public int jadwal_id { get; set; }
    public int pelabuhan_asal_id { get; set; }
    public int pelabuhan_tujuan_id { get; set; }
    public string kelas { get; set; }
    public DateTime tanggal_berangkat { get; set; }
    public TimeSpan waktu_berangkat { get; set; }
    public TimeSpan waktu_tiba { get; set; }
    public int sisa_kapasitas_penumpang_id { get; set; }
    public int sisa_kapasitas_kendaraan_id { get; set; }
    public JenisKendaraan jenis_kendaraan { get; set; }
    public StatusTiket status { get; set; }
    
    // Methods
    public void tampilkanJadwal()
    public void getDurasiPerjalanan()
    public void konfirmasiKetersediaan()
    public void kurangKapasitaspenumpang()
    public void pertambahanStatusJadwal()
    public void hitungKapasitaspenumpang()
}
```

#### 🎫 Tiket
```csharp
public class Tiket
{
    public int tiket_id { get; set; }
    public double total_harga { get; set; }
    public DateTime tanggal_pemesanan { get; set; }
    public JenisKendaraan jenis_kendaraan { get; set; }
    public StatusTiket status { get; set; }
    
    // Methods
    public bool buatTiket()
    public void tampilkanDetailTiket()
    public void cetakTiket()
    public void konfirmasiPembayaran()
    public void batalkanTiket()
}
```

#### 💳 Pembayaran
```csharp
public class Pembayaran
{
    public int pembayaran_id { get; set; }
    public string metode_pembayaran { get; set; }
    public double jumlah_bayar { get; set; }
    public DateTime tanggal_bayar { get; set; }
    
    // Methods
    public void prosesPembayaran()
    public void konfirmasiPembayaran()
}
```

#### 👥 Penumpang
```csharp
public class Penumpang
{
    public int NIK_penumpang { get; set; }
    public string nama { get; set; }
    public int no_hp { get; set; }
    
    // Navigation properties
    public List<RincianPenumpang> rincianPenumpangs { get; set; }
}
```

#### 🔔 Notifikasi
```csharp
public class Notifikasi
{
    public int notifikasi_id { get; set; }
    public JenisNotifikasi jenis { get; set; }
    public string pesan { get; set; }
    public DateTime waktu_kirim { get; set; }
    public string status_baca { get; set; }
    public bool is_broadcast { get; set; } // Menandakan broadcast
    
    // Navigation properties
    public Admin admin { get; set; } // Admin pengirim
    public int admin_id { get; set; }
    public Pengguna? pengguna { get; set; } // Null jika broadcast
    public int? pengguna_id { get; set; } // Null jika broadcast
    public Jadwal? jadwal { get; set; } // Jadwal terkait
    public int? jadwal_id { get; set; }
    
    // Methods
    public void kirimNotifikasi()
    public void kirimBroadcastNotifikasi()
    public void tandaiBacaan()
}
```

### 📊 Enums

#### 🚗 JenisKendaraan
```csharp
public enum JenisKendaraan
{
    Sepeda_Motor,
    Mobil,
    Truk,
    Bus
}
```

#### ✅ StatusTiket
```csharp
public enum StatusTiket
{
    Successful,
    Pending,
    Cancelled
}
```

#### 📢 JenisNotifikasi
```csharp
public enum JenisNotifikasi
{
    Pengingatkan,
    Update,
    Status
}
```

### 🔗 Relationship Classes
- `DetailTiket`: Menghubungkan Tiket dengan Jadwal, Kapal, Pengguna, dan Pembayaran
- `JadwalKapal`: Menghubungkan Jadwal dengan Kapal
- `JadwalPelabuhan`: Menghubungkan Jadwal dengan Pelabuhan asal dan tujuan

### 🛠️ Services

#### 📢 NotificationService
```csharp
public class NotificationService
{
    // Methods
    public void SendBroadcastNotification() // Kirim broadcast ke semua user
    public void SendScheduleChangeNotification() // Kirim notif perubahan jadwal
    public List<Notifikasi> GetBroadcastHistory() // Riwayat broadcast
}
```
**Fitur Khusus Notifikasi:**
- ✅ **Admin Broadcast**: Admin dapat mengirim notifikasi ke semua pengguna sekaligus
- ✅ **Schedule Change Alert**: Notifikasi khusus perubahan jadwal dengan detail lengkap
- ✅ **Personal Notification**: Notifikasi individual untuk pengingat personal
- ✅ **Broadcast History**: Riwayat semua notifikasi broadcast yang pernah dikirim

## 🚀 Cara Menjalankan

### Prerequisites
- .NET 9.0 SDK
- Visual Studio Code atau Visual Studio

### Instalasi & Menjalankan
```bash
# Clone repository
git clone https://github.com/benjminn/TiketLaut.git
cd TiketLaut

# Build aplikasi
dotnet build

# Jalankan aplikasi
dotnet run
```

## 🏗️ Struktur Proyek
```
TiketLaut/
├── Models/
│   ├── Admin.cs
│   ├── Pengguna.cs
│   ├── Kapal.cs
│   ├── Pelabuhan.cs
│   ├── Jadwal.cs
│   ├── Tiket.cs
│   ├── Pembayaran.cs
│   ├── Penumpang.cs
│   ├── RincianPenumpang.cs
│   ├── Notifikasi.cs
│   └── RelationshipClasses.cs
├── Services/
│   └── NotificationService.cs
├── Enums.cs
├── Program.cs
├── TiketLaut.csproj
└── README.md
```

## 🔮 Next Steps (WPF Implementation)
1. Implementasi UI dengan WPF
2. Database integration (Entity Framework Core)
3. Real-time notifications
4. Payment gateway integration
5. Report generation
6. Multi-language support

## 📱 Aplikasi Sejenis
- **Ferizy** - Platform booking ferry Indonesia

---
**© 2025 KapalKlik - Aplikasi Tiket Kapal Laut**
