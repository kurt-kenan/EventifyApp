using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Eventify.DataAccess.Data;
using Eventify.Core.Entities;
using Eventify.DTOs.Subscription;

namespace Eventify.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SubscriptionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => new SubscriptionPlanDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    DurationInDays = p.DurationInDays,
                    MaxDailyJoins = p.MaxDailyJoins,
                    MaxEventsPerMonth = p.MaxEventsPerMonth,
                    MaxParticipantsPerEvent = p.MaxParticipantsPerEvent,
                    CanCreateEvents = p.CanCreateEvents,
                    HasPrioritySupport = p.HasPrioritySupport,
                    HasAdvancedAnalytics = p.HasAdvancedAnalytics,
                    IsActive = p.IsActive,
                    SortOrder = p.SortOrder
                })
                .ToListAsync();

            return Ok(plans);
        }

        [HttpGet("my-subscription")]
        [Authorize]
        public async Task<IActionResult> GetMySubscription()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var subscription = await _context.UserSubscriptions
                .Include(us => us.Plan)
                .Where(us => us.UserId == user.Id && us.EndDate > DateTime.Now)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefaultAsync();

            if (subscription == null)
                return Ok(new { hasSubscription = false });

            var daysRemaining = (subscription.EndDate - DateTime.Now).Days;

            var dto = new UserSubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                PlanId = subscription.PlanId,
                PlanName = subscription.Plan?.Name ?? "",
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                IsActive = subscription.EndDate > DateTime.Now,
                DaysRemaining = daysRemaining > 0 ? daysRemaining : 0,
                Plan = new SubscriptionPlanDto
                {
                    Id = subscription.Plan?.Id ?? 0,
                    Name = subscription.Plan?.Name ?? "",
                    Description = subscription.Plan?.Description ?? "",
                    Price = subscription.Plan?.Price ?? 0,
                    DurationInDays = subscription.Plan?.DurationInDays ?? 0,
                    MaxDailyJoins = subscription.Plan?.MaxDailyJoins ?? 0,
                    MaxEventsPerMonth = subscription.Plan?.MaxEventsPerMonth ?? 0,
                    MaxParticipantsPerEvent = subscription.Plan?.MaxParticipantsPerEvent ?? 0,
                    CanCreateEvents = subscription.Plan?.CanCreateEvents ?? false,
                    HasPrioritySupport = subscription.Plan?.HasPrioritySupport ?? false,
                    HasAdvancedAnalytics = subscription.Plan?.HasAdvancedAnalytics ?? false,
                    IsActive = subscription.Plan?.IsActive ?? false,
                    SortOrder = subscription.Plan?.SortOrder ?? 0
                }
            };

            return Ok(new { hasSubscription = true, subscription = dto });
        }

        [HttpPost("purchase")]
        [Authorize]
        public async Task<IActionResult> PurchaseSubscription([FromBody] PurchaseSubscriptionDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.IsActive);
            if (plan == null) return NotFound("Plan bulunamadı.");

            // Burada gerçek ödeme işlemi yapılacak
            // Şimdilik başarılı kabul ediyoruz
            var startDate = DateTime.Now;
            var endDate = startDate.AddDays(plan.DurationInDays);

            var subscription = new UserSubscription
            {
                UserId = user.Id,
                PlanId = plan.Id,
                StartDate = startDate,
                EndDate = endDate
            };

            _context.UserSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Abonelik başarıyla satın alındı.", subscriptionId = subscription.Id });
        }

        [HttpPost("cancel")]
        [Authorize]
        public async Task<IActionResult> CancelSubscription()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var subscription = await _context.UserSubscriptions
                .Where(us => us.UserId == user.Id && us.EndDate > DateTime.Now)
                .FirstOrDefaultAsync();

            if (subscription == null)
                return NotFound("Aktif abonelik bulunamadı.");

            // Aboneliği iptal et (bitiş tarihini bugüne ayarla)
            subscription.EndDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Abonelik iptal edildi." });
        }

        [HttpGet("usage-stats")]
        [Authorize]
        public async Task<IActionResult> GetUsageStats()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // Kullanıcının aktif aboneliğini al
            var subscription = await _context.UserSubscriptions
                .Include(us => us.Plan)
                .Where(us => us.UserId == user.Id && us.EndDate > DateTime.Now)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefaultAsync();

            // Bu ay oluşturulan etkinlik sayısı
            var eventsCreatedThisMonth = await _context.Events
                .Where(e => e.CreatorId == user.Id && 
                           e.CreatedAt.Month == currentMonth && 
                           e.CreatedAt.Year == currentYear)
                .CountAsync();

            // Kalan etkinlik oluşturma hakkı
            var maxEventsPerMonth = subscription?.Plan?.MaxEventsPerMonth ?? 0;
            var remainingEvents = Math.Max(0, maxEventsPerMonth - eventsCreatedThisMonth);

            // Toplam katılımcı sayısı (kullanıcının oluşturduğu etkinliklere katılan kişiler)
            var totalParticipants = await _context.EventParticipants
                .Where(ep => ep.Event.CreatorId == user.Id)
                .CountAsync();

            // Favori etkinlik sayısı
            var favoriteEvents = await _context.FavoriteEvents
                .Where(uf => uf.UserId == user.Id)
                .CountAsync();

            // Bu ay katıldığı etkinlik sayısı
            var eventsJoinedThisMonth = await _context.EventParticipants
                .Where(ep => ep.UserId == user.Id && 
                           ep.JoinedAt.Month == currentMonth && 
                           ep.JoinedAt.Year == currentYear)
                .CountAsync();

            // Kalan günlük katılım hakkı
            var maxDailyJoins = subscription?.Plan?.MaxDailyJoins ?? 0;
            var todayJoins = await _context.EventParticipants
                .Where(ep => ep.UserId == user.Id && 
                           ep.JoinedAt.Date == DateTime.Now.Date)
                .CountAsync();
            var remainingDailyJoins = Math.Max(0, maxDailyJoins - todayJoins);

            return Ok(new
            {
                eventsCreatedThisMonth,
                maxEventsPerMonth,
                remainingEvents,
                totalParticipants,
                maxParticipantsPerEvent = subscription?.Plan?.MaxParticipantsPerEvent ?? 0,
                favoriteEvents,
                eventsJoinedThisMonth,
                maxDailyJoins,
                remainingDailyJoins,
                canCreateEvents = subscription?.Plan?.CanCreateEvents ?? false
            });
        }
    }
} 