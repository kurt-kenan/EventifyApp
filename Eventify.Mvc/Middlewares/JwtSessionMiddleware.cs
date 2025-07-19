using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Eventify.Mvc.Middlewares
{
    public class JwtSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtSessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    var claims = jwtToken.Claims;

                    var identity = new ClaimsIdentity(claims, "jwt");
                    context.User = new ClaimsPrincipal(identity);
                }
                catch
                {
                    // Geçersiz token varsa boş bırak
                }
            }

            await _next(context);
        }
    }

}
