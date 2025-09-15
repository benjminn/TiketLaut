using Microsoft.AspNetCore.Mvc;
using TiketLaut;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DetailKendaraanController : ControllerBase
    {
        /// <summary>
        /// Mendapatkan semua jenis kendaraan dengan spesifikasinya
        /// </summary>
        [HttpGet("specifications")]
        public ActionResult<IEnumerable<object>> GetAllSpecifications()
        {
            var allJenisKendaraan = Enum.GetValues<JenisKendaraan>();
            var specifications = allJenisKendaraan.Select(jenis => {
                var spec = DetailKendaraan.GetSpecificationByJenis(jenis);
                return new {
                    jenis_kendaraan = jenis,
                    jenis_kendaraan_string = jenis.ToString(),
                    bobot = spec.Bobot,
                    deskripsi = spec.Deskripsi,
                    spesifikasi_ukuran = spec.SpesifikasiUkuran
                };
            }).ToList();

            return Ok(specifications);
        }

        /// <summary>
        /// Mendapatkan spesifikasi kendaraan berdasarkan jenis
        /// </summary>
        /// <param name="jenisKendaraan">Jenis kendaraan</param>
        [HttpGet("specification/{jenisKendaraan}")]
        public ActionResult<object> GetSpecificationByJenis(JenisKendaraan jenisKendaraan)
        {
            var spec = DetailKendaraan.GetSpecificationByJenis(jenisKendaraan);
            var result = new {
                jenis_kendaraan = jenisKendaraan,
                jenis_kendaraan_string = jenisKendaraan.ToString(),
                bobot = spec.Bobot,
                deskripsi = spec.Deskripsi,
                spesifikasi_ukuran = spec.SpesifikasiUkuran
            };

            return Ok(result);
        }

        /// <summary>
        /// Mendapatkan detail kendaraan untuk jadwal tertentu (dengan harga)
        /// </summary>
        /// <param name="jadwalId">ID Jadwal</param>
        [HttpGet("jadwal/{jadwalId}")]
        public ActionResult<IEnumerable<object>> GetDetailKendaraanByJadwal(int jadwalId)
        {
            if (jadwalId <= 0)
            {
                return BadRequest("ID jadwal harus lebih dari 0");
            }

            // Ambil data jadwal untuk mendapatkan harga
            var jadwal = JadwalController.AllJadwalData.FirstOrDefault(j => 
                ((dynamic)j).jadwal_id == jadwalId);

            if (jadwal == null)
            {
                return NotFound($"Jadwal dengan ID {jadwalId} tidak ditemukan");
            }

            var jadwalData = (dynamic)jadwal;
            var allJenisKendaraan = Enum.GetValues<JenisKendaraan>();
            
            var detailKendaraans = allJenisKendaraan.Select(jenis => {
                var spec = DetailKendaraan.GetSpecificationByJenis(jenis);
                var harga = GetHargaByJenis(jadwalData, jenis);
                
                return new {
                    detail_kendaraan_id = $"{jadwalId}_{(int)jenis}",
                    jadwal_id = jadwalId,
                    jenis_kendaraan = jenis,
                    jenis_kendaraan_string = jenis.ToString(),
                    harga_kendaraan = harga,
                    bobot_unit = spec.Bobot,
                    deskripsi = spec.Deskripsi,
                    spesifikasi_ukuran = spec.SpesifikasiUkuran
                };
            }).ToList();

            return Ok(detailKendaraans);
        }

        /// <summary>
        /// Helper method untuk mendapatkan harga berdasarkan jenis kendaraan dari jadwal
        /// </summary>
        private decimal GetHargaByJenis(dynamic jadwal, JenisKendaraan jenis)
        {
            return jenis switch
            {
                JenisKendaraan.Jalan_Kaki => jadwal.harga_penumpang,
                JenisKendaraan.Golongan_I => jadwal.harga_golongan_I,
                JenisKendaraan.Golongan_II => jadwal.harga_golongan_II,
                JenisKendaraan.Golongan_III => jadwal.harga_golongan_III,
                JenisKendaraan.Golongan_IV_A => jadwal.harga_golongan_IV_A,
                JenisKendaraan.Golongan_IV_B => jadwal.harga_golongan_IV_B,
                JenisKendaraan.Golongan_V_A => jadwal.harga_golongan_V_A,
                JenisKendaraan.Golongan_V_B => jadwal.harga_golongan_V_B,
                JenisKendaraan.Golongan_VI_A => jadwal.harga_golongan_VI_A,
                JenisKendaraan.Golongan_VI_B => jadwal.harga_golongan_VI_B,
                JenisKendaraan.Golongan_VII => jadwal.harga_golongan_VII,
                JenisKendaraan.Golongan_VIII => jadwal.harga_golongan_VIII,
                JenisKendaraan.Golongan_IX => jadwal.harga_golongan_IX,
                _ => 0
            };
        }
    }
}