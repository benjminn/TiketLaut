using Microsoft.AspNetCore.Mvc;
using TiketLaut;

namespace TiketLaut.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private static readonly List<object> AllAdminData = new List<object>
        {
            new {
                admin_id = 1,
                nama = "Super Administrator",
                username = "superadmin",
                email = "superadmin@tiketlaut.com",
                password = "hashedPassword123",
                role = "SuperAdmin"
            },
            new {
                admin_id = 2,
                nama = "Ahmad Operasional",
                username = "admin_operasional",
                email = "operasional@tiketlaut.com",
                password = "hashedPassword456", 
                role = "OperationAdmin"
            },
            new {
                admin_id = 3,
                nama = "Siti Customer Service",
                username = "admin_customer",
                email = "customer@tiketlaut.com",
                password = "hashedPassword789",
                role = "OperationAdmin"
            },
            new {
                admin_id = 4,
                nama = "Budi Pelabuhan",
                username = "admin_pelabuhan",
                email = "pelabuhan@tiketlaut.com",
                password = "hashedPassword101",
                role = "OperationAdmin"
            },
            new {
                admin_id = 5,
                nama = "Maya Finance",
                username = "admin_finance",
                email = "finance@tiketlaut.com",
                password = "hashedPassword112", 
                role = "OperationAdmin"
            }
        };

        private static int _nextId = 6;

        [HttpGet]
        public ActionResult<IEnumerable<object>> GetAllAdmin()
        {
            return Ok(AllAdminData);
        }

        [HttpGet("{id}")]
        public ActionResult<object> GetAdmin(int id)
        {
            var admin = AllAdminData.FirstOrDefault(a => ((dynamic)a).admin_id == id);
            if (admin == null) return NotFound($"Admin dengan ID {id} tidak ditemukan");
            return Ok(admin);
        }

        [HttpGet("username/{username}")]
        public ActionResult<object> GetAdminByUsername(string username)
        {
            var admin = AllAdminData.FirstOrDefault(a => 
                ((dynamic)a).username.ToString()
                .Equals(username, StringComparison.OrdinalIgnoreCase));
            if (admin == null) return NotFound($"Admin dengan username {username} tidak ditemukan");
            return Ok(admin);
        }

        [HttpGet("role/{role}")]
        public ActionResult<IEnumerable<object>> GetAdminByRole(string role)
        {
            var admins = AllAdminData.Where(a => 
                ((dynamic)a).role.ToString()
                .Contains(role, StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(admins);
        }

        /// <summary>
        /// Check role dan permissions admin
        /// </summary>
        /// </summary>
        [HttpGet("{id}/permissions")]
        public ActionResult<object> GetAdminPermissions(int id)
        {
            var admin = AllAdminData.FirstOrDefault(a => ((dynamic)a).admin_id == id);
            if (admin == null) return NotFound($"Admin dengan ID {id} tidak ditemukan");

            var adminData = (dynamic)admin;
            var permissions = GetPermissionsByRole(adminData.role.ToString());
            
            var result = new
            {
                admin_id = adminData.admin_id,
                nama = adminData.nama,
                username = adminData.username,
                role = adminData.role,
                permissions = permissions
            };

            return Ok(result);
        }

        /// <summary>
        /// Get permissions berdasarkan role
        /// </summary>
        private object GetPermissionsByRole(string role)
        {
            return role switch
            {
                "SuperAdmin" => new
                {
                    can_create_admin = true,
                    can_edit_admin = true,
                    can_delete_admin = true,
                    can_manage_schedules = true,
                    can_manage_ships = true,
                    can_manage_harbors = true,
                    can_manage_payments = true,
                    can_manage_tickets = true,
                    can_view_reports = true
                },
                "OperationAdmin" => new
                {
                    can_create_admin = false,
                    can_edit_admin = false,
                    can_delete_admin = false,
                    can_manage_schedules = true,
                    can_manage_ships = true,
                    can_manage_harbors = true,
                    can_manage_payments = true,
                    can_manage_tickets = true,
                    can_view_reports = true
                },
                _ => new
                {
                    can_create_admin = false,
                    can_edit_admin = false,
                    can_delete_admin = false,
                    can_manage_schedules = false,
                    can_manage_ships = false,
                    can_manage_harbors = false,
                    can_manage_payments = false,
                    can_manage_tickets = false,
                    can_view_reports = false
                }
            };
        }

        [HttpGet("dashboard-stats")]
        public ActionResult<object> GetDashboardStats()
        {
            var totalAdmin = AllAdminData.Count;
            var adminAktif = AllAdminData.Count(a => ((dynamic)a).status_aktif == true);
            var adminTidakAktif = totalAdmin - adminAktif;
            
            var loginToday = AllAdminData.Count(a => 
                ((DateTime)((dynamic)a).terakhir_login).Date == DateTime.Today);

            var roleDistribution = AllAdminData
                .GroupBy(a => ((dynamic)a).role)
                .Select(g => new { role = g.Key, jumlah = g.Count() })
                .ToList();

            var departmentDistribution = AllAdminData
                .GroupBy(a => ((dynamic)a).departemen)
                .Select(g => new { departemen = g.Key, jumlah = g.Count() })
                .ToList();

            var stats = new
            {
                total_admin = totalAdmin,
                admin_aktif = adminAktif,
                admin_tidak_aktif = adminTidakAktif,
                login_hari_ini = loginToday,
                distribusi_role = roleDistribution,
                distribusi_departemen = departmentDistribution
            };

            return Ok(stats);
        }

        /// <summary>
        /// Membuat admin baru - Hanya SuperAdmin yang boleh melakukan ini
        /// </summary>
        [HttpPost]
        public ActionResult<object> CreateAdmin([FromBody] CreateAdminRequest request)
        {
            if (request == null) return BadRequest("Data admin tidak valid");
            
            // TODO: Implement authentication check here
            // For now, we'll assume the request includes the requesting admin ID
            if (request.requesting_admin_id.HasValue)
            {
                var requestingAdmin = AllAdminData.FirstOrDefault(a => 
                    ((dynamic)a).admin_id == request.requesting_admin_id.Value);
                
                if (requestingAdmin == null)
                    return Unauthorized("Admin tidak ditemukan");
                
                if (((dynamic)requestingAdmin).role != "SuperAdmin")
                    return Forbid("Hanya SuperAdmin yang dapat menambahkan admin baru");
            }
            
            // Check if username already exists
            var existingAdmin = AllAdminData.FirstOrDefault(a => 
                ((dynamic)a).username.ToString()
                .Equals(request.username, StringComparison.OrdinalIgnoreCase));
            
            if (existingAdmin != null) 
                return Conflict($"Username {request.username} sudah digunakan");
            
            var newAdmin = new
            {
                admin_id = _nextId++,
                nama = request.nama,
                username = request.username,
                email = request.email,
                password = request.password, // TODO: Hash this password in production
                role = request.role ?? "OperationAdmin" // Default to OperationAdmin
            };

            AllAdminData.Add(newAdmin);
            return CreatedAtAction(nameof(GetAdmin), new { id = newAdmin.admin_id }, newAdmin);
        }

        [HttpPut("{id}/status")]
        public ActionResult UpdateStatusAdmin(int id, [FromBody] UpdateAdminStatusRequest request)
        {
            var admin = AllAdminData.FirstOrDefault(a => ((dynamic)a).admin_id == id);
            if (admin == null) return NotFound($"Admin dengan ID {id} tidak ditemukan");

            return Ok(new { 
                message = $"Status admin {((dynamic)admin).username} berhasil diupdate", 
                new_status = request.status_aktif,
                updated_at = DateTime.Now 
            });
        }

        [HttpPut("{id}/login")]
        public ActionResult UpdateLoginInfo(int id, [FromBody] LoginUpdateRequest request)
        {
            var admin = AllAdminData.FirstOrDefault(a => ((dynamic)a).admin_id == id);
            if (admin == null) return NotFound($"Admin dengan ID {id} tidak ditemukan");

            return Ok(new { 
                message = $"Login info admin {((dynamic)admin).username} berhasil diupdate", 
                login_time = DateTime.Now,
                ip_address = request.ip_address
            });
        }
    }

    public class CreateAdminRequest
    {
        public int? requesting_admin_id { get; set; } // ID admin yang melakukan request (untuk validasi role)
        public string nama { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string role { get; set; } = "OperationAdmin"; // SuperAdmin atau OperationAdmin
    }

    public class UpdateAdminStatusRequest
    {
        public bool status_aktif { get; set; }
    }

    public class LoginUpdateRequest
    {
        public string ip_address { get; set; } = string.Empty;
    }
}