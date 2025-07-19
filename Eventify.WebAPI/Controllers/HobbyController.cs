using Eventify.Core.Entities;
using Eventify.DataAccess.Data;
using Eventify.DTOs.Hobby;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eventify.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HobbyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HobbyController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateHobby([FromBody] HobbyCreateDto dto)
        {
            var hobby = new Hobby { Name = dto.Name };
            await _context.AddAsync(hobby);
            await _context.SaveChangesAsync();
            return Ok(hobby);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var hobbies = await _context.Set<Hobby>()
                .Select(h => new { h.Id, h.Name })
                .ToListAsync();
            return Ok(hobbies);
        }

        [HttpPost("add-to-user")]
        [Authorize]
        public async Task<IActionResult> AddHobbyToUser([FromBody] UserHobbyDto dto)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return NotFound("Kullan�c� bulunamad�.");

            var hobbyExists = await _context.Set<Hobby>().AnyAsync(h => h.Id == dto.HobbyId);
            if (!hobbyExists) return BadRequest("Hobi bulunamad�.");

            var alreadyAdded = await _context.Set<UserHobbies>()
                .AnyAsync(uh => uh.UserId == user.Id && uh.HobbyId == dto.HobbyId);
            if (alreadyAdded) return BadRequest("Bu hobi zaten eklenmi�.");

            var userHobby = new UserHobbies
            {
                UserId = user.Id,
                HobbyId = dto.HobbyId
            };

            await _context.AddAsync(userHobby);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Hobi ba�ar�yla eklendi." });
        }

        [HttpGet("my-hobbies")]
        [Authorize]
        public async Task<IActionResult> GetMyHobbies()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users
                .Include(u => u.UserHobbies)
                .ThenInclude(uh => uh.Hobby)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return NotFound();

            var hobbies = user.UserHobbies.Select(uh => new
            {
                uh.Hobby.Id,
                uh.Hobby.Name
            });

            return Ok(hobbies);
        }

        [HttpPost("remove-from-user")]
        [Authorize]
        public async Task<IActionResult> RemoveHobbyFromUser([FromBody] UserHobbyDto dto)
        {
            var username = User.Identity?.Name;
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var userHobby = await _context.Set<UserHobbies>()
                .FirstOrDefaultAsync(uh => uh.UserId == user.Id && uh.HobbyId == dto.HobbyId);

            if (userHobby == null) return BadRequest("Bu hobi kullanıcıda bulunamadı.");

            _context.Remove(userHobby);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Hobi başarıyla kaldırıldı." });
        }
    }
}
