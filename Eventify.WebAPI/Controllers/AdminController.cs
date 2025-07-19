using Eventify.DataAccess.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eventify.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]  // T�m controller admin yetkisi ister
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // Kullanıcının oluşturduğu etkinlik var mı kontrol et
            bool hasEvents = await _context.Events.AnyAsync(e => e.CreatorId == id);
            if (hasEvents)
                return BadRequest("Bu kullanıcıya ait etkinlikler var. Önce etkinlikleri silmelisiniz.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kullanıcı başarıyla silindi." });
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] string newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Kullan�c� bulunamad�.");

            var allowedRoles = new[] { "Admin", "User", "Organizer" };  // Organizer eklendi
            if (!allowedRoles.Contains(newRole))
                return BadRequest("Ge�ersiz rol.");

            user.Role = newRole;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Kullan�c� rol� ba�ar�yla g�ncellendi." });
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUserUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.Role = dto.Role;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Kullanıcı başarıyla güncellendi." });
        }

        public class AdminUserUpdateDto
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
        }
    }
}
