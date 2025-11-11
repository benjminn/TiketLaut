# 🚢 TiketLaut - Aplikasi Pemesanan Tiket Kapal Laut

Sistem pemesanan tiket kapal laut berbasis desktop dengan fitur lengkap untuk manajemen booking, pembayaran, dan administrasi.

## 👥 Tim Pengembang - Ikan Asap Pak Be
| Jabatan           | Nama                                   | NIM                |
|-------------------|----------------------------------------|--------------------|
| Ketua Kelompok    | Benjamin Sigit                         | 23/514737/TK/56513 |
| Anggota 1         | Rafeyfa Asyla                          | 23/512856/TK/56361 |
| Anggota 2         | Chaira Nastya Warestri                 | 23/514942/TK/56550 |

## 📋 Deskripsi Aplikasi
**Nama Aplikasi:** TiketLaut (KapalKlik)  
**Platform:** Windows Desktop Application (WPF)  
**Framework:** .NET 9.0  
**Database:** Supabase PostgreSQL  
**Repository:** https://github.com/benjminn/TiketLaut

## 🎯 Permasalahan yang Dipecahkan
1. **Efisiensi Pemesanan:** Eliminasi antrian panjang di pelabuhan dengan sistem booking online
2. **Informasi Real-time:** Update langsung tentang jadwal, keterlambatan, dan perubahan rute
3. **Manajemen Data:** Sistem terintegrasi untuk admin mengelola jadwal, kapal, dan pembayaran
4. **Reminder Otomatis:** Notifikasi pengingat untuk jadwal keberangkatan dan check-in
5. **Transparansi Harga:** Sistem harga dinamis berdasarkan kelas layanan dan jenis kendaraan

## ✨ Fitur Lengkap

### 🎫 Fitur Pengguna
- **Pencarian Jadwal:** Cari rute berdasarkan pelabuhan asal/tujuan, tanggal, kelas layanan
- **Booking Online:** Pemesanan tiket untuk penumpang dan kendaraan
- **Multiple Payment:** Berbagai metode pembayaran (Transfer Bank, E-Wallet, Tunai)
- **E-Ticket:** Generate dan download e-ticket dalam format PNG
- **Tracking Status:** Monitor status pembayaran dan tiket secara real-time
- **History Booking:** Riwayat lengkap pemesanan dan pembayaran
- **Notifikasi Push:** Reminder check-in dan update status pembayaran
- **Google OAuth:** Login cepat menggunakan akun Google
- **Profile Management:** Kelola data profil dan informasi pribadi

### 👨‍💼 Fitur Admin
- **Dashboard Analitik:** Statistik pendapatan, tiket terjual, dan jadwal populer
- **Manajemen Jadwal:** CRUD lengkap untuk jadwal keberangkatan kapal
- **Manajemen Kapal:** Database kapal dengan kapasitas dan fasilitas
- **Manajemen Pelabuhan:** Data pelabuhan dengan koordinat dan timezone
- **Validasi Pembayaran:** Approve/reject pembayaran dari pengguna
- **Manajemen Tiket:** Update status tiket dan monitoring
- **Marine Weather:** Integrasi API cuaca maritim untuk keamanan pelayaran
- **User Management:** Kelola data pengguna dan admin
- **Notifikasi Broadcast:** Kirim notifikasi mass ke semua pengguna

### 🎨 Fitur UI/UX
- **Responsive Design:** Auto-scaling UI dengan minimum resolution support
- **Zoom Functionality:** Ctrl+Plus/Minus untuk zoom in/out (50%-200%)
- **Background Carousel:** Sliding image carousel dengan smooth transition
- **Transparent Navbar:** Modern navbar overlay pada homepage
- **Custom Dialogs:** Rounded corner dialogs dengan shadow effects
- **Loading States:** Smooth loading indicators untuk semua operasi
- **Error Handling:** User-friendly error messages

## 🛠️ Tech Stack

### Frontend (WPF)
- **.NET 9.0:** Latest .NET framework
- **WPF (Windows Presentation Foundation):** Desktop UI framework
- **XAML:** Declarative UI markup
- **Custom Controls:** Reusable UI components

### Backend & Database
- **Entity Framework Core 9.0:** ORM untuk database operations
- **Supabase PostgreSQL:** Cloud-hosted database
- **Npgsql:** PostgreSQL data provider
- **LINQ:** Query language untuk data manipulation

