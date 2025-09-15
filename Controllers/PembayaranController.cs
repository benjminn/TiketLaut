using Microsoft.AspNetCore.Mvc;
using TiketLaut;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PembayaranController : ControllerBase
    {
        // SINGLE SOURCE OF TRUTH - Data pembayaran
        private static readonly List<object> AllPembayaranData = new List<object>
        {
            new {
                pembayaran_id = 1,
                tiket_id = 1,
                pengguna_id = 1,
                metode_pembayaran = "Transfer Bank",
                nama_bank = "BCA",
                nomor_rekening = "1234567890",
                jumlah_pembayaran = 40000.0,
                status_pembayaran = "Berhasil",
                tanggal_pembayaran = DateTime.Now.AddHours(-2),
                kode_transaksi = "TXN001234567",
                biaya_admin = 2500.0,
                total_dibayar = 42500.0,
                bukti_pembayaran = "bukti_001.jpg"
            },
            new {
                pembayaran_id = 2,
                tiket_id = 2,
                pengguna_id = 2,
                metode_pembayaran = "E-Wallet",
                nama_bank = "OVO",
                nomor_rekening = "082345678901",
                jumlah_pembayaran = 65000.0,
                status_pembayaran = "Berhasil",
                tanggal_pembayaran = DateTime.Now.AddHours(-1),
                kode_transaksi = "TXN001234568",
                biaya_admin = 1500.0,
                total_dibayar = 66500.0,
                bukti_pembayaran = "bukti_002.jpg"
            },
            new {
                pembayaran_id = 3,
                tiket_id = 3,
                pengguna_id = 3,
                metode_pembayaran = "Transfer Bank",
                nama_bank = "Mandiri",
                nomor_rekening = "9876543210",
                jumlah_pembayaran = 90000.0,
                status_pembayaran = "Pending",
                tanggal_pembayaran = DateTime.Now.AddMinutes(-30),
                kode_transaksi = "TXN001234569",
                biaya_admin = 3000.0,
                total_dibayar = 93000.0,
                bukti_pembayaran = ""
            },
            new {
                pembayaran_id = 4,
                tiket_id = 4,
                pengguna_id = 4,
                metode_pembayaran = "Credit Card",
                nama_bank = "VISA",
                nomor_rekening = "****1234",
                jumlah_pembayaran = 55000.0,
                status_pembayaran = "Gagal",
                tanggal_pembayaran = DateTime.Now.AddMinutes(-15),
                kode_transaksi = "TXN001234570",
                biaya_admin = 2000.0,
                total_dibayar = 57000.0,
                bukti_pembayaran = ""
            },
            new {
                pembayaran_id = 5,
                tiket_id = 1,
                pengguna_id = 1,
                metode_pembayaran = "QRIS",
                nama_bank = "DANA",
                nomor_rekening = "081234567890",
                jumlah_pembayaran = 120000.0,
                status_pembayaran = "Berhasil",
                tanggal_pembayaran = DateTime.Now.AddDays(-1),
                kode_transaksi = "TXN001234571",
                biaya_admin = 1000.0,
                total_dibayar = 121000.0,
                bukti_pembayaran = "bukti_005.jpg"
            }
        };

        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllPembayaran()
        {
            return Ok(AllPembayaranData);
        }

        [HttpGet("{id}")]
        public ActionResult<object> GetPembayaran(int id)
        {
            var pembayaran = AllPembayaranData.FirstOrDefault(p => ((dynamic)p).pembayaran_id == id);
            if (pembayaran == null) return NotFound($"Pembayaran dengan ID {id} tidak ditemukan");
            return Ok(pembayaran);
        }

        [HttpGet("tiket/{tiketId}")]
        public ActionResult<IEnumerable<object>> GetPembayaranByTiket(int tiketId)
        {
            var pembayarans = AllPembayaranData.Where(p => 
                ((dynamic)p).tiket_id == tiketId).ToList();
            return Ok(pembayarans);
        }

        [HttpGet("pengguna/{penggunaId}")]
        public ActionResult<IEnumerable<object>> GetPembayaranByPengguna(int penggunaId)
        {
            var pembayarans = AllPembayaranData.Where(p => 
                ((dynamic)p).pengguna_id == penggunaId).ToList();
            return Ok(pembayarans);
        }

        [HttpGet("status/{status}")]
        public ActionResult<IEnumerable<object>> GetPembayaranByStatus(string status)
        {
            var pembayarans = AllPembayaranData.Where(p => 
                ((dynamic)p).status_pembayaran.ToString()
                .Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(pembayarans);
        }

        [HttpGet("metode/{metode}")]
        public ActionResult<IEnumerable<object>> GetPembayaranByMetode(string metode)
        {
            var pembayarans = AllPembayaranData.Where(p => 
                ((dynamic)p).metode_pembayaran.ToString()
                .Contains(metode, StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(pembayarans);
        }

        [HttpGet("transaksi/{kodeTransaksi}")]
        public ActionResult<object> GetPembayaranByKodeTransaksi(string kodeTransaksi)
        {
            var pembayaran = AllPembayaranData.FirstOrDefault(p => 
                ((dynamic)p).kode_transaksi.ToString()
                .Equals(kodeTransaksi, StringComparison.OrdinalIgnoreCase));
            if (pembayaran == null) return NotFound($"Pembayaran dengan kode transaksi {kodeTransaksi} tidak ditemukan");
            return Ok(pembayaran);
        }

        [HttpGet("laporan/harian")]
        public ActionResult<object> GetLaporanHarian([FromQuery] DateTime? tanggal = null)
        {
            var targetDate = tanggal ?? DateTime.Today;
            var pembayaranHarian = AllPembayaranData.Where(p => 
            {
                var dynamic_p = (dynamic)p;
                return ((DateTime)dynamic_p.tanggal_pembayaran).Date == targetDate.Date &&
                       dynamic_p.status_pembayaran == "Berhasil";
            }).ToList();

            var totalTransaksi = pembayaranHarian.Count;
            var totalPendapatan = pembayaranHarian.Sum(p => (double)((dynamic)p).total_dibayar);

            var laporan = new
            {
                tanggal = targetDate.ToString("yyyy-MM-dd"),
                total_transaksi = totalTransaksi,
                total_pendapatan = totalPendapatan,
                detail_transaksi = pembayaranHarian
            };

            return Ok(laporan);
        }

        [HttpPost]
        public ActionResult<object> CreatePembayaran([FromBody] CreatePembayaranRequest request)
        {
            if (request == null) return BadRequest("Data pembayaran tidak valid");
            
            var biayaAdmin = CalculateBiayaAdmin(request.metode_pembayaran, request.jumlah_pembayaran);
            var totalDibayar = request.jumlah_pembayaran + biayaAdmin;
            
            var newPembayaran = new
            {
                pembayaran_id = AllPembayaranData.Count + 1,
                tiket_id = request.tiket_id,
                pengguna_id = request.pengguna_id,
                metode_pembayaran = request.metode_pembayaran,
                nama_bank = request.nama_bank,
                nomor_rekening = request.nomor_rekening,
                jumlah_pembayaran = request.jumlah_pembayaran,
                status_pembayaran = "Pending",
                tanggal_pembayaran = DateTime.Now,
                kode_transaksi = GenerateKodeTransaksi(),
                biaya_admin = biayaAdmin,
                total_dibayar = totalDibayar,
                bukti_pembayaran = ""
            };

            AllPembayaranData.Add(newPembayaran);
            return CreatedAtAction(nameof(GetPembayaran), new { id = newPembayaran.pembayaran_id }, newPembayaran);
        }

        [HttpPut("{id}/status")]
        public ActionResult UpdateStatusPembayaran(int id, [FromBody] UpdatePembayaranStatusRequest request)
        {
            var pembayaran = AllPembayaranData.FirstOrDefault(p => ((dynamic)p).pembayaran_id == id);
            if (pembayaran == null) return NotFound($"Pembayaran dengan ID {id} tidak ditemukan");

            return Ok(new { 
                message = $"Status pembayaran {id} berhasil diupdate menjadi {request.status}", 
                updated_at = DateTime.Now 
            });
        }

        private static double CalculateBiayaAdmin(string metode, double jumlah)
        {
            return metode.ToLower() switch
            {
                "transfer bank" => Math.Max(2500, jumlah * 0.01),
                "e-wallet" => 1500,
                "qris" => 1000,
                "credit card" => Math.Max(2000, jumlah * 0.015),
                _ => 2000
            };
        }

        private static string GenerateKodeTransaksi()
        {
            return $"TXN{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";
        }
    }

    public class CreatePembayaranRequest
    {
        public int tiket_id { get; set; }
        public int pengguna_id { get; set; }
        public string metode_pembayaran { get; set; } = string.Empty;
        public string nama_bank { get; set; } = string.Empty;
        public string nomor_rekening { get; set; } = string.Empty;
        public double jumlah_pembayaran { get; set; }
    }

    public class UpdatePembayaranStatusRequest
    {
        public string status { get; set; } = string.Empty;
    }
}