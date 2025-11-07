# AdminKelolaNotifikasiPage - Dokumentasi

## 📬 Fitur Lengkap Kelola Notifikasi Admin

### ✅ Fitur yang Tersedia

#### **Tab 1: Kirim Notifikasi** ✉️
Form untuk mengirim notifikasi manual ke pengguna:

**Fitur:**
- ✅ **Pilih Penerima:**
  - Semua Pengguna (broadcast)
  - Pengguna Tertentu (dropdown pilih user)
  
- ✅ **Jenis Notifikasi** dengan emoji:
  - 💳 Pembayaran → `iconPaymentNotif.png`
  - ⚠️ Pemberitahuan → `iconDangerNotif.png`
  - ⏰ Pengingat → `iconTimerNotif.png`
  - ❌ Pembatalan → `iconGagalNotif.png`
  - 📋 Umum → `iconTaskNotif.png`

- ✅ **Input Form:**
  - Judul Notifikasi (max 100 char)
  - Isi Pesan (textarea dengan scroll)
  
- ✅ **Preview Real-time:**
  - Preview otomatis saat mengetik
  - Menampilkan emoji sesuai jenis yang dipilih
  
- ✅ **Actions:**
  - 🧹 Bersihkan - Clear semua input
  - ✉️ Kirim Notifikasi - Kirim dengan konfirmasi

**Logic:**
- Notifikasi dikirim dengan `oleh_system = false` (manual by admin)
- `admin_id` diambil dari `SessionManager.CurrentAdmin`
- Jika broadcast, loop semua user dan kirim satu-satu
- Pesan otomatis ditambahkan emoji sesuai jenis

---

#### **Tab 2: Notifikasi Otomatis** ⚙️
Lihat semua notifikasi yang dikirim oleh sistem:

**Fitur:**
- ✅ DataGrid dengan kolom:
  - ID
  - Pengguna (nama)
  - Jenis
  - Judul
  - Waktu Kirim
  - Status (✓ Dibaca / ○ Belum dibaca)
  
- ✅ Filter otomatis: `oleh_system = true`
- ✅ Button 🔄 Refresh untuk reload data
- ✅ Auto-load saat tab dibuka

**Use Case:**
- Monitoring notifikasi pengingat keberangkatan
- Tracking notifikasi pembayaran otomatis
- Audit trail sistem notification

---

#### **Tab 3: Semua Notifikasi** 📋
Lihat dan kelola semua notifikasi (otomatis + manual):

**Fitur:**
- ✅ **DataGrid Lengkap** dengan kolom:
  - ID
  - Pengguna
  - Jenis
  - Judul
  - Sumber (⚙️ Sistem / 👤 Admin)
  - Waktu
  - Status

- ✅ **Filter Dinamis:**
  - Filter Jenis: Semua, pembayaran, pemberitahuan, pengingat, pembatalan, umum
  - Filter Sumber: Semua, Otomatis (Sistem), Manual (Admin)
  - Kombinasi filter bekerja bersamaan
  
- ✅ **Statistik:**
  - Total notifikasi (update real-time saat filter)
  
- ✅ **Actions:**
  - 🔄 Refresh - Reload semua data
  - 🗑️ Hapus Lama (>30 hari) - Cleanup notifikasi dengan konfirmasi
  - ❌ Reset Filter - Kembalikan ke default (Semua)

**Logic:**
- Data dimuat sekali ke `_allNotifikasi`
- Filter diterapkan di client-side untuk performa
- Hapus lama menggunakan `DeleteOldNotificationsAsync(30)` dari service

---

## 🎨 UI/UX Features

### Design Elements:
- **Tab Navigation:** 3 tab dengan active state (blue underline)
- **Card-based Layout:** White cards dengan shadow
- **Color Scheme:**
  - Primary: `#00658D` (Blue)
  - Success: `#28A745` (Green)
  - Danger: `#DC3545` (Red)
  - Background: White cards, `#F8F9FA` untuk section
  
- **Responsive Grid:** 2 kolom untuk form input
- **Preview Section:** Real-time preview dengan background `#F8F9FA`

