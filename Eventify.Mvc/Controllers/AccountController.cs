using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace Eventify.Mvc.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _httpClient;
        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Eventify.WebAPI");
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.Message = "Geçersiz doğrulama bağlantısı.";
                ViewBag.Success = false;
                return View();
            }

            var response = await _httpClient.GetAsync($"api/user/verify-email?token={token}");
            if (response.IsSuccessStatusCode)
            {
                ViewBag.Message = "E-posta başarıyla doğrulandı.";
                ViewBag.Success = true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                ViewBag.Message = !string.IsNullOrWhiteSpace(error) ? error : "E-posta doğrulama başarısız veya bağlantı geçersiz.";
                ViewBag.Success = false;
            }
            return View();
        }
    }
} 