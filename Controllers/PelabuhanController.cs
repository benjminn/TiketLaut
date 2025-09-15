using Microsoft.AspNetCore.Mvc;
using TiketLaut;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PelabuhanController : ControllerBase
    {
        // SINGLE SOURCE OF TRUTH - Data pelabuhan (sesuai dengan atribut class Pelabuhan)
        public static readonly List<object> AllPelabuhanData = new List<object>
        {
            new {
                pelabuhan_id = 1,
                nama_pelabuhan = "Pelabuhan Merak",
                kota = "Cilegon",
                provinsi = "Banten",
                fasilitas = "Parkir, Toilet, Mushola, Kantin, ATM",
                deskripsi = "Pelabuhan utama penghubung Pulau Jawa dan Sumatera di Selat Sunda"
            },
            new {
                pelabuhan_id = 2,
                nama_pelabuhan = "Pelabuhan Bakauheni",
                kota = "Lampung Selatan",
                provinsi = "Lampung",
                fasilitas = "Parkir, Toilet, Mushola, Kantin, ATM, Ruang VIP",
                deskripsi = "Pelabuhan tersibuk di Lampung untuk penyeberangan ke Pulau Jawa"
            },
            new {
                pelabuhan_id = 3,
                nama_pelabuhan = "Pelabuhan Gilimanuk",
                kota = "Jembrana",
                provinsi = "Bali",
                fasilitas = "Parkir, Toilet, Mushola, Restoran, Toko Souvenir",
                deskripsi = "Pelabuhan di ujung barat Pulau Bali penghubung ke Pulau Jawa"
            },
            new {
                pelabuhan_id = 4,
                nama_pelabuhan = "Pelabuhan Ketapang",
                kota = "Banyuwangi",
                provinsi = "Jawa Timur",
                fasilitas = "Parkir, Toilet, Mushola, Kantin, Mini Market",
                deskripsi = "Pelabuhan di ujung timur Pulau Jawa penghubung ke Pulau Bali"
            },
            new {
                pelabuhan_id = 5,
                nama_pelabuhan = "Pelabuhan Lembar",
                kota = "Lombok Barat",
                provinsi = "Nusa Tenggara Barat",
                fasilitas = "Parkir, Toilet, Mushola, Restoran, ATM, Wifi",
                deskripsi = "Pelabuhan utama di Pulau Lombok untuk penyeberangan antar pulau"
            },
            new {
                pelabuhan_id = 6,
                nama_pelabuhan = "Pelabuhan Padangbai",
                kota = "Karangasem",
                provinsi = "Bali",
                fasilitas = "Parkir, Toilet, Mushola, Warung, Penginapan",
                deskripsi = "Pelabuhan di timur Bali penghubung ke Lombok dan Gili"
            }
        };

        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllPelabuhan()
        {
            return Ok(AllPelabuhanData);
        }

        [HttpGet("{id}")]
        public ActionResult<object> GetPelabuhan(int id)
        {
            var pelabuhan = AllPelabuhanData.FirstOrDefault(p => ((dynamic)p).pelabuhan_id == id);
            if (pelabuhan == null) return NotFound($"Pelabuhan dengan ID {id} tidak ditemukan");
            return Ok(pelabuhan);
        }

        [HttpGet("kode/{kode}")]
        public ActionResult<object> GetPelabuhanByKode(string kode)
        {
            var pelabuhan = AllPelabuhanData.FirstOrDefault(p => 
                ((dynamic)p).kode_pelabuhan.ToString()
                .Equals(kode, StringComparison.OrdinalIgnoreCase));
            if (pelabuhan == null) return NotFound($"Pelabuhan dengan kode {kode} tidak ditemukan");
            return Ok(pelabuhan);
        }

        [HttpGet("provinsi/{provinsi}")]
        public ActionResult<IEnumerable<object>> GetPelabuhanByProvinsi(string provinsi)
        {
            var pelabuhanList = AllPelabuhanData.Where(p => 
            {
                var dynamic_p = (dynamic)p;
                return dynamic_p.provinsi.ToString()
                    .Contains(provinsi, StringComparison.OrdinalIgnoreCase);
            }).ToList();
            return Ok(pelabuhanList);
        }

        [HttpGet("kota/{kota}")]
        public ActionResult<IEnumerable<object>> GetPelabuhanByKota(string kota)
        {
            var pelabuhanList = AllPelabuhanData.Where(p => 
            {
                var dynamic_p = (dynamic)p;
                return dynamic_p.kota.ToString()
                    .Contains(kota, StringComparison.OrdinalIgnoreCase);
            }).ToList();
            return Ok(pelabuhanList);
        }

        [HttpGet("search")]
        public ActionResult<IEnumerable<object>> SearchPelabuhan([FromQuery] string? nama = null, [FromQuery] string? kota = null)
        {
            var result = AllPelabuhanData.Where(p => 
            {
                var dynamic_p = (dynamic)p;
                bool matchNama = string.IsNullOrEmpty(nama) || 
                    dynamic_p.nama_pelabuhan.ToString().Contains(nama, StringComparison.OrdinalIgnoreCase);
                bool matchKota = string.IsNullOrEmpty(kota) || 
                    dynamic_p.kota.ToString().Contains(kota, StringComparison.OrdinalIgnoreCase);
                return matchNama && matchKota;
            }).ToList();

            return Ok(result);
        }
    }
}