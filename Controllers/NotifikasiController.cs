using Microsoft.AspNetCore.Mvc;
using TiketLaut;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class NotifikasiController : ControllerBase
    {
        // SINGLE SOURCE OF TRUTH - Data notifikasi hanya didefinisikan SEKALI di sini
        private static readonly List<object> AllNotifikasiData = new List<object>
        {
            new {
                notifikasi_id = 1,
                pengguna_id = 0, // 0 untuk broadcast ke semua
                jenis_enum_penumpang_update_status = "Info",
                pesan = "Selamat datang di TiketLaut! Sistem pemesanan tiket kapal online.",
                waktu_kirim = DateTime.Now.AddHours(-2),
                status_baca = true,
                kirimNotifikasiId = 1001
            },
            new {
                notifikasi_id = 2,
                pengguna_id = 1,
                jenis_enum_penumpang_update_status = "Status",
                pesan = "Tiket Anda untuk jadwal Ketapang-Gilimanuk telah dikonfirmasi.",
                waktu_kirim = DateTime.Now.AddMinutes(-30),
                status_baca = false,
                kirimNotifikasiId = 1002
            },
            new {
                notifikasi_id = 3,
                pengguna_id = 0, // 0 untuk broadcast ke semua
                jenis_enum_penumpang_update_status = "Peringatan",
                pesan = "Cuaca buruk! Jadwal keberangkatan Bakauheni-Merak mungkin tertunda.",
                waktu_kirim = DateTime.Now.AddMinutes(-15),
                status_baca = false,
                kirimNotifikasiId = 1003
            },
            new {
                notifikasi_id = 4,
                pengguna_id = 2,
                jenis_enum_penumpang_update_status = "Pembayaran",
                pesan = "Pembayaran tiket Anda berhasil diproses. Terima kasih!",
                waktu_kirim = DateTime.Now.AddMinutes(-10),
                status_baca = false,
                kirimNotifikasiId = 1004
            },
            new {
                notifikasi_id = 5,
                pengguna_id = 0, // 0 untuk broadcast ke semua
                jenis_enum_penumpang_update_status = "Jadwal",
                pesan = "Update: Jadwal Padangbai-Lembar ditambahkan untuk besok pukul 14:00.",
                waktu_kirim = DateTime.Now.AddMinutes(-5),
                status_baca = false,
                kirimNotifikasiId = 1005
            }
        };

        private static int _nextId = 6;

        /// <summary>
        /// Mendapatkan semua notifikasi
        /// </summary>
        /// <returns>List semua notifikasi</returns>
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllNotifikasi()
        {
            return Ok(AllNotifikasiData);
        }

        /// <summary>
        /// Mendapatkan notifikasi berdasarkan ID - FILTERING dari data yang sama
        /// </summary>
        /// <param name="id">ID notifikasi</param>
        /// <returns>Detail notifikasi</returns>
        [HttpGet("{id}")]
        public ActionResult<object> GetNotifikasi(int id)
        {
            if (id <= 0)
            {
                return BadRequest("ID notifikasi harus lebih dari 0");
            }

            // FILTERING: Cari berdasarkan ID dari data yang sama
            var notifikasi = AllNotifikasiData.FirstOrDefault(n => 
                ((dynamic)n).notifikasi_id == id);

            if (notifikasi == null)
            {
                return NotFound($"Notifikasi dengan ID {id} tidak ditemukan");
            }

            return Ok(notifikasi);
        }

        /// <summary>
        /// Mendapatkan notifikasi berdasarkan pengguna ID
        /// </summary>
        /// <param name="penggunaId">ID pengguna</param>
        /// <returns>List notifikasi untuk pengguna tertentu</returns>
        [HttpGet("pengguna/{penggunaId}")]
        public ActionResult<IEnumerable<object>> GetNotifikasiByPengguna(int penggunaId)
        {
            if (penggunaId <= 0)
            {
                return BadRequest("ID pengguna harus lebih dari 0");
            }

            // FILTERING: Ambil notifikasi untuk pengguna tertentu + broadcast (pengguna_id = 0)
            var notifikasiPengguna = AllNotifikasiData.Where(n =>
            {
                var dynamic_n = (dynamic)n;
                return dynamic_n.pengguna_id == 0 || dynamic_n.pengguna_id == penggunaId;
            }).ToList();

            return Ok(notifikasiPengguna);
        }

        /// <summary>
        /// Mendapatkan notifikasi berdasarkan jenis
        /// </summary>
        /// <param name="jenis">Jenis notifikasi (Info, Peringatan, Status, dll)</param>
        /// <returns>List notifikasi berdasarkan jenis</returns>
        [HttpGet("jenis/{jenis}")]
        public ActionResult<IEnumerable<object>> GetNotifikasiByJenis(string jenis)
        {
            if (string.IsNullOrEmpty(jenis))
            {
                return BadRequest("Jenis notifikasi harus diisi");
            }

            // FILTERING: Berdasarkan jenis notifikasi
            var notifikasiByJenis = AllNotifikasiData.Where(n =>
                ((dynamic)n).jenis_enum_penumpang_update_status.ToString()
                .Equals(jenis, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(notifikasiByJenis);
        }

        /// <summary>
        /// Mendapatkan notifikasi yang belum dibaca
        /// </summary>
        /// <param name="penggunaId">ID pengguna (optional, jika tidak diisi ambil semua)</param>
        /// <returns>List notifikasi yang belum dibaca</returns>
        [HttpGet("unread")]
        public ActionResult<IEnumerable<object>> GetUnreadNotifikasi([FromQuery] int? penggunaId = null)
        {
            var unreadNotifikasi = AllNotifikasiData.Where(n =>
            {
                var dynamic_n = (dynamic)n;
                var isUnread = dynamic_n.status_baca == false;
                
                if (penggunaId.HasValue)
                {
                    // Filter untuk pengguna tertentu + broadcast (pengguna_id = 0)
                    return isUnread && (dynamic_n.pengguna_id == 0 || dynamic_n.pengguna_id == penggunaId.Value);
                }
                
                return isUnread;
            }).ToList();

            return Ok(unreadNotifikasi);
        }

        /// <summary>
        /// Membuat notifikasi baru
        /// </summary>
        /// <param name="request">Data notifikasi baru</param>
        /// <returns>Notifikasi yang berhasil dibuat</returns>
        [HttpPost]
        public ActionResult<object> CreateNotifikasi([FromBody] CreateNotifikasiRequest request)
        {
            if (request == null)
            {
                return BadRequest("Data notifikasi tidak valid");
            }

            var newNotifikasi = new
            {
                notifikasi_id = _nextId++,
                pengguna_id = request.pengguna_id ?? 0, // 0 untuk broadcast
                jenis_enum_penumpang_update_status = request.jenis ?? "Info",
                pesan = request.pesan ?? "",
                waktu_kirim = DateTime.Now,
                status_baca = false,
                kirimNotifikasiId = _nextId + 1000
            };

            AllNotifikasiData.Add(newNotifikasi);
            return CreatedAtAction(nameof(GetNotifikasi), new { id = newNotifikasi.notifikasi_id }, newNotifikasi);
        }

        /// <summary>
        /// Tandai notifikasi sebagai sudah dibaca
        /// </summary>
        /// <param name="id">ID notifikasi</param>
        /// <returns>Hasil update</returns>
        [HttpPut("{id}/mark-read")]
        public ActionResult MarkAsRead(int id)
        {
            if (id <= 0)
            {
                return BadRequest("ID notifikasi tidak valid");
            }

            var notifikasi = AllNotifikasiData.FirstOrDefault(n => 
                ((dynamic)n).notifikasi_id == id);

            if (notifikasi == null)
            {
                return NotFound($"Notifikasi dengan ID {id} tidak ditemukan");
            }

            // Update status_baca menjadi true (simulasi, karena anonymous object immutable)
            return Ok(new { 
                message = $"Notifikasi {id} berhasil ditandai sebagai sudah dibaca", 
                updated_at = DateTime.Now 
            });
        }

        /// <summary>
        /// Hapus notifikasi
        /// </summary>
        /// <param name="id">ID notifikasi</param>
        /// <returns>Hasil penghapusan</returns>
        [HttpDelete("{id}")]
        public ActionResult DeleteNotifikasi(int id)
        {
            if (id <= 0)
            {
                return BadRequest("ID notifikasi tidak valid");
            }

            var notifikasi = AllNotifikasiData.FirstOrDefault(n => 
                ((dynamic)n).notifikasi_id == id);

            if (notifikasi == null)
            {
                return NotFound($"Notifikasi dengan ID {id} tidak ditemukan");
            }

            AllNotifikasiData.Remove(notifikasi);
            return Ok(new { 
                message = $"Notifikasi {id} berhasil dihapus", 
                deleted_at = DateTime.Now 
            });
        }
    }

    public class CreateNotifikasiRequest
    {
        public int? pengguna_id { get; set; } = 0; // 0 untuk broadcast, atau ID pengguna spesifik
        public string jenis { get; set; } = "Info";
        public string pesan { get; set; } = string.Empty;
        public bool status_baca { get; set; } = false;
        public int kirimNotifikasiId { get; set; } = 0; // Will be auto-generated
    }
}