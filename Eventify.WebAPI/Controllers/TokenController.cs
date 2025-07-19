using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Eventify.DataAccess.Data;
using Eventify.Core.Entities;
using Eventify.DTOs.Token;

namespace Eventify.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TokenController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("packages")]
        public async Task<IActionResult> GetPackages()
        {
            var packages = await _context.TokenPackages
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => new TokenPackageDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    TokenAmount = p.TokenAmount,
                    Price = p.Price,
                    IsActive = p.IsActive,
                    SortOrder = p.SortOrder,
                    IsPopular = p.IsPopular,
                    BonusText = p.BonusText
                })
                .ToListAsync();

            return Ok(packages);
        }

        [HttpPost("purchase")]
        [Authorize]
        public async Task<IActionResult> PurchaseTokens([FromBody] PurchaseTokenDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var package = await _context.TokenPackages.FirstOrDefaultAsync(p => p.Id == dto.PackageId && p.IsActive);
            if (package == null) return NotFound("Paket bulunamadı.");

            // Test modu - doğrulama yapmadan direkt satın al
            // Jetonları kullanıcıya ekle
            user.Tokens += package.TokenAmount;

            // İşlem kaydı oluştur
            var transaction = new TokenTransaction
            {
                UserId = user.Id,
                Amount = package.TokenAmount,
                Reason = $"Jeton satın alındı - {package.Name} (Test Modu)",
                CreatedAt = DateTime.Now,
                TransactionId = Guid.NewGuid().ToString(),
                PaymentMethod = dto.PaymentMethod ?? "test_payment",
                Price = package.Price
            };

            _context.TokenTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Jetonlar başarıyla satın alındı! (Test Modu)", 
                transactionId = transaction.TransactionId,
                amount = package.TokenAmount,
                newBalance = user.Tokens
            });
        }

        [HttpGet("balance")]
        [Authorize]
        public async Task<IActionResult> GetBalance()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            return Ok(new { balance = user.Tokens });
        }

        [HttpGet("transactions")]
        [Authorize]
        public async Task<IActionResult> GetTransactions()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            var transactions = await _context.TokenTransactions
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .Select(t => new
                {
                    t.Id,
                    t.Amount,
                    t.Reason,
                    t.CreatedAt,
                    t.Price
                })
                .ToListAsync();

            return Ok(transactions);
        }
    }
} 