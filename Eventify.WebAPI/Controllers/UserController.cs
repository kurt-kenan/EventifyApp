using Eventify.Core.Entities;
using Eventify.DataAccess.Data;
using Eventify.DTOs.User;
using Eventify.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Eventify.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly EmailSender _emailSender;
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public UserController(AppDbContext context, JwtService jwtService, EmailSender emailSender)
        {
            _context = context;
            _jwtService = jwtService;
            _emailSender = emailSender;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return BadRequest("Username is already taken.");

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email is already registered.");

            var user = new User
            {
                FullName = dto.FullName,
                Username = dto.Username,
                Email = dto.Email,
                BirthDate = dto.BirthDate,
                IsGoogleAccount = dto.IsGoogleAccount,
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                Tokens = 10,
                IsEmailVerified = false
            };

            if (!dto.IsGoogleAccount)
            {
                if (string.IsNullOrWhiteSpace(dto.Password))
                    return BadRequest("Password is required for normal registration.");

                user.PasswordHash = HashPassword(dto.Password);
            }

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Email do�rulama tokeni olu�tur
            var token = Guid.NewGuid().ToString();
            var verification = new EmailVerification
            {
                UserId = user.Id,
                Token = token,
                Expiration = DateTime.UtcNow.AddHours(24),
                IsUsed = false
            };
            await _context.EmailVerifications.AddAsync(verification);
            await _context.SaveChangesAsync();

            // Mail g�nderimi
            await _emailSender.SendEmailAsync(
                user.Email,
                "Email Do�rulama",
                $"https://localhost:7079/Account/VerifyEmail?token={Uri.EscapeDataString(token)}"
            );

            return Ok(new { message = "User registered successfully. Please verify your email." });
        }


        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var enteredHash = HashPassword(enteredPassword);
            return enteredHash == storedHash;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.UsernameOrEmail || u.Email == loginDto.UsernameOrEmail);

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                return Unauthorized("Kullan�c� ad�, email veya �ifre hatal�.");

            if (!user.IsEmailVerified)
                return Unauthorized("Email do�rulamas� yap�lmam��. L�tfen e-postan�z� do�rulay�n.");

            user.LastLoginAt = DateTime.UtcNow;  // <--- buras� eklenecek
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            var response = new LoginResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Role = user.Role
            };

            return Ok(response);
        }


        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound("Kullan�c� bulunamad�.");

            if (user.IsGoogleAccount)
                return BadRequest("Google ile giri� yapan kullan�c�lar �ifre de�i�tiremez.");

            if (!VerifyPassword(dto.OldPassword, user.PasswordHash))
                return Unauthorized("Eski �ifre yanl��.");

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("Yeni �ifre bo� olamaz.");

            user.PasswordHash = HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "�ifre ba�ar�yla de�i�tirildi." });
        }


        [HttpPost("admin-login")]
        [AllowAnonymous]
        public async Task<IActionResult> AdminLogin([FromBody] LoginRequestDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.UsernameOrEmail || u.Email == loginDto.UsernameOrEmail);

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                return Unauthorized("Kullan�c� ad�, email veya �ifre hatal�.");

            if (user.Role != "Admin")
                return Unauthorized("Bu giri� sadece adminler i�indir.");

            var token = _jwtService.GenerateToken(user);

            var response = new LoginResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Role = user.Role
            };

            return Ok(response);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.FullName,
                user.Username,
                user.Email,
                user.BirthDate,
                user.Role,
                user.Tokens,
                user.CreatedAt,
                user.PhotoPath
            });
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound();

            bool emailChanged = !string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email;
            user.FullName = dto.FullName ?? user.FullName;
            user.BirthDate = dto.BirthDate ?? user.BirthDate;
            if (emailChanged)
            {
                user.Email = dto.Email;
                user.IsEmailVerified = false;
                // Eski tokenleri iptal et
                var oldTokens = await _context.EmailVerifications.Where(v => v.UserId == user.Id && !v.IsUsed && v.Expiration > DateTime.UtcNow).ToListAsync();
                foreach (var oldToken in oldTokens)
                    oldToken.IsUsed = true;
                // Yeni token oluştur
                var token = Guid.NewGuid().ToString();
                var verification = new EmailVerification
                {
                    UserId = user.Id,
                    Token = token,
                    Expiration = DateTime.UtcNow.AddHours(24),
                    IsUsed = false
                };
                await _context.EmailVerifications.AddAsync(verification);
                await _context.SaveChangesAsync();
                // Yeni e-posta adresine doğrulama maili gönder
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Email Doğrulama (E-posta değişikliği)",
                    $"https://localhost:7079/Account/VerifyEmail?token={Uri.EscapeDataString(token)}"
                );
            }
            else
            {
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "Profil başarıyla güncellendi." });
        }

        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetRoles()
        {
            var roles = new[] { "Admin", "User" };
            return Ok(roles);
        }

        [HttpGet("tokens")]
        [Authorize]
        public async Task<IActionResult> GetTokenInfo()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return NotFound();

            return Ok(new { tokens = user.Tokens });
        }

        public class PurchaseTokenDto
        {
            public int Amount { get; set; }
        }

        [HttpPost("purchase-tokens")]
        [Authorize]
        public async Task<IActionResult> PurchaseTokens([FromBody] PurchaseTokenDto dto)
        {
            if (dto.Amount <= 0)
                return BadRequest("Ge�erli bir jeton miktar� giriniz.");

            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return NotFound();

            user.Tokens += dto.Amount;

            var transaction = new TokenTransaction
            {
                UserId = user.Id,
                Amount = dto.Amount,
                Reason = "Jeton sat�n al�nd�",
                CreatedAt = DateTime.UtcNow
            };

            await _context.TokenTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{dto.Amount} jeton ba�ar�yla sat�n al�nd�.", user.Tokens });
        }

        public class UseTokenDto
        {
            public int Amount { get; set; } = 1;
            public string Reason { get; set; }
        }

        [HttpPost("use-token")]
        [Authorize]
        public async Task<IActionResult> UseTokens([FromBody] UseTokenDto dto)
        {
            if (dto.Amount <= 0)
                return BadRequest("Harcanacak jeton miktar� 0'dan b�y�k olmal�d�r.");

            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
                return NotFound();

            if (user.Tokens < dto.Amount)
                return BadRequest("Yeterli jeton yok.");

            user.Tokens -= dto.Amount;

            var transaction = new TokenTransaction
            {
                UserId = user.Id,
                Amount = -dto.Amount,
                Reason = dto.Reason ?? "Jeton harcand�",
                CreatedAt = DateTime.UtcNow
            };

            await _context.TokenTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"{dto.Amount} jeton ba�ar�yla harcand�.",
                remainingTokens = user.Tokens,
                reason = transaction.Reason
            });
        }

        [HttpGet("tokens/history")]
        [Authorize]
        public async Task<IActionResult> GetTokenHistory()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound();

            var history = await _context.TokenTransactions
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Amount,
                    t.Reason,
                    t.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
        {
            var verification = await _context.EmailVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Token == dto.Token && !v.IsUsed && v.Expiration > DateTime.UtcNow);

            if (verification == null)
                return BadRequest("Ge�ersiz veya s�resi dolmu� token.");

            verification.IsUsed = true;
            verification.User.IsEmailVerified = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Email do�ruland�." });
        }

        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmailGet([FromQuery] string token)
        {
            var verification = await _context.EmailVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Token == token && !v.IsUsed && v.Expiration > DateTime.UtcNow);

            if (verification == null)
                return BadRequest("Ge�ersiz veya s�resi dolmu� token.");

            verification.IsUsed = true;
            verification.User.IsEmailVerified = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Email do�ruland�." });
        }


        [HttpPost("resend-verification-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendEmailDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email || u.Username == dto.Username);
            if (user == null)
                return NotFound();

            if (user.IsEmailVerified)
                return BadRequest("Email zaten do�rulanm��.");

            // Eski tokenleri iptal et...
            var oldTokens = await _context.EmailVerifications
                .Where(v => v.UserId == user.Id && !v.IsUsed && v.Expiration > DateTime.UtcNow)
                .ToListAsync();

            foreach (var oldToken in oldTokens)
            {
                oldToken.IsUsed = true;
            }

            var token = Guid.NewGuid().ToString();
            var verification = new EmailVerification
            {
                UserId = user.Id,
                Token = token,
                Expiration = DateTime.UtcNow.AddHours(24),
                IsUsed = false
            };

            await _context.EmailVerifications.AddAsync(verification);
            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(
                user.Email,
                "Email Do�rulama (Yeniden G�nderildi)",
                $"Do�rulama linkiniz: https://localhost:7079/account/verify-email?token={token}"
            );

            return Ok(new { message = "Do�rulama emaili yeniden g�nderildi." });
        }

        public class ResendEmailDto
        {
            public string Email { get; set; }
            public string Username { get; set; }
        }

        // 1. �ifre s�f�rlama linki g�nderme
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound("Kullan�c� bulunamad�.");

            // Var ise eski tokenlar� iptal et
            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed && t.Expiration > DateTime.UtcNow)
                .ToListAsync();

            foreach (var oldToken in oldTokens)
                oldToken.IsUsed = true;

            var token = Guid.NewGuid().ToString();
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                Expiration = DateTime.UtcNow.AddHours(1), // 1 saat ge�erli
                IsUsed = false
            };

            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(
                user.Email,
                "�ifre S�f�rlama",
                $"�ifre s�f�rlama linkiniz: https://localhost:7079/account/reset-password?token={token}"
            );

            return Ok(new { message = "�ifre s�f�rlama maili g�nderildi." });
        }

        public class ForgotPasswordDto
        {
            public string Email { get; set; }
        }


        // 2. Yeni �ifre belirleme
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == dto.Token && !t.IsUsed && t.Expiration > DateTime.UtcNow);

            if (resetToken == null)
                return BadRequest("Ge�ersiz veya s�resi dolmu� token.");

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("Yeni �ifre bo� olamaz.");

            resetToken.IsUsed = true;
            resetToken.User.PasswordHash = HashPassword(dto.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(new { message = "�ifre ba�ar�yla s�f�rland�." });
        }

        public class ResetPasswordDto
        {
            public string Token { get; set; }
            public string NewPassword { get; set; }
        }

        [HttpPost("resend-password-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendPasswordReset([FromBody] ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return NotFound("Kullan�c� bulunamad�.");

            // Eski aktif tokenleri iptal et
            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id && !t.IsUsed && t.Expiration > DateTime.UtcNow)
                .ToListAsync();

            foreach (var oldToken in oldTokens)
                oldToken.IsUsed = true;

            var token = Guid.NewGuid().ToString();
            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                Expiration = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };

            await _context.PasswordResetTokens.AddAsync(resetToken);
            await _context.SaveChangesAsync();

            await _emailSender.SendEmailAsync(
                user.Email,
                "�ifre S�f�rlama (Yeniden G�nderildi)",
                $"�ifre s�f�rlama linkiniz: https://localhost:7079/account/reset-password?token={token}"
            );

            return Ok(new { message = "�ifre s�f�rlama maili yeniden g�nderildi." });
        }

        [HttpPost("upload-photo")]
        [Authorize]
        public async Task<IActionResult> UploadProfilePhoto([FromForm] IFormFile file)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound();
            if (file == null || file.Length == 0)
                return BadRequest("Dosya seçilmedi.");
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/profilephotos");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploads, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            user.PhotoPath = $"https://localhost:44315/profilephotos/{fileName}";
            await _context.SaveChangesAsync();
            return Ok(new { path = user.PhotoPath });
        }

        [HttpGet("profile-photo")]
        [Authorize]
        public async Task<IActionResult> GetProfilePhoto()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound();
            return Ok(new { path = user.PhotoPath });
        }

        [HttpPost("add-favorite")]
        [Authorize]
        public async Task<IActionResult> AddFavorite([FromBody] FavoriteEventDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.Include(u => u.FavoriteEvents).FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            var eventExists = await _context.Events.AnyAsync(e => e.Id == dto.EventId);
            if (!eventExists)
                return NotFound("Etkinlik bulunamadı.");

            var alreadyFavorite = await _context.FavoriteEvents.AnyAsync(f => f.UserId == user.Id && f.EventId == dto.EventId);
            if (alreadyFavorite)
                return BadRequest("Bu etkinlik zaten favorilerde.");

            var favorite = new FavoriteEvent { UserId = user.Id, EventId = dto.EventId };
            _context.FavoriteEvents.Add(favorite);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Etkinlik favorilere eklendi." });
        }

        [HttpPost("remove-favorite")]
        [Authorize]
        public async Task<IActionResult> RemoveFavorite([FromBody] FavoriteEventDto dto)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            var favorite = await _context.FavoriteEvents.FirstOrDefaultAsync(f => f.UserId == user.Id && f.EventId == dto.EventId);
            if (favorite == null)
                return NotFound("Favori bulunamadı.");

            _context.FavoriteEvents.Remove(favorite);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Etkinlik favorilerden çıkarıldı." });
        }

        [HttpGet("favorites")]
        [Authorize]
        public async Task<IActionResult> GetFavorites()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();
            var user = await _context.Users.Include(u => u.FavoriteEvents).FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return NotFound();
            var favoriteEventIds = user.FavoriteEvents.Select(f => f.EventId).ToList();
            return Ok(favoriteEventIds);
        }

    }
}