### External APIs
- **Google OAuth 2.0:** Authentication service
- **Open-Meteo Marine API:** Weather forecasting untuk maritim
- **HTTP Client:** RESTful API consumption

### Libraries & Tools
- **Microsoft.Extensions.Configuration:** App settings management
- **System.Text.Json:** JSON serialization
- **ConsoleTraceListener:** Debug logging
- **ZoomHelper:** Custom zoom functionality

## 📦 Instalasi & Setup

### Prerequisites
- Windows 10/11
- .NET 9.0 SDK atau lebih tinggi
- Visual Studio 2022 (recommended) atau VS Code
- Supabase account (untuk database)
- Google Cloud Console account (untuk OAuth)

### 1. Clone Repository
```powershell
git clone https://github.com/benjminn/TiketLaut.git
cd TiketLaut
```

### 2. Setup Configuration

**Copy template configuration:**
```powershell
Copy-Item appsettings.example.json appsettings.json
```

**Edit appsettings.json dengan credentials Anda:**
```json
{
  "ConnectionStrings": {
    "SupabaseConnection": "Host=YOUR_HOST;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASS"
  },
  "GoogleOAuth": {
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "RedirectUri": "http://localhost:8080/",
    "RedirectPort": "8080"
  }
}
```

> 📖 **Dokumentasi lengkap:** 
> - [ENVIRONMENT_VARIABLES_GUIDE.md](./ENVIRONMENT_VARIABLES_GUIDE.md)
> - [REAL_GOOGLE_OAUTH_SETUP.md](./REAL_GOOGLE_OAUTH_SETUP.md)

### 3. Restore Dependencies
```powershell
dotnet restore
```

### 4. Build Application
```powershell
dotnet build
```

### 5. Run Application
```powershell
dotnet run
```

### 6. Test Google OAuth (Optional)
- Dengan credentials → Browser akan membuka Google login
- Tanpa credentials → Manual input dialog akan muncul

> 🔒 **Security Note:** File appsettings.json sudah di .gitignore

## 🎮 Cara Penggunaan

### Sebagai Pengguna

1. **Daftar/Login**
   - Gunakan email & password atau Google OAuth
   - Lengkapi profil dengan NIK dan data pribadi

2. **Cari & Booking**
   - Pilih pelabuhan asal dan tujuan
   - Tentukan tanggal keberangkatan
   - Pilih kelas layanan (Reguler/Ekonomi/Eksekutif)
   - Pilih jenis kendaraan (jika ada)

3. **Isi Data Penumpang**
   - Input data semua penumpang (nama, NIK, jenis kelamin)
   - Input plat nomor (jika membawa kendaraan)

4. **Pembayaran**
   - Pilih metode pembayaran
   - Upload bukti transfer (jika transfer bank)
   - Tunggu validasi admin

5. **Download E-Ticket**
   - Setelah pembayaran divalidasi
   - Download e-ticket dari menu "History" atau "Cek Booking"

### Sebagai Admin

1. **Login Admin**
   - Email: admin@tiketlaut.com
   - Password: (sesuai database)

2. **Dashboard**
   - Monitor statistik real-time
   - Lihat jadwal populer dan pendapatan

3. **Kelola Jadwal**
   - Tambah/edit/hapus jadwal keberangkatan
   - Set harga berdasarkan kelas dan grup kendaraan

4. **Validasi Pembayaran**
   - Review bukti pembayaran dari pengguna
   - Approve atau reject dengan alasan

5. **Kirim Notifikasi**
   - Broadcast pengumuman ke semua pengguna
   - Kirim reminder check-in otomatis

## 🔑 Fitur Khusus

### Zoom Functionality
- **Ctrl + Plus (+):** Zoom in (maksimal 200%)
- **Ctrl + Minus (-):** Zoom out (minimal 50%)
- **Ctrl + 0:** Reset ke 100%
- Berfungsi di semua window aplikasi

### Timezone Support
- Konversi otomatis waktu keberangkatan ke timezone lokal pelabuhan
- Mendukung WIB, WITA, WIT
- Akurat untuk reminder dan check-in time

### Marine Weather Integration
- Data cuaca real-time untuk rute maritim
- Prediksi gelombang dan kecepatan angin
- Safety assessment (Aman/Hati-hati/Berbahaya)

## Git Workflow 💪🏻

