using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TiketLaut.Data;

namespace TiketLaut.Services
{
    public class BookingService
    {
        private readonly AppDbContext _context;

        public BookingService()
        {
            _context = DatabaseService.GetContext();
        }

        /// <summary>
        /// Simpan booking lengkap ke database (Tiket + Penumpang + RincianPenumpang)
        /// </summary>
        public async Task<Tiket> CreateBookingAsync(BookingData bookingData)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Generate kode tiket unik
                string kodeTiket = GenerateKodeTiket();

                // 2. Hitung total harga
                decimal totalHarga = await CalculateTotalHargaAsync(
                    bookingData.JadwalId,
                    bookingData.JenisKendaraanId,
                    bookingData.JumlahPenumpang);

                // 3. Buat dan simpan Tiket
                var tiket = new Tiket
                {
                    pengguna_id = bookingData.PenggunaId,
                    jadwal_id = bookingData.JadwalId,
                    kode_tiket = kodeTiket,
                    jumlah_penumpang = bookingData.JumlahPenumpang,
                    total_harga = totalHarga,
                    tanggal_pemesanan = DateTime.UtcNow,
                    status_tiket = "Menunggu Pembayaran",
                    jenis_kendaraan_enum = GetJenisKendaraanText(bookingData.JenisKendaraanId),
                    plat_nomor = bookingData.PlatNomor,
                    // Data Pemesan (kontak saja, NIK tersimpan di Penumpang)
                    nama_pemesan = bookingData.NamaPemesan,
                    nomor_hp_pemesan = bookingData.NomorHpPemesan,
                    email_pemesan = bookingData.EmailPemesan
                };

                _context.Tikets.Add(tiket);
                await _context.SaveChangesAsync();

                // 4. Simpan data penumpang
                foreach (var penumpangData in bookingData.DataPenumpang)
                {
                    // Cek apakah penumpang sudah ada (berdasarkan NIK)
                    var existingPenumpang = await _context.Penumpangs
                        .FirstOrDefaultAsync(p => 
                            p.nomor_identitas == penumpangData.NomorIdentitas &&
                            p.pengguna_id == bookingData.PenggunaId);

                    Penumpang penumpang;

                    if (existingPenumpang != null)
                    {
                        // Gunakan data penumpang yang sudah ada
                        penumpang = existingPenumpang;
                    }
                    else
                    {
                        // Buat penumpang baru
                        penumpang = new Penumpang
                        {
                            pengguna_id = bookingData.PenggunaId,
                            nama = penumpangData.Nama,
                            nomor_identitas = penumpangData.NomorIdentitas,
                            jenis_identitas = penumpangData.JenisIdentitas,
                            jenis_kelamin = penumpangData.JenisKelamin
                        };

                        _context.Penumpangs.Add(penumpang);
                        await _context.SaveChangesAsync();
                    }

                    // 5. Buat RincianPenumpang (relasi tiket-penumpang)
                    var rincianPenumpang = new RincianPenumpang
                    {
                        tiket_id = tiket.tiket_id,
                        penumpang_id = penumpang.penumpang_id
                    };

                    _context.RincianPenumpangs.Add(rincianPenumpang);
                }

                await _context.SaveChangesAsync();

                // 6. Update kapasitas jadwal
                await UpdateKapasitasJadwalAsync(
                    bookingData.JadwalId,
                    bookingData.JumlahPenumpang,
                    bookingData.JenisKendaraanId);

                // Commit transaction
                await transaction.CommitAsync();

                return tiket;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"[BookingService] Error creating booking: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[BookingService] Inner Exception: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"[BookingService] StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Generate kode tiket unik dengan format: TKT-YYYYMMDD-XXXXXX
        /// </summary>
        private string GenerateKodeTiket()
        {
            var datePrefix = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random();
            var randomNumber = random.Next(100000, 999999);
            return $"TKT-{datePrefix}-{randomNumber}";
        }

