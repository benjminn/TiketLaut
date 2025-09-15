using Microsoft.AspNetCore.Mvc;
using TiketLaut;
using TiketLaut.Controllers;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RincianPenumpangController : ControllerBase
    {
        // SINGLE SOURCE OF TRUTH - Junction table dengan business rule: 1 tiket = 1 user only
        public static readonly List<object> AllRincianPenumpangData = new List<object>
        {
            // Tiket ID 1 - Semua penumpang dari USER 1 
            new { rincian_penumpang_id = 1, tiket_id = 1, penumpang_id = 1 }, // Ahmad Suryadi (user 1)
            new { rincian_penumpang_id = 2, tiket_id = 1, penumpang_id = 2 }, // Sari Suryadi (user 1)
            new { rincian_penumpang_id = 3, tiket_id = 1, penumpang_id = 7 }, // Anak Ahmad (user 1)
            
            // Tiket ID 2 - Semua penumpang dari USER 2
            new { rincian_penumpang_id = 4, tiket_id = 2, penumpang_id = 3 }, // Siti Nurhaliza (user 2)
            new { rincian_penumpang_id = 5, tiket_id = 2, penumpang_id = 4 }, // Budi Santoso (user 2)
            
            // Tiket ID 3 - Semua penumpang dari USER 3
            new { rincian_penumpang_id = 6, tiket_id = 3, penumpang_id = 5 }, // Diana Sari (user 3)
            new { rincian_penumpang_id = 7, tiket_id = 3, penumpang_id = 6 }, // Anak Budi (user 3)
            
            // Tiket ID 4 - Semua penumpang dari USER 1 (tidak campur dengan user lain)
            new { rincian_penumpang_id = 8, tiket_id = 4, penumpang_id = 1 }, // Ahmad Suryadi (user 1)
            new { rincian_penumpang_id = 9, tiket_id = 4, penumpang_id = 2 }  // Sari Suryadi (user 1)
        };

        /// <summary>
        /// Mengambil semua rincian penumpang
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllRincianPenumpang()
        {
            return Ok(AllRincianPenumpangData);
        }

        /// <summary>
        /// Mengambil rincian penumpang dengan data lengkap (JOIN dengan Penumpang)
        /// </summary>
        [HttpGet("detailed")]
        public ActionResult<IEnumerable<object>> GetDetailedRincianPenumpang()
        {
            var detailedData = AllRincianPenumpangData.Select(rp => {
                var rincian = (dynamic)rp;
                
                // Ambil data penumpang berdasarkan FK
                var penumpang = PenumpangController.AllPenumpangData.FirstOrDefault(p => ((dynamic)p).penumpang_id == rincian.penumpang_id);
                var penumpangData = (dynamic)penumpang!;
                
                return new {
                    rincian_penumpang_id = rincian.rincian_penumpang_id,
                    tiket_id = rincian.tiket_id,
                    penumpang_id = rincian.penumpang_id,
                    // Data dari Penumpang via FK - tidak ada duplikasi
                    nama = penumpangData.nama,
                    NIK_penumpang = penumpangData.NIK_penumpang,
                    pengguna_id = penumpangData.pengguna_id
                };
            }).ToList();

            return Ok(detailedData);
        }

        /// <summary>
        /// Mengambil rincian penumpang berdasarkan ID
        /// </summary>
        [HttpGet("{id}")]
        public ActionResult<object> GetRincianPenumpang(int id)
        {
            var rincian = AllRincianPenumpangData.FirstOrDefault(rp => ((dynamic)rp).rincian_penumpang_id == id);
            if (rincian == null) return NotFound($"Rincian penumpang dengan ID {id} tidak ditemukan");
            return Ok(rincian);
        }

        /// <summary>
        /// Mengambil semua penumpang dalam satu tiket
        /// </summary>
        [HttpGet("tiket/{tiketId}")]
        public ActionResult<IEnumerable<object>> GetPenumpangByTiket(int tiketId)
        {
            var rincianTiket = AllRincianPenumpangData
                .Where(rp => ((dynamic)rp).tiket_id == tiketId)
                .Select(rp => {
                    var rincian = (dynamic)rp;
                    
                    // Ambil data penumpang berdasarkan FK
                    var penumpang = PenumpangController.AllPenumpangData.FirstOrDefault(p => ((dynamic)p).penumpang_id == rincian.penumpang_id);
                    var penumpangData = (dynamic)penumpang!;
                    
                    return new {
                        rincian_penumpang_id = rincian.rincian_penumpang_id,
                        penumpang_id = rincian.penumpang_id,
                        // Data dari Penumpang - tidak ada duplikasi
                        nama = penumpangData.nama,
                        NIK_penumpang = penumpangData.NIK_penumpang
                    };
                })
                .ToList();

            if (!rincianTiket.Any()) return NotFound($"Tidak ada penumpang untuk tiket ID {tiketId}");
            return Ok(rincianTiket);
        }

        /// <summary>
        /// Menghitung jumlah penumpang per tiket
        /// </summary>
        [HttpGet("tiket/{tiketId}/count")]
        public ActionResult<object> GetJumlahPenumpangByTiket(int tiketId)
        {
            var jumlah = AllRincianPenumpangData.Count(rp => ((dynamic)rp).tiket_id == tiketId);
            return Ok(new { tiket_id = tiketId, jumlah_penumpang = jumlah });
        }

        /// <summary>
        /// Membuat rincian penumpang baru (Menambah penumpang ke tiket)
        /// BUSINESS RULE: Satu tiket hanya boleh berisi penumpang dari user yang sama
        /// </summary>
        [HttpPost]
        public ActionResult<object> CreateRincianPenumpang([FromBody] dynamic rincianData)
        {
            try
            {
                var tiketId = (int)rincianData.tiket_id;
                var penumpangId = (int)rincianData.penumpang_id;
                
                // 1. Validasi penumpang exists
                var penumpang = PenumpangController.AllPenumpangData.FirstOrDefault(p => ((dynamic)p).penumpang_id == penumpangId);
                if (penumpang == null)
                {
                    return BadRequest($"Penumpang dengan ID {penumpangId} tidak ditemukan");
                }
                
                var penumpangData = (dynamic)penumpang;
                var newPenumpangUserId = penumpangData.pengguna_id;

                // 2. BUSINESS RULE VALIDATION: Check if tiket sudah ada penumpang dari user lain
                var existingRincianInTiket = AllRincianPenumpangData
                    .Where(rp => ((dynamic)rp).tiket_id == tiketId)
                    .ToList();
                
                if (existingRincianInTiket.Any())
                {
                    // Ambil user_id dari penumpang pertama yang sudah ada di tiket
                    var firstRincian = (dynamic)existingRincianInTiket.First();
                    var firstPenumpang = PenumpangController.AllPenumpangData
                        .FirstOrDefault(p => ((dynamic)p).penumpang_id == firstRincian.penumpang_id);
                    var firstPenumpangData = (dynamic)firstPenumpang!;
                    var existingUserId = firstPenumpangData.pengguna_id;
                    
                    // BUSINESS RULE: Penumpang baru harus dari user yang sama
                    if (newPenumpangUserId != existingUserId)
                    {
                        return BadRequest($"BUSINESS RULE VIOLATION: Tiket {tiketId} sudah berisi penumpang dari User {existingUserId}. " +
                                        $"Penumpang baru (User {newPenumpangUserId}) tidak bisa ditambahkan. " +
                                        $"Satu tiket hanya boleh berisi penumpang dari user yang sama.");
                    }
                }

                var newId = AllRincianPenumpangData.Count > 0 
                    ? AllRincianPenumpangData.Max(rp => ((dynamic)rp).rincian_penumpang_id) + 1 
                    : 1;

                var newRincian = new
                {
                    rincian_penumpang_id = newId,
                    tiket_id = tiketId,
                    penumpang_id = penumpangId
                };

                AllRincianPenumpangData.Add(newRincian);
                return CreatedAtAction(nameof(GetRincianPenumpang), new { id = newId }, new { 
                    success = true,
                    message = $"Penumpang {penumpangData.nama} berhasil ditambahkan ke Tiket {tiketId}",
                    data = newRincian 
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating rincian penumpang: {ex.Message}");
            }
        }

        /// <summary>
        /// Mengupdate rincian penumpang (tiket_id atau penumpang_id)
        /// </summary>
        [HttpPut("{id}")]
        public ActionResult<object> UpdateRincianPenumpang(int id, [FromBody] dynamic rincianData)
        {
            try
            {
                var existingRincian = AllRincianPenumpangData.FirstOrDefault(rp => ((dynamic)rp).rincian_penumpang_id == id);
                if (existingRincian == null) return NotFound($"Rincian penumpang dengan ID {id} tidak ditemukan");

                // Validasi penumpang_id masih ada
                var penumpang = PenumpangController.AllPenumpangData.FirstOrDefault(p => ((dynamic)p).penumpang_id == (int)rincianData.penumpang_id);
                if (penumpang == null)
                {
                    return BadRequest($"Penumpang dengan ID {rincianData.penumpang_id} tidak ditemukan");
                }

                var updatedRincian = new
                {
                    rincian_penumpang_id = id,
                    tiket_id = (int)rincianData.tiket_id,
                    penumpang_id = (int)rincianData.penumpang_id
                    // Pure junction table - hanya FK
                };

                var index = AllRincianPenumpangData.FindIndex(rp => ((dynamic)rp).rincian_penumpang_id == id);
                AllRincianPenumpangData[index] = updatedRincian;

                return Ok(updatedRincian);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating rincian penumpang: {ex.Message}");
            }
        }

        /// <summary>
        /// Menghapus rincian penumpang (Mengeluarkan penumpang dari tiket)
        /// </summary>
        [HttpDelete("{id}")]
        public ActionResult DeleteRincianPenumpang(int id)
        {
            var rincian = AllRincianPenumpangData.FirstOrDefault(rp => ((dynamic)rp).rincian_penumpang_id == id);
            if (rincian == null) return NotFound($"Rincian penumpang dengan ID {id} tidak ditemukan");

            AllRincianPenumpangData.Remove(rincian);
            return Ok($"Rincian penumpang dengan ID {id} berhasil dihapus");
        }

        /// <summary>
        /// Statistik penumpang per tiket
        /// </summary>
        [HttpGet("stats")]
        public ActionResult<object> GetRincianPenumpangStats()
        {
            var stats = AllRincianPenumpangData
                .GroupBy(rp => ((dynamic)rp).tiket_id)
                .Select(g => new
                {
                    tiket_id = g.Key,
                    jumlah_penumpang = g.Count()
                })
                .ToList();

            return Ok(stats);
        }

        /// <summary>
        /// Validasi business rule: Check apakah tiket sudah memiliki konsistensi user
        /// </summary>
        [HttpGet("validate-user-consistency")]
        public ActionResult<object> ValidateUserConsistency()
        {
            var results = AllRincianPenumpangData
                .GroupBy(rp => ((dynamic)rp).tiket_id)
                .Select(tiketGroup => {
                    var tiketId = tiketGroup.Key;
                    
                    // Get all users dalam tiket ini
                    var usersInTiket = tiketGroup
                        .Select(rp => {
                            var rincian = (dynamic)rp;
                            var penumpang = PenumpangController.AllPenumpangData
                                .FirstOrDefault(p => ((dynamic)p).penumpang_id == rincian.penumpang_id);
                            return ((dynamic)penumpang!).pengguna_id;
                        })
                        .Distinct()
                        .ToList();
                    
                    var isConsistent = usersInTiket.Count == 1;
                    
                    return new {
                        tiket_id = tiketId,
                        jumlah_penumpang = tiketGroup.Count(),
                        users_in_tiket = usersInTiket,
                        is_consistent = isConsistent,
                        status = isConsistent ? "✅ VALID" : "❌ VIOLATION",
                        message = isConsistent 
                            ? $"Tiket {tiketId} valid - hanya berisi penumpang dari User {usersInTiket.First()}"
                            : $"Tiket {tiketId} INVALID - berisi penumpang dari multiple users: [{string.Join(", ", usersInTiket)}]"
                    };
                })
                .ToList();
                
            var totalViolations = results.Count(r => !(bool)((dynamic)r).is_consistent);
            
            return Ok(new {
                validation_summary = new {
                    total_tikets = results.Count,
                    valid_tikets = results.Count - totalViolations,
                    violation_tikets = totalViolations,
                    overall_status = totalViolations == 0 ? "✅ ALL VALID" : $"❌ {totalViolations} VIOLATIONS"
                },
                tiket_details = results
            });
        }
    }
}