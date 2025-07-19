using Eventify.Core.Entities;
using Eventify.DataAccess.Data;
using Eventify.DTOs.Event;
using Eventify.DTOs.Hobby;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eventify.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/events
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .Include(e => e.Creator)
                .Include(e => e.EventHobbies)
                    .ThenInclude(eh => eh.Hobby)
                .Include(e => e.Participants)
                .Select(e => new EventDetailDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    Location = e.Location,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    MaxParticipants = e.MaxParticipants,
                    CreatorUsername = e.Creator.Username,
                    PhotoPath = e.PhotoPath,
                    ParticipantCount = e.Participants.Count,
                    Hobbies = e.EventHobbies.Select(eh => new DTOs.Hobby.HobbyDto { Id = eh.Hobby.Id, Name = eh.Hobby.Name }).ToList()
                })
                .ToListAsync();

            return Ok(events);
        }

        // POST: /api/events
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto dto)
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Unauthorized();

            if (dto.EndDate <= dto.StartDate)
                return BadRequest("Bitiş tarihi başlangıç tarihinden önce olamaz.");

            // Abonelik kontrolü
            var subscription = await _context.UserSubscriptions
                .Include(us => us.Plan)
                .Where(us => us.UserId == user.Id && us.EndDate > DateTime.Now)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefaultAsync();

            if (subscription == null || !subscription.Plan.CanCreateEvents)
                return BadRequest("Etkinlik oluşturmak için aktif bir aboneliğiniz olmalıdır.");

            // Bu ay oluşturulan etkinlik sayısını kontrol et
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var eventsCreatedThisMonth = await _context.Events
                .Where(e => e.CreatorId == user.Id && 
                           e.CreatedAt.Month == currentMonth && 
                           e.CreatedAt.Year == currentYear)
                .CountAsync();

            if (eventsCreatedThisMonth >= subscription.Plan.MaxEventsPerMonth)
                return BadRequest($"Bu ay maksimum {subscription.Plan.MaxEventsPerMonth} etkinlik oluşturabilirsiniz. Limit dolmuştur.");

            // Maksimum katılımcı sayısını kontrol et
            if (dto.MaxParticipants > subscription.Plan.MaxParticipantsPerEvent)
                return BadRequest($"Etkinlik başına maksimum {subscription.Plan.MaxParticipantsPerEvent} katılımcı alabilirsiniz.");

            var newEvent = new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                MaxParticipants = dto.MaxParticipants,
                CreatorId = user.Id,
                CreatedAt = DateTime.UtcNow,
                PhotoPath = dto.PhotoPath
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            // Hobileri ekle
            if (dto.HobbyIds == null || !dto.HobbyIds.Any())
            {
                return BadRequest("En az bir hobi seçmelisiniz.");
            }
            // Hobi id'leri gerçekten var mı kontrol et
            var validHobbyIds = await _context.Set<Hobby>().Where(h => dto.HobbyIds.Contains(h.Id)).Select(h => h.Id).ToListAsync();
            if (validHobbyIds.Count != dto.HobbyIds.Count)
            {
                return BadRequest("Geçersiz hobi seçimi yapıldı.");
            }
            foreach (var hobbyId in dto.HobbyIds)
            {
                _context.EventHobbies.Add(new EventHobby { EventId = newEvent.Id, HobbyId = hobbyId });
            }
            await _context.SaveChangesAsync();
            // Eklenen hobiler gerçekten kaydedildi mi kontrol et
            var addedHobbies = await _context.EventHobbies.Where(eh => eh.EventId == newEvent.Id).ToListAsync();
            if (addedHobbies.Count == 0)
            {
                return StatusCode(500, "Hobiler kaydedilemedi. Lütfen tekrar deneyin.");
            }

            return CreatedAtAction(nameof(GetEventById), new { id = newEvent.Id }, new { newEvent.Id });
        }

        [HttpPost("upload-photo")]
        [Authorize]
        public async Task<IActionResult> UploadEventPhoto([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Dosya seçilmedi.");

            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/eventphotos");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photoPath = $"https://localhost:44315/eventphotos/{fileName}";
            return Ok(new { path = photoPath });
        }

        // GET: /api/events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Creator)
                .Include(e => e.EventHobbies)
                    .ThenInclude(eh => eh.Hobby)
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
                return NotFound();

            var dto = new EventDetailDto
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                Location = ev.Location,
                StartDate = ev.StartDate,
                EndDate = ev.EndDate,
                MaxParticipants = ev.MaxParticipants,
                CreatorUsername = ev.Creator.Username,
                PhotoPath = ev.PhotoPath,
                ParticipantCount = ev.Participants.Count,
                Hobbies = ev.EventHobbies.Select(eh => new DTOs.Hobby.HobbyDto { Id = eh.Hobby.Id, Name = eh.Hobby.Name }).ToList()
            };

            return Ok(dto);
        }

        // GET: /api/events/mine
        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMyEvents()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Unauthorized();

            var myEvents = await _context.Events
                .Where(e => e.CreatorId == user.Id)
                .Include(e => e.Participants)
                .Select(e => new EventDetailDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    Location = e.Location,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    MaxParticipants = e.MaxParticipants,
                    CreatorUsername = username,
                    PhotoPath = e.PhotoPath,
                    ParticipantCount = e.Participants.Count
                })
                .ToListAsync();

            return Ok(myEvents);
        }

        // GET: /api/events/joined
        [HttpGet("joined")]
        [Authorize]
        public async Task<IActionResult> GetJoinedEvents()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Unauthorized();
            var joinedEvents = await _context.EventParticipants
                .Where(p => p.UserId == user.Id)
                .Include(p => p.Event)
                    .ThenInclude(e => e.Participants)
                .Select(p => new EventDetailDto
                {
                    Id = p.Event.Id,
                    Title = p.Event.Title,
                    Description = p.Event.Description,
                    Location = p.Event.Location,
                    StartDate = p.Event.StartDate,
                    EndDate = p.Event.EndDate,
                    MaxParticipants = p.Event.MaxParticipants,
                    CreatorUsername = p.Event.Creator.Username,
                    PhotoPath = p.Event.PhotoPath,
                    ParticipantCount = p.Event.Participants.Count
                })
                .ToListAsync();
            return Ok(joinedEvents);
        }

        // POST: /api/events/{id}/join
        [HttpPost("{id}/join")]
        [Authorize]
        public async Task<IActionResult> JoinEvent(int id)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Unauthorized();

            var ev = await _context.Events
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
                return NotFound("Etkinlik bulunamad�.");

            if (ev.MaxParticipants > 0 && ev.Participants.Count >= ev.MaxParticipants)
                return BadRequest("Bu etkinlik i�in kat�l�mc� limiti dolmu�tur.");

            var alreadyJoined = ev.Participants.Any(p => p.UserId == user.Id);
            if (alreadyJoined)
                return BadRequest("Zaten bu etkinli�e kat�lm��s�n�z.");

            if (user.Tokens < 1)
                return BadRequest("Yeterli jetonunuz yok.");

            user.Tokens -= 1;

            var participant = new EventParticipant
            {
                EventId = id,
                UserId = user.Id,
                JoinedAt = DateTime.UtcNow
            };
            _context.EventParticipants.Add(participant);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Etkinli�e ba�ar�yla kat�ld�n�z.", remainingTokens = user.Tokens });
        }

        // PUT: /api/events/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventCreateDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Unauthorized();

            var ev = await _context.Events.Include(e => e.EventHobbies).FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null)
                return NotFound("Etkinlik bulunamadı.");

            // Kullanıcının kendi etkinliği mi veya admin mi kontrol et
            if (ev.CreatorId != user.Id && user.Role != "Admin")
                return Forbid("Bu etkinliği düzenleme yetkiniz yok.");

            // Etkinlik bilgilerini güncelle
            ev.Title = dto.Title;
            ev.Description = dto.Description;
            ev.Location = dto.Location;
            ev.StartDate = dto.StartDate;
            ev.EndDate = dto.EndDate;
            ev.MaxParticipants = dto.MaxParticipants;
            ev.PhotoPath = dto.PhotoPath;

            // Mevcut hobileri kaldır
            _context.EventHobbies.RemoveRange(ev.EventHobbies);

            // Yeni hobileri ekle
            if (dto.HobbyIds == null || !dto.HobbyIds.Any())
            {
                return BadRequest("En az bir hobi seçmelisiniz.");
            }

            // Hobi id'leri gerçekten var mı kontrol et
            var validHobbyIds = await _context.Set<Hobby>().Where(h => dto.HobbyIds.Contains(h.Id)).Select(h => h.Id).ToListAsync();
            if (validHobbyIds.Count != dto.HobbyIds.Count)
            {
                return BadRequest("Geçersiz hobi seçimi yapıldı.");
            }

            foreach (var hobbyId in dto.HobbyIds)
            {
                _context.EventHobbies.Add(new EventHobby { EventId = ev.Id, HobbyId = hobbyId });
            }

            await _context.SaveChangesAsync();

            // Eklenen hobiler gerçekten kaydedildi mi kontrol et
            var addedHobbies = await _context.EventHobbies.Where(eh => eh.EventId == ev.Id).ToListAsync();
            if (addedHobbies.Count == 0)
            {
                return StatusCode(500, "Hobiler kaydedilemedi. Lütfen tekrar deneyin.");
            }

            return Ok(new { message = "Etkinlik başarıyla güncellendi." });
        }

        // DELETE: /api/events/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
                return NotFound("Etkinlik bulunamadı.");

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Etkinlik başarıyla silindi." });
        }
    }
}
