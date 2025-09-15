using Microsoft.AspNetCore.Mvc;
using TiketLaut;
using TiketLaut.Controllers;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class JadwalController : ControllerBase
    {
        // SINGLE SOURCE OF TRUTH - Hanya data intrinsik jadwal, tidak duplikasi nama
        public static readonly List<object> AllJadwalData = new List<object>
        {
            new {
                jadwal_id = 1,
                pelabuhan_asal_id = 1,  // FK ke PelabuhanController
                pelabuhan_tujuan_id = 2,  // FK ke PelabuhanController
                kapal_id = 1,  // FK ke KapalController
                // nama pelabuhan/kapal: computed dari FK, tidak disimpan manual
                kelas = "Ekonomi",
                tanggal_berangkat = DateTime.Today.AddDays(1),
                waktu_berangkat = TimeSpan.FromHours(8),
                waktu_tiba = TimeSpan.FromHours(9),
                harga_penumpang = 15000m,
                harga_golongan_I = 30000m,
                harga_golongan_II = 40000m,
                harga_golongan_III = 50000m,
                harga_golongan_IV_A = 90000m,
                harga_golongan_IV_B = 100000m,
                harga_golongan_V_A = 150000m,
                harga_golongan_V_B = 160000m,
                harga_golongan_VI_A = 200000m,
                harga_golongan_VI_B = 210000m,
                harga_golongan_VII = 300000m,
                harga_golongan_VIII = 400000m,
                harga_golongan_IX = 500000m,
                status = "Tersedia"
            },
            new {
                jadwal_id = 2,
                pelabuhan_asal_id = 3,
                pelabuhan_tujuan_id = 4,
                kapal_id = 2,
                kelas = "Ekonomi",
                tanggal_berangkat = DateTime.Today.AddDays(1),
                waktu_berangkat = TimeSpan.FromHours(10),
                waktu_tiba = TimeSpan.FromHours(12),
                harga_penumpang = 25000m,
                harga_golongan_I = 45000m,
                harga_golongan_II = 65000m,
                harga_golongan_III = 85000m,
                harga_golongan_IV_A = 150000m,
                harga_golongan_IV_B = 165000m,
                harga_golongan_V_A = 250000m,
                harga_golongan_V_B = 265000m,
                harga_golongan_VI_A = 350000m,
                harga_golongan_VI_B = 365000m,
                harga_golongan_VII = 500000m,
                harga_golongan_VIII = 650000m,
                harga_golongan_IX = 800000m,
                status = "Tersedia"
            },
            // TAMBAH DATA BARU DI SINI - hanya sekali, akan otomatis tersedia di semua endpoint
            new {
                jadwal_id = 3,
                pelabuhan_asal_id = 5,
                pelabuhan_tujuan_id = 6,
                kapal_id = 3,
                kelas = "Ekonomi", 
                tanggal_berangkat = DateTime.Today.AddDays(2),
                waktu_berangkat = TimeSpan.FromHours(14),
                waktu_tiba = TimeSpan.FromHours(16),
                harga_penumpang = 20000m,
                harga_golongan_I = 35000m,
                harga_golongan_II = 55000m,
                harga_golongan_III = 75000m,
                harga_golongan_IV_A = 120000m,
                harga_golongan_IV_B = 135000m,
                harga_golongan_V_A = 200000m,
                harga_golongan_V_B = 215000m,
                harga_golongan_VI_A = 300000m,
                harga_golongan_VI_B = 315000m,
                harga_golongan_VII = 450000m,
                harga_golongan_VIII = 580000m,
                harga_golongan_IX = 720000m,
                status = "Tersedia"
            }
        };

        /// <summary>
        /// Mendapatkan semua jadwal
        /// </summary>
        /// <returns>List semua jadwal</returns>
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllJadwal()
        {
            return Ok(AllJadwalData);
        }

        /// <summary>
        /// Mendapatkan jadwal dengan informasi lengkap (JOIN dengan Pelabuhan dan Kapal)
        /// </summary>
        [HttpGet("detailed")]
        public ActionResult<IEnumerable<object>> GetDetailedJadwal()
        {
            var detailedJadwal = AllJadwalData.Select(j => {
                var jadwal = (dynamic)j;
                
                // Ambil nama pelabuhan asal via FK
                var pelabuhanAsal = PelabuhanController.AllPelabuhanData
                    .FirstOrDefault(p => ((dynamic)p).pelabuhan_id == jadwal.pelabuhan_asal_id);
                var namaAsalData = (dynamic)pelabuhanAsal!;
                
                // Ambil nama pelabuhan tujuan via FK
                var pelabuhanTujuan = PelabuhanController.AllPelabuhanData
                    .FirstOrDefault(p => ((dynamic)p).pelabuhan_id == jadwal.pelabuhan_tujuan_id);
                var namaTujuanData = (dynamic)pelabuhanTujuan!;
                
                // Ambil nama kapal via FK
                var kapal = KapalController.AllKapalData
                    .FirstOrDefault(k => ((dynamic)k).kapal_id == jadwal.kapal_id);
                var kapalData = (dynamic)kapal!;
                
                return new {
                    jadwal_id = jadwal.jadwal_id,
                    pelabuhan_asal_id = jadwal.pelabuhan_asal_id,
                    pelabuhan_tujuan_id = jadwal.pelabuhan_tujuan_id,
                    kapal_id = jadwal.kapal_id,
                    kelas = jadwal.kelas,
                    tanggal_berangkat = jadwal.tanggal_berangkat,
                    waktu_berangkat = jadwal.waktu_berangkat,
                    waktu_tiba = jadwal.waktu_tiba,
                    status = jadwal.status,
                    // Computed data dari FK - tidak duplikasi
                    pelabuhan_asal = namaAsalData.nama_pelabuhan,
                    pelabuhan_tujuan = namaTujuanData.nama_pelabuhan,
                    kapal = kapalData.nama_kapal,
                    // Harga info
                    harga_penumpang = jadwal.harga_penumpang,
                    harga_golongan_I = jadwal.harga_golongan_I,
                    harga_golongan_II = jadwal.harga_golongan_II,
                    harga_golongan_III = jadwal.harga_golongan_III,
                    harga_golongan_IV_A = jadwal.harga_golongan_IV_A,
                    harga_golongan_IV_B = jadwal.harga_golongan_IV_B,
                    harga_golongan_V_A = jadwal.harga_golongan_V_A,
                    harga_golongan_V_B = jadwal.harga_golongan_V_B,
                    harga_golongan_VI_A = jadwal.harga_golongan_VI_A,
                    harga_golongan_VI_B = jadwal.harga_golongan_VI_B,
                    harga_golongan_VII = jadwal.harga_golongan_VII,
                    harga_golongan_VIII = jadwal.harga_golongan_VIII,
                    harga_golongan_IX = jadwal.harga_golongan_IX
                };
            }).ToList();
            
            return Ok(detailedJadwal);
        }

        /// <summary>
        /// Mendapatkan jadwal berdasarkan ID - FILTERING dari data yang sama
        /// </summary>
        /// <param name="id">ID jadwal</param>
        /// <returns>Detail jadwal</returns>
        [HttpGet("{id}")]
        public ActionResult<object> GetJadwal(int id)
        {
            if (id <= 0)
            {
                return BadRequest("ID jadwal harus lebih dari 0");
            }

            // FILTERING: Cari berdasarkan ID dari data yang sama
            var jadwal = AllJadwalData.FirstOrDefault(j => 
                ((dynamic)j).jadwal_id == id);

            if (jadwal == null)
            {
                return NotFound($"Jadwal dengan ID {id} tidak ditemukan");
            }

            return Ok(jadwal);
        }

        /// <summary>
        /// Mendapatkan harga kendaraan untuk jadwal tertentu
        /// </summary>
        /// <param name="id">ID jadwal</param>
        /// <returns>Daftar harga per jenis kendaraan</returns>
        [HttpGet("{id}/harga-kendaraan")]
        public ActionResult<object> GetHargaKendaraan(int id)
        {
            if (id <= 0)
            {
                return BadRequest("ID jadwal harus lebih dari 0");
            }

            // Ambil data jadwal berdasarkan ID
            var jadwal = AllJadwalData.FirstOrDefault(j => 
                ((dynamic)j).jadwal_id == id);

            if (jadwal == null)
            {
                return NotFound($"Jadwal dengan ID {id} tidak ditemukan");
            }

            var jadwalData = (dynamic)jadwal;

            var hargaKendaraan = new
            {
                jadwal_id = jadwalData.jadwal_id,
                harga_penumpang = jadwalData.harga_penumpang,
                golongan_kendaraan = new
                {
                    golongan_I = new { nama = "Sepeda", harga = jadwalData.harga_golongan_I },
                    golongan_II = new { nama = "Motor <500cc", harga = jadwalData.harga_golongan_II },
                    golongan_III = new { nama = "Motor >500cc", harga = jadwalData.harga_golongan_III },
                    golongan_IV_A = new { nama = "Mobil Penumpang ≤5m", harga = jadwalData.harga_golongan_IV_A },
                    golongan_IV_B = new { nama = "Mobil Barang ≤5m", harga = jadwalData.harga_golongan_IV_B },
                    golongan_V_A = new { nama = "Bus 5-7m", harga = jadwalData.harga_golongan_V_A },
                    golongan_V_B = new { nama = "Truk 5-7m", harga = jadwalData.harga_golongan_V_B },
                    golongan_VI_A = new { nama = "Bus 7-10m", harga = jadwalData.harga_golongan_VI_A },
                    golongan_VI_B = new { nama = "Truk 7-10m", harga = jadwalData.harga_golongan_VI_B },
                    golongan_VII = new { nama = "Truk Tronton 10-12m", harga = jadwalData.harga_golongan_VII },
                    golongan_VIII = new { nama = "Truk Tronton 12-16m", harga = jadwalData.harga_golongan_VIII },
                    golongan_IX = new { nama = "Truk Tronton >16m", harga = jadwalData.harga_golongan_IX }
                }
            };

            return Ok(hargaKendaraan);
        }

        /// <summary>
        /// Cari jadwal berdasarkan rute dan tanggal
        /// </summary>
        /// <param name="asal">Pelabuhan asal</param>
        /// <param name="tujuan">Pelabuhan tujuan</param>
        /// <param name="tanggal">Tanggal keberangkatan</param>
        /// <returns>List jadwal yang sesuai</returns>
        [HttpGet("search")]
        public ActionResult<IEnumerable<object>> SearchJadwal(
            [FromQuery] string? asal, 
            [FromQuery] string? tujuan,
            [FromQuery] DateTime? tanggal)
        {
            // Sample search results
            var results = new[]
            {
                new {
                    jadwal_id = 1,
                    pelabuhan_asal = asal ?? "Ketapang",
                    pelabuhan_tujuan = tujuan ?? "Gilimanuk",
                    kapal = "KMP Dharma Rucitra",
                    tanggal_berangkat = tanggal ?? DateTime.Today.AddDays(1),
                    waktu_berangkat = TimeSpan.FromHours(8),
                    waktu_tiba = TimeSpan.FromHours(9),
                    harga_penumpang = 15000m,
                    sisa_kapasitas = 150,
                    status = "Tersedia"
                }
            };

            return Ok(results);
        }
    }
}