### Branch Strategy
1. Checkout ke main branch
2. Pull latest changes: git pull origin main
3. Buat branch baru: git checkout -b <type>/<description>.<nama>
4. Code & commit
5. Pull main lagi: git pull origin main
6. Push branch: git push origin <branch-name>
7. Buat Pull Request ke main
8. Merge & delete branch

### Branch Naming Convention ✏️
Format: <type>/<short_description>.<nama>

**Types:**
- feature: Fitur baru
- fix: Perbaikan bug
- refactor: Refactoring kode
- docs: Update dokumentasi

**Contoh:**
- feature/navbar.benjamin
- fix/payment-validation.rafeyfa
- refactor/clean-code.chaira

### Commit Message Format 📝
Format: <type>(<scope>): <short_summary>

**Types:**
- feat: Fitur baru
- fix: Bug fix
- refactor: Refactoring
- docs: Dokumentasi
- style: Formatting
- test: Testing
- chore: Maintenance

**Contoh:**
```
feat(api): Creating booking endpoint
fix(payment): Resolve validation error
refactor(ui): Clean up unused comments
docs(readme): Update installation guide
```

## 📁 Struktur Project

```
TiketLaut/
├── Data/
│   └── AppDbContext.cs              # Database context & configurations
├── Models/
│   ├── Admin.cs                     # Model admin
│   ├── Pengguna.cs                  # Model user
│   ├── Tiket.cs                     # Model tiket
│   ├── Pembayaran.cs                # Model pembayaran
│   ├── Jadwal.cs                    # Model jadwal
│   ├── Kapal.cs                     # Model kapal
│   ├── Pelabuhan.cs                 # Model pelabuhan
│   ├── Penumpang.cs                 # Model penumpang
│   ├── RincianPenumpang.cs          # Relasi tiket-penumpang
│   ├── Notifikasi.cs                # Model notifikasi
│   ├── GrupKendaraan.cs             # Model grup kendaraan
│   └── DetailKendaraan.cs           # Model detail kendaraan
├── Services/
│   ├── AdminService.cs              # Business logic admin
│   ├── PenggunaService.cs           # Business logic user
│   ├── TiketService.cs              # Business logic tiket
│   ├── PembayaranService.cs         # Business logic pembayaran
│   ├── JadwalService.cs             # Business logic jadwal
│   ├── NotifikasiService.cs         # Business logic notifikasi
│   ├── MarineWeatherService.cs      # Marine weather API
│   ├── BookingService.cs            # Booking operations
│   ├── SessionManager.cs            # User session management
│   └── DatabaseService.cs           # Database utilities
├── Views/
│   ├── HomePage.xaml                # Landing page
│   ├── LoginWindow.xaml             # Login page
│   ├── RegisterWindow.xaml          # Register page
│   ├── ScheduleWindow.xaml          # Schedule browsing
│   ├── BookingDetailWindow.xaml     # Booking form
│   ├── PaymentWindow.xaml           # Payment page
│   ├── TiketDetailWindow.xaml       # E-ticket view
│   ├── HistoryWindow.xaml           # Booking history
│   ├── CekBookingWindow.xaml        # Check booking status
│   ├── NotifikasiWindow.xaml        # Notifications
│   ├── ProfileWindow.xaml           # User profile
│   ├── AdminDashboard.xaml          # Admin main dashboard
│   ├── Components/
│   │   ├── NavbarPreLogin.xaml     # Navbar before login
│   │   ├── NavbarPostLogin.xaml    # Navbar after login
│   │   └── CustomDialog.xaml       # Custom dialog component
│   └── Admin/
│       ├── AdminJadwalPage.xaml    # Admin schedule management
│       ├── AdminPembayaranPage.xaml # Admin payment validation
│       ├── AdminTiketPage.xaml     # Admin ticket management
│       ├── AdminKapalPage.xaml     # Admin ship management
│       ├── AdminPelabuhanPage.xaml # Admin port management
│       ├── AdminNotifikasiPage.xaml # Admin notifications
│       ├── AdminKelolaAdminPage.xaml # Admin user management
│       └── MarineWeatherPage.xaml  # Marine weather dashboard
├── Helpers/
│   ├── ZoomHelper.cs                # Zoom functionality
│   └── WindowSizeHelper.cs          # Window size utilities
├── Enums.cs                         # Enumerations
├── Program.cs                       # Entry point
├── App.xaml                         # App configuration
├── appsettings.json                 # App settings (gitignored)
└── appsettings.example.json         # Settings template
```

