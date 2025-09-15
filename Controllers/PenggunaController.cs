using Microsoft.AspNetCore.Mvc;
using TiketLaut;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PenggunaController : ControllerBase
    {
        // SINGLE SOURCE OF TRUTH - Data pengguna (sesuai dengan atribut class Pengguna)
        private static readonly List<object> AllPenggunaData = new List<object>
        {
            new {
                pengguna_id = 1,
                nama = "Ahmad Suryadi",
                email = "ahmad.suryadi@email.com",
                password = "hashed_password_1",
                no_hp = "081234567890",
                jenis_kelamin = "Laki-laki",
                tanggal_lahir = new DateTime(1990, 5, 15),
                kewarganegaraan = "Indonesia",
                alamat = "Jl. Sudirman No. 123, Jakarta",
                tanggal_daftar = new DateTime(2023, 1, 15)
            },
            new {
                pengguna_id = 2,
                nama = "Siti Nurhaliza",
                email = "siti.nurhaliza@email.com",
                password = "hashed_password_2",
                no_hp = "082345678901",
                jenis_kelamin = "Perempuan",
                tanggal_lahir = new DateTime(1985, 8, 22),
                kewarganegaraan = "Indonesia",
                alamat = "Jl. Diponegoro No. 456, Surabaya",
                tanggal_daftar = new DateTime(2023, 3, 10)
            },
            new {
                pengguna_id = 3,
                nama = "Budi Santoso",
                email = "budi.santoso@email.com",
                password = "hashed_password_3",
                no_hp = "083456789012",
                jenis_kelamin = "Laki-laki",
                tanggal_lahir = new DateTime(1992, 12, 3),
                kewarganegaraan = "Indonesia",
                alamat = "Jl. Gajah Mada No. 789, Denpasar",
                tanggal_daftar = new DateTime(2023, 6, 20)
            },
            new {
                pengguna_id = 4,
                nama = "Dewi Kartika",
                email = "dewi.kartika@email.com",
                password = "hashed_password_4",
                no_hp = "084567890123",
                jenis_kelamin = "Perempuan",
                tanggal_lahir = new DateTime(1988, 4, 18),
                kewarganegaraan = "Indonesia",
                alamat = "Jl. Ahmad Yani No. 321, Bandar Lampung",
                tanggal_daftar = new DateTime(2022, 11, 5)
            }
        };

        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllPengguna()
        {
            return Ok(AllPenggunaData);
        }

        [HttpGet("{id}")]
        public ActionResult<object> GetPengguna(int id)
        {
            var pengguna = AllPenggunaData.FirstOrDefault(p => ((dynamic)p).pengguna_id == id);
            if (pengguna == null) return NotFound($"Pengguna dengan ID {id} tidak ditemukan");
            return Ok(pengguna);
        }

        [HttpGet("email/{email}")]
        public ActionResult<object> GetPenggunaByEmail(string email)
        {
            var pengguna = AllPenggunaData.FirstOrDefault(p => 
                ((dynamic)p).email.ToString().Equals(email, StringComparison.OrdinalIgnoreCase));
            if (pengguna == null) return NotFound($"Pengguna dengan email {email} tidak ditemukan");
            return Ok(pengguna);
        }
    }

    public class CreatePenggunaRequest
    {
        public string nama { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string no_hp { get; set; } = string.Empty;
        public string jenis_kelamin { get; set; } = string.Empty;
        public DateTime tanggal_lahir { get; set; }
        public string kewarganegaraan { get; set; } = string.Empty;
        public string alamat { get; set; } = string.Empty;
    }
}