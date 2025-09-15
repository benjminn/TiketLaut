using Microsoft.AspNetCore.Mvc;
using TiketLaut;
using TiketLaut.Controllers;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TiketController : ControllerBase
    {
        // SINGLE SOURCE OF TRUTH - Data tiket dengan format baru (JenisKendaraan enum + plat_nomor)
        private static readonly List<object> AllTiketData = new List<object>
        {
            new { 
                tiket_id = 1, 
                jadwal_id = 1,
                total_harga = 40000.0,
                status = "Aktif",
                tanggal_pemesanan = DateTime.Now.AddDays(-1),
                jenis_kendaraan_enum = (JenisKendaraan?)JenisKendaraan.Golongan_II,
                plat_nomor = "B1234CD"
                // pengguna_id: computed dari RincianPenumpang → Penumpang → pengguna_id
                // jumlah_penumpang: computed dari RincianPenumpang.Count()
            },
            new { 
                tiket_id = 2, 
                jadwal_id = 2,
                total_harga = 65000.0,
                status = "Aktif",
                tanggal_pemesanan = DateTime.Now.AddHours(-2),
                jenis_kendaraan_enum = (JenisKendaraan?)JenisKendaraan.Golongan_II,
                plat_nomor = "D5678EF"
            },
            new { 
                tiket_id = 3, 
                jadwal_id = 1,
                total_harga = 90000.0,
                status = "Pending",
                tanggal_pemesanan = DateTime.Now.AddMinutes(-30),
                jenis_kendaraan_enum = (JenisKendaraan?)JenisKendaraan.Golongan_IV_A,
                plat_nomor = "F9012GH"
            },
            new { 
                tiket_id = 4, 
                jadwal_id = 3,
                total_harga = 55000.0,
                status = "Aktif",
                tanggal_pemesanan = DateTime.Now.AddMinutes(-15),
                jenis_kendaraan_enum = (JenisKendaraan?)null, // Jalan kaki
                plat_nomor = ""
            }
        };

        /// <summary>
        /// Mendapatkan tiket berdasarkan ID - FILTERING dari data yang sama
        /// </summary>
        /// <param name="id">ID tiket</param>
        /// <returns>Detail tiket</returns>
        [HttpGet("{id}")]
        public ActionResult<object> GetTiket(int id)
        {
            if (id <= 0)
            {
                return BadRequest("ID tiket harus lebih dari 0");
            }

            // FILTERING: Cari berdasarkan ID dari data yang sama
            var tiket = AllTiketData.FirstOrDefault(t => 
                ((dynamic)t).tiket_id == id);

            if (tiket == null)
            {
                return NotFound($"Tiket dengan ID {id} tidak ditemukan");
            }

            return Ok(tiket);
        }

        /// <summary>
        /// Mengambil semua tiket
        /// </summary>
        [HttpGet("all")]
        public ActionResult<IEnumerable<object>> GetAllTiket()
        {
            return Ok(AllTiketData);
        }

        /// <summary>
        /// Mengambil tiket dengan computed properties (pengguna_id, jumlah_penumpang dari RincianPenumpang)
        /// </summary>
        [HttpGet("detailed")]
        public ActionResult<IEnumerable<object>> GetDetailedTiket()
        {
            var detailedTikets = AllTiketData.Select(t => {
                var tiket = (dynamic)t;
                var tiketId = tiket.tiket_id;
                
                // Hitung dari RincianPenumpang
                var rincianList = RincianPenumpangController.AllRincianPenumpangData
                    .Where(rp => ((dynamic)rp).tiket_id == tiketId)
                    .ToList();
                
                var jumlahPenumpang = rincianList.Count;
                
                // Ambil pengguna_id dari penumpang pertama (asumsi 1 tiket = 1 pengguna)
                var penggunaId = 0;
                if (rincianList.Any())
                {
                    var firstPenumpangId = ((dynamic)rincianList.First()).penumpang_id;
                    var penumpang = PenumpangController.AllPenumpangData
                        .FirstOrDefault(p => ((dynamic)p).penumpang_id == firstPenumpangId);
                    if (penumpang != null)
                    {
                        penggunaId = ((dynamic)penumpang).pengguna_id;
                    }
                }
                
                return new {
                    tiket_id = tiket.tiket_id,
                    jadwal_id = tiket.jadwal_id,
                    total_harga = tiket.total_harga,
                    status = tiket.status,
                    tanggal_pemesanan = tiket.tanggal_pemesanan,
                    jenis_kendaraan_enum = tiket.jenis_kendaraan_enum,
                    plat_nomor = tiket.plat_nomor,
                    ada_kendaraan = tiket.jenis_kendaraan_enum.HasValue && 
                                   tiket.jenis_kendaraan_enum != JenisKendaraan.Jalan_Kaki,
                    // Computed properties
                    pengguna_id = penggunaId,
                    jumlah_penumpang = jumlahPenumpang
                };
            }).ToList();
            
            return Ok(detailedTikets);
        }

        /// <summary>
        /// Membuat tiket baru
        /// </summary>
        /// <param name="request">Data tiket baru</param>
        /// <returns>Tiket yang berhasil dibuat</returns>
        [HttpPost]
        public ActionResult<object> CreateTiket([FromBody] CreateTiketRequest request)
        {
            if (request == null)
            {
                return BadRequest("Data tiket tidak valid");
            }

            // Simulasi pembuatan tiket
            var newTiket = new
            {
                tiket_id = new Random().Next(1000, 9999),
                pengguna_id = request.pengguna_id,
                jadwal_id = request.jadwal_id,
                total_harga = CalculatePrice(request.jadwal_id, request.jenis_kendaraan_enum),
                status = "Aktif",
                tanggal_pemesanan = DateTime.Now,
                jenis_kendaraan_enum = request.jenis_kendaraan_enum,
                plat_nomor = request.plat_nomor ?? "",
                ada_kendaraan = request.jenis_kendaraan_enum.HasValue && 
                               request.jenis_kendaraan_enum != JenisKendaraan.Jalan_Kaki,
                jumlah_penumpang = request.jumlah_penumpang
            };

            return CreatedAtAction(nameof(GetTiket), new { id = newTiket.tiket_id }, newTiket);
        }

        /// <summary>
        /// Update status tiket
        /// </summary>
        /// <param name="id">ID tiket</param>
        /// <param name="status">Status baru</param>
        /// <returns>Hasil update</returns>
        [HttpPut("{id}/status")]
        public ActionResult UpdateTiketStatus(int id, [FromBody] string status)
        {
            if (id <= 0)
            {
                return BadRequest("ID tiket tidak valid");
            }

            return Ok(new { message = $"Status tiket {id} berhasil diupdate menjadi {status}", updated_at = DateTime.Now });
        }

        /// <summary>
        /// Hapus tiket
        /// </summary>
        /// <param name="id">ID tiket</param>
        /// <returns>Hasil penghapusan</returns>
        [HttpDelete("{id}")]
        public ActionResult DeleteTiket(int id)
        {
            if (id <= 0)
            {
                return BadRequest("ID tiket tidak valid");
            }

            return Ok(new { message = $"Tiket {id} berhasil dihapus", deleted_at = DateTime.Now });
        }

        private double CalculatePrice(int jadwalId, JenisKendaraan? jenisKendaraan)
        {
            // Sample pricing logic menggunakan JenisKendaraan enum
            var basePrice = 25000.0;
            
            if (!jenisKendaraan.HasValue || jenisKendaraan == JenisKendaraan.Jalan_Kaki)
                return basePrice;

            return jenisKendaraan switch
            {
                JenisKendaraan.Golongan_I => basePrice + 15000,   // Sepeda
                JenisKendaraan.Golongan_II => basePrice + 25000,  // Motor <500cc
                JenisKendaraan.Golongan_III => basePrice + 35000, // Motor >500cc
                JenisKendaraan.Golongan_IV_A => basePrice + 75000, // Mobil penumpang
                JenisKendaraan.Golongan_IV_B => basePrice + 85000, // Mobil barang
                JenisKendaraan.Golongan_V_A => basePrice + 115000, // Bus 5-7m
                JenisKendaraan.Golongan_V_B => basePrice + 135000, // Truk 5-7m
                JenisKendaraan.Golongan_VI_A => basePrice + 155000, // Bus 7-10m
                JenisKendaraan.Golongan_VI_B => basePrice + 175000, // Truk 7-10m
                JenisKendaraan.Golongan_VII => basePrice + 215000, // Truk 10-12m
                JenisKendaraan.Golongan_VIII => basePrice + 275000, // Truk 12-16m
                JenisKendaraan.Golongan_IX => basePrice + 335000, // Truk >16m
                _ => basePrice + 50000
            };
        }
    }

    public class CreateTiketRequest
    {
        public int pengguna_id { get; set; }
        public int jadwal_id { get; set; }
        public JenisKendaraan? jenis_kendaraan_enum { get; set; } // null = jalan kaki
        public string? plat_nomor { get; set; } = "";
        public int jumlah_penumpang { get; set; } = 1;
    }
}
