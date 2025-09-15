using Microsoft.AspNetCore.Mvc;
using TiketLaut;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PenumpangController : ControllerBase
    {
        // SINGLE SOURCE OF TRUTH untuk data Penumpang
        public static readonly List<object> AllPenumpangData = new List<object>
        {
            // User 1 penumpang
            new { penumpang_id = 1, pengguna_id = 1, nama = "Ahmad Suryadi", NIK_penumpang = "3201051505900001" },
            new { penumpang_id = 2, pengguna_id = 1, nama = "Sari Suryadi", NIK_penumpang = "3201053008920002" },
            new { penumpang_id = 7, pengguna_id = 1, nama = "Anak Ahmad", NIK_penumpang = "3201050101180001" }, // Tambahan untuk user 1
            
            // User 2 penumpang  
            new { penumpang_id = 3, pengguna_id = 2, nama = "Siti Nurhaliza", NIK_penumpang = "3578082208850002" },
            new { penumpang_id = 4, pengguna_id = 2, nama = "Budi Santoso", NIK_penumpang = "3273080512880001" },
            
            // User 3 penumpang
            new { penumpang_id = 5, pengguna_id = 3, nama = "Diana Sari", NIK_penumpang = "3671052801950003" },
            new { penumpang_id = 6, pengguna_id = 3, nama = "Anak Budi", NIK_penumpang = "3671053108170001" }
        };

        /// <summary>
        /// Mengambil semua penumpang
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllPenumpang()
        {
            return Ok(AllPenumpangData);
        }

        /// <summary>
        /// Mengambil penumpang berdasarkan ID
        /// </summary>
        [HttpGet("{id}")]
        public ActionResult<object> GetPenumpang(int id)
        {
            var penumpang = AllPenumpangData.FirstOrDefault(p => ((dynamic)p).penumpang_id == id);
            if (penumpang == null)
            {
                return NotFound();
            }
            return Ok(penumpang);
        }

        /// <summary>
        /// Mengambil penumpang berdasarkan pengguna_id (FK)
        /// </summary>
        [HttpGet("user/{penggunaId}")]
        public ActionResult<IEnumerable<object>> GetPenumpangByPengguna(int penggunaId)
        {
            var penumpangList = AllPenumpangData
                .Where(p => ((dynamic)p).pengguna_id == penggunaId)
                .ToList();
            
            if (!penumpangList.Any()) return NotFound($"Tidak ada penumpang untuk pengguna ID {penggunaId}");
            return Ok(penumpangList);
        }

        /// <summary>
        /// Menghitung jumlah penumpang per pengguna
        /// </summary>
        [HttpGet("count/user/{penggunaId}")]
        public ActionResult<object> GetJumlahPenumpangByPengguna(int penggunaId)
        {
            var jumlah = AllPenumpangData.Count(p => ((dynamic)p).pengguna_id == penggunaId);
            return Ok(new { pengguna_id = penggunaId, jumlah_penumpang = jumlah });
        }

        /// <summary>
        /// Membuat penumpang baru
        /// </summary>
        [HttpPost]
        public ActionResult<object> CreatePenumpang([FromBody] dynamic penumpangData)
        {
            try
            {
                var newId = AllPenumpangData.Count > 0 
                    ? AllPenumpangData.Max(p => ((dynamic)p).penumpang_id) + 1 
                    : 1;

                var newPenumpang = new
                {
                    penumpang_id = newId,
                    pengguna_id = (int)penumpangData.pengguna_id,
                    nama = (string)penumpangData.nama,
                    NIK_penumpang = (string)penumpangData.NIK_penumpang
                };

                AllPenumpangData.Add(newPenumpang);
                return Ok(new { message = "Penumpang berhasil ditambahkan", data = newPenumpang });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Update penumpang
        /// </summary>
        [HttpPut("{id}")]
        public ActionResult<object> UpdatePenumpang(int id, [FromBody] dynamic penumpangData)
        {
            try
            {
                var existingIndex = AllPenumpangData.FindIndex(p => ((dynamic)p).penumpang_id == id);
                if (existingIndex == -1)
                {
                    return NotFound();
                }

                var updatedPenumpang = new
                {
                    penumpang_id = id,
                    pengguna_id = (int)penumpangData.pengguna_id,
                    nama = (string)penumpangData.nama,
                    NIK_penumpang = (string)penumpangData.NIK_penumpang
                };

                AllPenumpangData[existingIndex] = updatedPenumpang;
                return Ok(new { message = "Penumpang berhasil diperbarui", data = updatedPenumpang });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Hapus penumpang
        /// </summary>
        [HttpDelete("{id}")]
        public ActionResult DeletePenumpang(int id)
        {
            var penumpang = AllPenumpangData.FirstOrDefault(p => ((dynamic)p).penumpang_id == id);
            if (penumpang == null)
            {
                return NotFound();
            }

            AllPenumpangData.Remove(penumpang);
            return Ok(new { message = "Penumpang berhasil dihapus" });
        }
    }
}
