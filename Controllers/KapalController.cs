using Microsoft.AspNetCore.Mvc;
using TiketLaut;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class KapalController : ControllerBase
    {
        // SINGLE SOURCE OF TRUTH - Data kapal (sesuai dengan atribut class Kapal)
        public static readonly List<object> AllKapalData = new List<object>
        {
            new {
                kapal_id = 1,
                nama_kapal = "KMP Dharma Rucitra",
                kapasitas_penumpang_max = 450,
                kapasitas_kendaraan_max = 50,
                fasilitas = "Ruang Penumpang, Toilet, Kantin, Mushola, WiFi",
                deskripsi = "Kapal ferry dengan fasilitas lengkap untuk rute Ketapang - Gilimanuk"
            },
            new {
                kapal_id = 2,
                nama_kapal = "KMP Bahtera Jaya",
                kapasitas_penumpang_max = 600,
                kapasitas_kendaraan_max = 75,
                fasilitas = "AC, Ruang VIP, Toilet, Kantin, Mushola, WiFi, Area Bermain Anak",
                deskripsi = "Kapal ferry modern dengan fasilitas premium untuk rute Bakauheni - Merak"
            },
            new {
                kapal_id = 3,
                nama_kapal = "KMP Legundi",
                kapasitas_penumpang_max = 380,
                kapasitas_kendaraan_max = 45,
                fasilitas = "Ruang Penumpang, Toilet, Kantin, WiFi",
                deskripsi = "Kapal ferry untuk rute Padangbai - Lembar dengan fasilitas standar"
            },
            new {
                kapal_id = 4,
                nama_kapal = "KMP Nusantara",
                kapasitas_penumpang_max = 300,
                kapasitas_kendaraan_max = 35,
                fasilitas = "Ruang Penumpang, Toilet, Kantin",
                deskripsi = "Kapal ferry untuk rute Tanjung Perak - Pelabuhan Ratu"
            }
        };

        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllKapal()
        {
            return Ok(AllKapalData);
        }

        [HttpGet("{id}")]
        public ActionResult<object> GetKapal(int id)
        {
            var kapal = AllKapalData.FirstOrDefault(k => ((dynamic)k).kapal_id == id);
            if (kapal == null) return NotFound($"Kapal dengan ID {id} tidak ditemukan");
            return Ok(kapal);
        }
    }
}