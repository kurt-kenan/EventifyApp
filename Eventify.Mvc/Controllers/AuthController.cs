using Eventify.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Eventify.Mvc.Controllers
{
    public class AuthController : Controller
    {
        private readonly HttpClient _httpClient;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Eventify.WebAPI");
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("token")))
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var response = await _httpClient.PostAsJsonAsync("api/user/login", model);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var token = root.GetProperty("token").GetString();
                var fullName = root.TryGetProperty("fullName", out var fn) ? fn.GetString() : null;
                var username = root.TryGetProperty("username", out var un) ? un.GetString() : null;
                var role = root.TryGetProperty("role", out var r) ? r.GetString() : null;
                
                // Admin kullanıcılar normal giriş yapamaz
                if (role == "Admin")
                {
                    ModelState.AddModelError("", "Admin kullanıcılar normal giriş yapamaz. Lütfen admin panelinden giriş yapın.");
                    return View(model);
                }
                
                HttpContext.Session.SetString("token", token);
                if (!string.IsNullOrEmpty(fullName))
                    HttpContext.Session.SetString("Username", fullName);
                else if (!string.IsNullOrEmpty(username))
                    HttpContext.Session.SetString("Username", username);
                return RedirectToAction("Index", "Home");
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", !string.IsNullOrWhiteSpace(error) ? error : "Giriş başarısız.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("token")))
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var response = await _httpClient.PostAsJsonAsync("api/user/register", model);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Login");
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", !string.IsNullOrWhiteSpace(error) ? error : "Kayıt başarısız.");
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }

}
