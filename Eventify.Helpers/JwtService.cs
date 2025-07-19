using Eventify.Core.Entities;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Eventify.Helpers
{
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly int _expiryMinutes;

        public JwtService(string secretKey, int expiryMinutes = 60)
        {
            _secretKey = secretKey;
            _expiryMinutes = expiryMinutes;
        }

        public string GenerateToken(User user)
        {
            var claims = new[]
            {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim("fullname", user.FullName ?? ""),
        new Claim("role", user.Role ?? ""),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
