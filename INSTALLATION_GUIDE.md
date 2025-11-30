# 📦 Installation Guide - TiketLaut

Panduan lengkap instalasi dan konfigurasi aplikasi TiketLaut untuk pengembang dan administrator sistem.

## 📋 Daftar Isi
- [System Requirements](#system-requirements)
- [Prasyarat Software](#prasyarat-software)
- [Database Setup](#database-setup)
- [Instalasi Aplikasi](#instalasi-aplikasi)
- [Konfigurasi](#konfigurasi)
- [Build & Run](#build--run)
- [Troubleshooting](#troubleshooting)

---

## 🖥️ System Requirements

### Minimum Requirements
- **OS:** Windows 10 (64-bit) atau lebih baru
- **Processor:** Intel Core i3 atau AMD equivalent
- **RAM:** 4 GB
- **Storage:** 500 MB ruang kosong
- **Display:** 1366 x 768 resolution
- **Internet:** Koneksi internet stabil untuk akses database dan API

### Recommended Requirements
- **OS:** Windows 11 (64-bit)
- **Processor:** Intel Core i5 atau AMD Ryzen 5
- **RAM:** 8 GB atau lebih
- **Storage:** 1 GB ruang kosong (SSD recommended)
- **Display:** 1920 x 1080 resolution atau lebih tinggi
- **Internet:** Broadband connection (min 10 Mbps)

---

## 🛠️ Prasyarat Software

### 1. .NET 9.0 SDK
**Download:** https://dotnet.microsoft.com/download/dotnet/9.0

**Instalasi:**
1. Download .NET 9.0 SDK installer untuk Windows
2. Jalankan installer dan ikuti instruksi
3. Verifikasi instalasi:
```cmd
dotnet --version
```
Output harus menunjukkan versi 9.0.x

### 2. Visual Studio 2022 (Optional, untuk development)
**Download:** https://visualstudio.microsoft.com/

**Workload yang diperlukan:**
- .NET Desktop Development
- Windows Presentation Foundation (WPF)

**Alternatif:** Visual Studio Code dengan C# extension

### 3. Git (Optional, untuk clone repository)
**Download:** https://git-scm.com/download/win

---

## 🗄️ Database Setup

### Opsi 1: Menggunakan Supabase (Recommended)

#### 1. Membuat Project Supabase
1. Buka https://supabase.com dan buat akun gratis
2. Klik **"New Project"**
3. Isi detail project:
   - **Name:** TiketLaut
   - **Database Password:** (buat password yang kuat)
   - **Region:** Singapore atau Southeast Asia
   - **Pricing Plan:** Free (atau sesuai kebutuhan)
4. Tunggu hingga project selesai dibuat (~2 menit)

#### 2. Mendapatkan Connection String
1. Di dashboard Supabase, buka **Settings** → **Database**
2. Scroll ke bagian **Connection String**
3. Pilih **URI** atau **Connection Pooling**
4. Salin connection string dengan format:
```
postgresql://postgres.[PROJECT-REF]:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres
```

#### 3. Menjalankan Database Migration
1. Di Supabase Dashboard, buka **SQL Editor**
2. Jalankan script SQL untuk membuat tabel-tabel:

```sql
-- Table: Pelabuhan
CREATE TABLE "Pelabuhan" (
    "PelabuhanID" SERIAL PRIMARY KEY,
    "NamaPelabuhan" VARCHAR(100) NOT NULL,
    "Kota" VARCHAR(100) NOT NULL,
    "Provinsi" VARCHAR(100) NOT NULL,
    "Latitude" DECIMAL(10, 8),
    "Longitude" DECIMAL(11, 8),
    "Timezone" VARCHAR(50)
);

-- Table: Kapal
CREATE TABLE "Kapal" (
    "KapalID" SERIAL PRIMARY KEY,
    "NamaKapal" VARCHAR(100) NOT NULL,
    "TipeKapal" VARCHAR(50) NOT NULL,
    "KapasitasPenumpang" INT NOT NULL,
    "KapasitasKendaraan" INT NOT NULL,
    "Fasilitas" TEXT,
    "Status" VARCHAR(20) DEFAULT 'Aktif'
);

-- Table: GrupKendaraan
CREATE TABLE "GrupKendaraan" (
    "GrupKendaraanID" SERIAL PRIMARY KEY,
    "NamaGrup" VARCHAR(100) NOT NULL,
    "HargaTambahan" DECIMAL(10, 2) NOT NULL,
    "Deskripsi" TEXT
);

-- Table: DetailKendaraan
CREATE TABLE "DetailKendaraan" (
    "DetailKendaraanID" SERIAL PRIMARY KEY,
    "GrupKendaraanID" INT REFERENCES "GrupKendaraan"("GrupKendaraanID"),
    "NamaKendaraan" VARCHAR(100) NOT NULL,
    "Dimensi" VARCHAR(100)
);

-- Table: Jadwal
CREATE TABLE "Jadwal" (
    "JadwalID" SERIAL PRIMARY KEY,
    "KapalID" INT REFERENCES "Kapal"("KapalID"),
    "PelabuhanAsalID" INT REFERENCES "Pelabuhan"("PelabuhanID"),
    "PelabuhanTujuanID" INT REFERENCES "Pelabuhan"("PelabuhanID"),
    "WaktuBerangkat" TIMESTAMP NOT NULL,
    "WaktuTiba" TIMESTAMP NOT NULL,
    "HargaEkonomi" DECIMAL(10, 2) NOT NULL,
    "HargaBisnis" DECIMAL(10, 2) NOT NULL,
    "HargaEksekutif" DECIMAL(10, 2) NOT NULL,
    "Status" VARCHAR(20) DEFAULT 'Terjadwal'
);

-- Table: Pengguna
CREATE TABLE "Pengguna" (
    "PenggunaID" SERIAL PRIMARY KEY,
    "Nama" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(100) UNIQUE NOT NULL,
    "Password" VARCHAR(255),
    "NoTelepon" VARCHAR(20),
    "TanggalLahir" DATE,
    "JenisKelamin" VARCHAR(10),
    "Alamat" TEXT,
    "GoogleId" VARCHAR(255),
    "TanggalDaftar" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Table: Admin
CREATE TABLE "Admin" (
    "AdminID" SERIAL PRIMARY KEY,
    "Nama" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(100) UNIQUE NOT NULL,
    "Password" VARCHAR(255) NOT NULL,
    "Role" VARCHAR(50) DEFAULT 'Admin',
    "TanggalDibuat" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Table: Tiket
CREATE TABLE "Tiket" (
    "TiketID" SERIAL PRIMARY KEY,
    "PenggunaID" INT REFERENCES "Pengguna"("PenggunaID"),
    "JadwalID" INT REFERENCES "Jadwal"("JadwalID"),
    "KodeBooking" VARCHAR(20) UNIQUE NOT NULL,
    "TanggalPemesanan" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "JumlahPenumpang" INT NOT NULL,
    "KelasLayanan" VARCHAR(20) NOT NULL,
    "TotalHarga" DECIMAL(10, 2) NOT NULL,
    "StatusPembayaran" VARCHAR(20) DEFAULT 'Menunggu',
    "StatusTiket" VARCHAR(20) DEFAULT 'Aktif',
    "ETicketPath" TEXT
);

-- Table: RincianPenumpang
CREATE TABLE "RincianPenumpang" (
    "RincianPenumpangID" SERIAL PRIMARY KEY,
    "TiketID" INT REFERENCES "Tiket"("TiketID"),
    "NamaPenumpang" VARCHAR(100) NOT NULL,
    "NIK" VARCHAR(20) NOT NULL,
    "JenisKelamin" VARCHAR(10) NOT NULL,
    "TanggalLahir" DATE NOT NULL,
    "Kewarganegaraan" VARCHAR(50) NOT NULL
);

-- Table: Penumpang (untuk kendaraan)
CREATE TABLE "Penumpang" (
    "PenumpangID" SERIAL PRIMARY KEY,
    "TiketID" INT REFERENCES "Tiket"("TiketID"),
    "GrupKendaraanID" INT REFERENCES "GrupKendaraan"("GrupKendaraanID"),
    "DetailKendaraanID" INT REFERENCES "DetailKendaraan"("DetailKendaraanID"),
    "NomorPolisi" VARCHAR(20)
);

-- Table: Pembayaran
CREATE TABLE "Pembayaran" (
    "PembayaranID" SERIAL PRIMARY KEY,
    "TiketID" INT REFERENCES "Tiket"("TiketID"),
    "MetodePembayaran" VARCHAR(50) NOT NULL,
    "JumlahPembayaran" DECIMAL(10, 2) NOT NULL,
    "TanggalPembayaran" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "StatusPembayaran" VARCHAR(20) DEFAULT 'Pending',
    "BuktiPembayaran" TEXT,
    "NomorReferensi" VARCHAR(100)
);

-- Table: Notifikasi
CREATE TABLE "Notifikasi" (
    "NotifikasiID" SERIAL PRIMARY KEY,
    "PenggunaID" INT REFERENCES "Pengguna"("PenggunaID"),
    "TiketID" INT REFERENCES "Tiket"("TiketID"),
    "Judul" VARCHAR(200) NOT NULL,
    "Pesan" TEXT NOT NULL,
    "TipeNotifikasi" VARCHAR(50) NOT NULL,
    "TanggalDibuat" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "Dibaca" BOOLEAN DEFAULT FALSE
);

-- Insert sample admin account
INSERT INTO "Admin" ("Nama", "Email", "Password", "Role")
VALUES ('Administrator', 'admin@tiketlaut.com', 'admin123', 'SuperAdmin');
```

3. Klik **Run** untuk mengeksekusi script
4. Verifikasi tabel telah dibuat di **Table Editor**

### Opsi 2: Menggunakan PostgreSQL Lokal

#### 1. Install PostgreSQL
1. Download PostgreSQL dari https://www.postgresql.org/download/windows/
2. Jalankan installer dan ikuti wizard:
   - Port: 5432 (default)
   - Password: (buat password untuk user postgres)
   - Locale: Default locale
3. Install pgAdmin (included) untuk management GUI

#### 2. Membuat Database
1. Buka pgAdmin atau psql command line
2. Buat database baru:
```sql
CREATE DATABASE tiketlaut;
```
3. Connect ke database dan jalankan script SQL di atas

---

## 📥 Instalasi Aplikasi

### Metode 1: Clone dari GitHub (Untuk Developer)

1. **Clone Repository**
```cmd
cd d:\
git clone https://github.com/benjminn/TiketLaut.git
cd TiketLaut
```

2. **Restore NuGet Packages**
```cmd
dotnet restore
```

3. **Build Application**
```cmd
dotnet build --configuration Release
```

### Metode 2: Download Release Binary (Untuk End User)

**File executable TIDAK tersedia di repository utama.**

Untuk mendapatkan aplikasi:

1. **Via GitHub Releases:**
   - Kunjungi https://github.com/benjminn/TiketLaut/releases/tag/v.1.0.0
   - Download file aplikasi (RAR atau ZIP) dari Assets
   - Extract ke folder pilihan (contoh: `C:\Program Files\TiketLaut`)
   - Ikuti petunjuk di `README.txt` di dalam folder

2. **Build Sendiri:**
   - Clone repository dan build dengan `dotnet build --configuration Release`
   - File executable ada di `bin\Release\net9.0-windows\TiketLaut.exe`

3. Lanjut ke bagian **Konfigurasi**

---

## ⚙️ Konfigurasi

### 1. Setup File Konfigurasi

1. **Copy template konfigurasi:**
```cmd
copy appsettings.example.json appsettings.json
```

2. **Edit `appsettings.json`** dengan text editor (Notepad, VS Code, dll)

### 2. Konfigurasi Database

Sesuaikan connection string dengan database Anda:

**Untuk Supabase:**
```json
{
  "ConnectionStrings": {
    "SupabaseConnection": "User Id=postgres.xxxx;Password=your_password;Server=db.xxxx.supabase.co;Port=5432;Database=postgres"
  }
}
```

**Untuk PostgreSQL Lokal:**
```json
{
  "ConnectionStrings": {
    "SupabaseConnection": "User Id=postgres;Password=your_password;Server=localhost;Port=5432;Database=tiketlaut"
  }
}
```

### 3. Konfigurasi Google OAuth (Optional)

Untuk mengaktifkan fitur login dengan Google:

#### Langkah 1: Membuat Google Cloud Project
1. Buka https://console.cloud.google.com
2. Buat project baru atau pilih existing project
3. Navigasi ke **APIs & Services** → **Credentials**

#### Langkah 2: Konfigurasi OAuth Consent Screen
1. Klik **OAuth consent screen**
2. Pilih **External** dan klik **Create**
3. Isi informasi:
   - **App name:** TiketLaut
   - **User support email:** email Anda
   - **Developer contact:** email Anda
4. Klik **Save and Continue**
5. Pada **Scopes**, tambahkan:
   - `userinfo.email`
   - `userinfo.profile`
6. Klik **Save and Continue** hingga selesai

#### Langkah 3: Membuat OAuth Client
1. Kembali ke **Credentials**
2. Klik **Create Credentials** → **OAuth client ID**
3. Pilih **Application type:** Desktop app
4. **Name:** TiketLaut Desktop
5. Klik **Create**
6. Salin **Client ID** dan **Client Secret**

#### Langkah 4: Update appsettings.json
```json
{
  "GoogleOAuth": {
    "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "RedirectUri": "http://localhost:8080/",
    "RedirectPort": 8080
  }
}
```

### 4. Konfigurasi Logging (Optional)

Sesuaikan level logging di `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

**Log Levels:**
- **Trace:** Detail maksimum (untuk debugging mendalam)
- **Debug:** Informasi debug
- **Information:** Informasi umum (recommended)
- **Warning:** Peringatan non-critical
- **Error:** Error yang perlu perhatian
- **Critical:** Kegagalan sistem

---

## 🚀 Build & Run

### Menjalankan dari Source Code

**Development Mode:**
```cmd
dotnet run
```

**Release Mode:**
```cmd
dotnet run --configuration Release
```

### Menjalankan dari Binary

**Setelah build atau extract release:**

1. Navigasi ke folder aplikasi:
```cmd
cd "d:\SMT 5\Junpro\TiketLaut\bin\Release\net9.0-windows"
```

2. Jalankan executable:
```cmd
TiketLaut.exe
```

**Informasi Build Terbaru:**
- **Lokasi:** `bin\Release\net9.0-windows\TiketLaut.exe`
- **Ukuran Executable:** ~186.5 KB
- **Total dengan Dependencies:** ~45.7 MB
- **Last Build:** 30 November 2025

### Membuat Shortcut Desktop (Optional)

1. Klik kanan pada `TiketLaut.exe`
2. Pilih **Send to** → **Desktop (create shortcut)**
3. Rename shortcut menjadi "TiketLaut"

---

## 🔧 Troubleshooting

### Error: "Could not find .NET runtime"

**Solusi:**
1. Pastikan .NET 9.0 Runtime terinstall
2. Download dari https://dotnet.microsoft.com/download/dotnet/9.0
3. Install **Desktop Runtime** (bukan SDK)
4. Restart aplikasi

### Error: "Connection to database failed"

**Solusi:**
1. Verifikasi connection string di `appsettings.json`
2. Pastikan database server berjalan (untuk PostgreSQL lokal)
3. Test koneksi dengan pgAdmin atau psql
4. Periksa firewall tidak memblokir port 5432
5. Untuk Supabase, cek quota dan status project

### Error: "The type initializer for 'Npgsql.NpgsqlConnection' threw an exception"

**Solusi:**
1. Install Visual C++ Redistributable:
   - Download dari https://aka.ms/vs/17/release/vc_redist.x64.exe
   - Install dan restart komputer
2. Update Npgsql package:
```cmd
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.0
```

### Error: "Google OAuth failed"

**Solusi:**
1. Verifikasi Client ID dan Client Secret di `appsettings.json`
2. Pastikan Redirect URI di Google Console sama dengan config (http://localhost:8080/)
3. Cek port 8080 tidak digunakan aplikasi lain
4. Pastikan OAuth consent screen sudah dipublish (minimal Testing mode)
5. Tambahkan test users di OAuth consent screen jika masih mode Testing

### Aplikasi Crash saat Startup

**Solusi:**
1. Hapus file cache di folder `obj` dan `bin`:
```cmd
rmdir /s /q obj
rmdir /s /q bin
```
2. Rebuild aplikasi:
```cmd
dotnet clean
dotnet restore
dotnet build
```

### Performance Issues

**Solusi:**
1. Tutup aplikasi lain yang berat
2. Pastikan RAM minimal 4GB tersedia
3. Bersihkan cache browser/aplikasi
4. Update driver graphics card
5. Disable background services yang tidak perlu

### Database Migration Gagal

**Solusi:**
1. Backup database existing
2. Drop semua table dan recreate:
```sql
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;
```
3. Jalankan ulang script SQL migration
4. Restore data dari backup jika diperlukan

---

## 📞 Support

### Kontak Tim
- **Email:** support@tiketlaut.com
- **GitHub Issues:** https://github.com/benjminn/TiketLaut/issues

### Dokumentasi Tambahan
- [User Guide](USER_GUIDE.md)
- [Analisis OOP dan Tech Stack](ANALISIS_OOP_DAN_TECH_STACK.md)
- [API Documentation](API_DOCUMENTATION.md)

### Contributors
- Benjamin Sigit (23/514737/TK/56513)
- Rafeyfa Asyla (23/512856/TK/56361)
- Chaira Nastya Warestri (23/514942/TK/56550)

---

## 📝 Notes

- Pastikan selalu backup `appsettings.json` sebelum update aplikasi
- Jangan commit file `appsettings.json` ke version control (sudah di .gitignore)
- Untuk production deployment, gunakan environment variables untuk credentials
- Update aplikasi secara berkala untuk bug fixes dan fitur baru

---

**Last Updated:** 29 November 2025  
**Version:** 1.0.0