## 🎨 Design Patterns & Architecture

### Architecture
- **MVVM-inspired:** Separation of concerns dengan Models, Views, Services
- **Repository Pattern:** Service layer sebagai data access abstraction
- **Dependency Injection:** Services di-inject ke Views
- **Session Management:** Centralized user session handling

### Code Quality
- **Naming Conventions:**
  - PascalCase: Classes, methods, properties
  - camelCase: Local variables, parameters
  - Descriptive names: Avoid abbreviations
- **SOLID Principles:** Maintained across services
- **Error Handling:** Try-catch dengan user-friendly messages
- **Null Safety:** Null-conditional operators (?.) untuk safety

## 🔐 Security Features

- **Password Encryption:** Hashed passwords di database
- **SQL Injection Prevention:** Parameterized queries via EF Core
- **OAuth 2.0:** Secure Google authentication flow
- **Session Management:** Secure user session handling
- **Role-Based Access:** Admin vs User permissions
- **Input Validation:** Server-side validation untuk semua form
- **Connection String Security:** Gitignored appsettings.json

## 🧪 Testing

### Manual Testing Checklist
- [ ] User registration & login
- [ ] Google OAuth flow
- [ ] Search jadwal dengan berbagai filter
- [ ] Booking dengan/tanpa kendaraan
- [ ] Multiple penumpang booking
- [ ] Payment upload & validation
- [ ] E-ticket generation
- [ ] Admin CRUD operations
- [ ] Notification system
- [ ] Zoom functionality
- [ ] Timezone conversion accuracy

## 📊 Database Schema

### Key Tables
- **pengguna:** User accounts
- **admin:** Admin accounts dengan roles
- **pelabuhan:** Port data dengan coordinates & timezone
- **kapal:** Ship data dengan capacity
- **jadwal:** Schedule dengan pricing
- **grup_kendaraan:** Vehicle pricing groups
- **detail_kendaraan:** Vehicle types dalam grup
- **tiket:** Tickets dengan booking info
- **pembayaran:** Payment records
- **penumpang:** Passenger master data
- **rincian_penumpang:** Ticket-passenger relations
- **notifikasi:** User notifications

### Relationships
- Jadwal → Pelabuhan (many-to-one, asal & tujuan)
- Jadwal → Kapal (many-to-one)
- Jadwal → GrupKendaraan (many-to-one)
- Tiket → Jadwal (many-to-one)
- Tiket → Pengguna (many-to-one)
- Tiket → RincianPenumpang → Penumpang (many-to-many)
- Pembayaran → Tiket (one-to-one)

## 🐛 Known Issues & Limitations

- Hot reload may cause temporary UI glitches (restart recommended)
- Large image carousel files may slow initial load
- Marine weather API requires internet connection
- E-ticket PDF generation not yet implemented (currently PNG only)
- Mobile/tablet version not available (Windows desktop only)

## 🚀 Future Enhancements

- [ ] Mobile app (Android/iOS)
- [ ] SMS notification integration
- [ ] Payment gateway integration (Midtrans, etc)
- [ ] QR Code for e-tickets
- [ ] Multi-language support (English, etc)
- [ ] Dark mode theme
- [ ] Seat selection feature
- [ ] Loyalty program
- [ ] Analytics dashboard improvement
- [ ] Export reports to Excel/PDF

## 📄 License

This project is created for educational purposes as part of university coursework.

## 📞 Contact & Support

**Tim Ikan Asap Pak Be:**
- Benjamin Sigit - 23/514737/TK/56513
- Rafeyfa Asyla - 23/512856/TK/56361  
- Chaira Nastya Warestri - 23/514942/TK/56550

**Repository:** https://github.com/benjminn/TiketLaut

---

## Class Diagram

Berikut adalah rancangan class diagram yang dibuat

<div style="text-align: center;">
  <img alt="Class Diagram" src="./.docs/class-diagram.jpg" width="550px" style="display: block; margin: auto;"/>
</div>

## Entity Relation Diagram

Berikut adalah rancangan ERD yang dibuat

<div style="text-align: center;">
  <img alt="ERD" src="./.docs/ERD.jpg" width="550px" style="display: block; margin: auto;"/>
</div>

---

**Made with ❤️ by Tim Ikan Asap Pak Be**

*Last Updated: December 2024*