        /// <summary>
        /// Hitung total harga berdasarkan jadwal dan jenis kendaraan
        /// </summary>
        private async Task<decimal> CalculateTotalHargaAsync(int jadwalId, int jenisKendaraanId, int jumlahPenumpang)
        {
            // Get jadwal with its GrupKendaraan and DetailKendaraans
            var jadwal = await _context.Jadwals
                .Include(j => j.GrupKendaraan)
                    .ThenInclude(g => g!.DetailKendaraans)
                .FirstOrDefaultAsync(j => j.jadwal_id == jadwalId);

            if (jadwal?.GrupKendaraan?.DetailKendaraans == null)
            {
                throw new Exception($"Jadwal {jadwalId} tidak ditemukan atau tidak memiliki data kendaraan");
            }

            // Find the DetailKendaraan for the requested jenis_kendaraan
            var detailKendaraan = jadwal.GrupKendaraan.DetailKendaraans
                .FirstOrDefault(d => d.jenis_kendaraan == jenisKendaraanId);

            if (detailKendaraan == null)
            {
                throw new Exception($"Jenis kendaraan {jenisKendaraanId} tidak tersedia dalam grup kendaraan untuk jadwal {jadwalId}");
            }

            decimal harga = detailKendaraan.harga_kendaraan;

            // Jika pejalan kaki (jenisKendaraanId == 0), kalikan dengan jumlah penumpang
            if (jenisKendaraanId == 0)
            {
                return harga * jumlahPenumpang;
            }

            // Jika menggunakan kendaraan, harga tidak dikali penumpang
            return harga;
        }

        /// <summary>
        /// Update kapasitas jadwal setelah booking
        /// </summary>
        private async Task UpdateKapasitasJadwalAsync(int jadwalId, int jumlahPenumpang, int jenisKendaraanId)
        {
            var jadwal = await _context.Jadwals.FindAsync(jadwalId);
            if (jadwal == null)
            {
                throw new Exception($"Jadwal {jadwalId} tidak ditemukan");
            }

            // Kurangi kapasitas penumpang
            jadwal.sisa_kapasitas_penumpang -= jumlahPenumpang;

            // Kurangi kapasitas kendaraan jika ada kendaraan
            if (jenisKendaraanId > 0)
            {
                jadwal.sisa_kapasitas_kendaraan -= 1;
            }

            // Validasi kapasitas tidak minus
            if (jadwal.sisa_kapasitas_penumpang < 0 || jadwal.sisa_kapasitas_kendaraan < 0)
            {
                throw new Exception("Kapasitas tidak mencukupi!");
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Helper: Convert jenis kendaraan ID ke text
        /// HARUS SAMA PERSIS dengan Tag di ScheduleWindow & HomePage popup!
        /// </summary>
        private string GetJenisKendaraanText(int jenisKendaraanId)
        {
            return jenisKendaraanId switch
            {
                0 => "Pejalan Kaki",
                1 => "Sepeda",
                2 => "Sepeda Motor (<500cc)",
                3 => "Sepeda Motor (>500cc)",
                4 => "Mobil Penumpang",
                5 => "Truk Pickup",
                6 => "Bus Sedang",
                7 => "Truk Sedang",
                8 => "Bus Besar",
                9 => "Truk Besar",
                10 => "Truk Tronton",
                11 => "Truk Tronton (<16 meter)",
                12 => "Truk Tronton (>16 meter)",
                _ => "Tidak Diketahui"
            };
        }

        /// <summary>
        /// Get tiket by ID dengan relasi lengkap
        /// </summary>
        public async Task<Tiket?> GetTiketByIdAsync(int tiketId)
        {
            return await _context.Tikets
                .Include(t => t.Pengguna)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.kapal)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.pelabuhan_asal)
                .Include(t => t.Jadwal)
                    .ThenInclude(j => j.pelabuhan_tujuan)
                .Include(t => t.RincianPenumpangs)
                    .ThenInclude(rp => rp.penumpang)
                .FirstOrDefaultAsync(t => t.tiket_id == tiketId);
        }
    }

    /// <summary>
    /// Data transfer object untuk booking
    /// </summary>
    public class BookingData
    {
        public int PenggunaId { get; set; }
        public int JadwalId { get; set; }
        public int JenisKendaraanId { get; set; }
        public int JumlahPenumpang { get; set; }
        public string? PlatNomor { get; set; }
        
        // Data Pemesan (kontak saja, NIK tersimpan di Penumpang)
        public string NamaPemesan { get; set; }
        public string NomorHpPemesan { get; set; }
        public string EmailPemesan { get; set; }
        
        public List<PenumpangData> DataPenumpang { get; set; } = new List<PenumpangData>();
    }

    public class PenumpangData
    {
        public string Nama { get; set; } = string.Empty;
        public long NomorIdentitas { get; set; }
        public string JenisIdentitas { get; set; } = string.Empty;
        public string JenisKelamin { get; set; } = string.Empty;
    }
}