### Button Styles:
- **PrimaryButton:** Blue untuk actions umum
- **SuccessButton:** Green untuk submit/kirim
- **DangerButton:** Red untuk clear/delete
- **TabButton:** Transparent dengan bottom border saat active

---

## 🔧 Technical Details

### Dependencies:
```csharp
using TiketLaut.Models;
using TiketLaut.Services;
// - NotifikasiService
// - PenggunaService
// - SessionManager
```

### Key Methods:

**Tab 1 - Kirim:**
```csharp
- LoadUsersAsync() // Load dropdown users
- CmbPenerima_SelectionChanged() // Show/hide user dropdown
- UpdatePreview() // Real-time preview update
- BtnKirim_Click() // Send notification logic
- BtnClear_Click() // Clear form
```

**Tab 2 - Otomatis:**
```csharp
- LoadOtomatisNotifikasi() // Load system notifications
- BtnRefreshOtomatis_Click() // Refresh data
```

**Tab 3 - Semua:**
```csharp
- LoadSemuaNotifikasi() // Load all notifications
- ApplyFilter() // Client-side filtering
- Filter_Changed() // Trigger when filter changed
- BtnResetFilter_Click() // Reset filters
- BtnHapusLama_Click() // Delete old notifications
```

### Data Binding:
- DataGrid menggunakan anonymous objects dengan computed properties:
  - `StatusText`: "✓ Dibaca" / "○ Belum dibaca"
  - `SumberText`: "⚙️ Sistem" / "👤 Admin"

---

## 📝 Usage Examples

### Send Manual Notification:
1. Pilih tab "✉️ Kirim Notifikasi"
2. Pilih penerima (semua / tertentu)
3. Pilih jenis notifikasi
4. Isi judul dan pesan
5. Lihat preview
6. Klik "✉️ Kirim Notifikasi"
7. Konfirmasi pengiriman

### Monitor System Notifications:
1. Klik tab "⚙️ Notifikasi Otomatis"
2. Lihat list notifikasi yang dikirim sistem
3. Cek status baca per user

### Cleanup Old Data:
1. Klik tab "📋 Semua Notifikasi"
2. Klik "🗑️ Hapus Lama (>30 hari)"
3. Konfirmasi penghapusan
4. Data >30 hari akan terhapus permanent

### Filter Notifications:
1. Pilih filter jenis (contoh: pembayaran)
2. Pilih filter sumber (contoh: Otomatis)
3. Lihat hasil filter real-time
4. Total notifikasi ter-update otomatis

---

## 🚀 Integration Points

### SessionManager:
```csharp
SessionManager.CurrentAdmin?.admin_id // For admin_id in notifications
```

### NotifikasiService Methods Used:
- `CreateNotifikasiAsync()` - Create new notification
- `GetAllNotifikasiAsync()` - Get all notifications
- `DeleteOldNotificationsAsync(days)` - Delete old notifications

### PenggunaService Methods Used:
- `GetAllAsync()` - Get all users for dropdown

---

## ✨ Highlights

1. ✅ **Real-time Preview** - Lihat notifikasi sebelum dikirim
2. ✅ **Broadcast Support** - Kirim ke semua user sekaligus
3. ✅ **Smart Filtering** - Multi-filter dengan kombinasi
4. ✅ **Auto Emoji** - Emoji otomatis berdasarkan jenis
5. ✅ **Admin Tracking** - Semua notifikasi manual tercatat dengan admin_id
6. ✅ **Clean UI** - Modern card-based design
7. ✅ **Responsive** - 2-column layout untuk efisiensi space

---

## 🎯 Next Steps (Optional Enhancements)

1. ⏰ **Schedule Notification** - Kirim notifikasi terjadwal
2. 📊 **Analytics Dashboard** - Statistik notifikasi (open rate, dll)
3. 📱 **Push Notification** - Integration dengan FCM/APNS
4. 🔔 **Sound Alert** - Bunyi saat notifikasi masuk
5. 📎 **Attachment Support** - Kirim gambar/file dalam notifikasi
6. 🌐 **Multi-language** - Support bahasa Indonesia & English

---

**Status:** ✅ Fully Implemented & Tested
**Build:** ✅ Successful
**Location:** `Views/Admin/AdminNotifikasiPage.xaml` & `.xaml.cs`
