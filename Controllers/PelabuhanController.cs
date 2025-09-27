using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TiketLaut;
using TiketLaut.Data;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PelabuhanController : ControllerBase
    {
        private readonly TiketLautDbContext _context;
        private readonly ILogger<PelabuhanController> _logger;

        public PelabuhanController(TiketLautDbContext context, ILogger<PelabuhanController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all harbors/ports
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pelabuhan>>> GetAllPelabuhan()
        {
            try
            {
                var pelabuhans = await _context.Pelabuhans.ToListAsync();
                _logger.LogInformation("Retrieved {Count} pelabuhans from database", pelabuhans.Count);
                return Ok(pelabuhans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pelabuhans from database");
                return StatusCode(500, "Internal server error while retrieving pelabuhans");
            }
        }

        /// <summary>
        /// Get harbor by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Pelabuhan>> GetPelabuhan(int id)
        {
            try
            {
                var pelabuhan = await _context.Pelabuhans.FindAsync(id);
                if (pelabuhan == null)
                {
                    _logger.LogWarning("Pelabuhan with ID {Id} not found", id);
                    return NotFound($"Pelabuhan dengan ID {id} tidak ditemukan");
                }
                return Ok(pelabuhan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pelabuhan with ID {Id}", id);
                return StatusCode(500, "Internal server error while retrieving pelabuhan");
            }
        }

        /// <summary>
        /// Get harbors by province
        /// </summary>
        [HttpGet("provinsi/{provinsi}")]
        public async Task<ActionResult<IEnumerable<Pelabuhan>>> GetPelabuhanByProvinsi(string provinsi)
        {
            try
            {
                var pelabuhanList = await _context.Pelabuhans
                    .Where(p => p.provinsi.ToLower().Contains(provinsi.ToLower()))
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} pelabuhans in province {Province}", pelabuhanList.Count, provinsi);
                return Ok(pelabuhanList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pelabuhans by province {Province}", provinsi);
                return StatusCode(500, "Internal server error while retrieving pelabuhans by province");
            }
        }

        /// <summary>
        /// Get harbors by city
        /// </summary>
        [HttpGet("kota/{kota}")]
        public async Task<ActionResult<IEnumerable<Pelabuhan>>> GetPelabuhanByKota(string kota)
        {
            try
            {
                var pelabuhanList = await _context.Pelabuhans
                    .Where(p => p.kota.ToLower().Contains(kota.ToLower()))
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} pelabuhans in city {City}", pelabuhanList.Count, kota);
                return Ok(pelabuhanList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pelabuhans by city {City}", kota);
                return StatusCode(500, "Internal server error while retrieving pelabuhans by city");
            }
        }

        /// <summary>
        /// Search harbors by name and/or city
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Pelabuhan>>> SearchPelabuhan([FromQuery] string? nama = null, [FromQuery] string? kota = null)
        {
            try
            {
                var query = _context.Pelabuhans.AsQueryable();

                if (!string.IsNullOrEmpty(nama))
                {
                    query = query.Where(p => p.nama_pelabuhan.ToLower().Contains(nama.ToLower()));
                }

                if (!string.IsNullOrEmpty(kota))
                {
                    query = query.Where(p => p.kota.ToLower().Contains(kota.ToLower()));
                }

                var result = await query.ToListAsync();
                _logger.LogInformation("Search found {Count} pelabuhans (nama: {Nama}, kota: {Kota})", result.Count, nama, kota);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching pelabuhans (nama: {Nama}, kota: {Kota})", nama, kota);
                return StatusCode(500, "Internal server error while searching pelabuhans");
            }
        }

        /// <summary>
        /// Create new harbor
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Pelabuhan>> CreatePelabuhan(Pelabuhan pelabuhan)
        {
            try
            {
                _context.Pelabuhans.Add(pelabuhan);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created new pelabuhan with ID {Id}: {Name}", pelabuhan.pelabuhan_id, pelabuhan.nama_pelabuhan);
                return CreatedAtAction(nameof(GetPelabuhan), new { id = pelabuhan.pelabuhan_id }, pelabuhan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pelabuhan {Name}", pelabuhan.nama_pelabuhan);
                return StatusCode(500, "Internal server error while creating pelabuhan");
            }
        }

        /// <summary>
        /// Update existing harbor
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePelabuhan(int id, Pelabuhan pelabuhan)
        {
            if (id != pelabuhan.pelabuhan_id)
            {
                return BadRequest("ID mismatch");
            }

            try
            {
                _context.Entry(pelabuhan).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated pelabuhan with ID {Id}: {Name}", id, pelabuhan.nama_pelabuhan);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PelabuhanExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating pelabuhan with ID {Id}", id);
                return StatusCode(500, "Internal server error while updating pelabuhan");
            }
        }

        /// <summary>
        /// Delete harbor
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePelabuhan(int id)
        {
            try
            {
                var pelabuhan = await _context.Pelabuhans.FindAsync(id);
                if (pelabuhan == null)
                {
                    return NotFound();
                }

                _context.Pelabuhans.Remove(pelabuhan);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted pelabuhan with ID {Id}: {Name}", id, pelabuhan.nama_pelabuhan);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting pelabuhan with ID {Id}", id);
                return StatusCode(500, "Internal server error while deleting pelabuhan");
            }
        }

        private async Task<bool> PelabuhanExists(int id)
        {
            return await _context.Pelabuhans.AnyAsync(e => e.pelabuhan_id == id);
        }
    }
}