using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Eventify.DTOs.Token;
using Eventify.Mvc.Attributes;

namespace Eventify.Mvc.Controllers
{
    [UserAuthorization]
    public class TokenController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TokenController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Packages()
        {
            var isAdmin = HttpContext.Session.GetString("isAdmin") == "true";
            if (isAdmin)
            {
                TempData["ToastError"] = "Admin kullanıcılar jeton satın alamaz. Zaten sınırsız erişiminiz var!";
                return RedirectToAction("Index", "Home");
            }

            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            var response = await httpClient.GetAsync("api/token/packages");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Jeton paketleri alınamadı.";
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();
            var packages = JsonSerializer.Deserialize<List<TokenPackageDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(packages);
        }

        public async Task<IActionResult> Purchase(int packageId)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            var response = await httpClient.GetAsync($"api/token/packages");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Paket bilgileri alınamadı.";
                return RedirectToAction("Packages");
            }

            var json = await response.Content.ReadAsStringAsync();
            var packages = JsonSerializer.Deserialize<List<TokenPackageDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var package = packages?.FirstOrDefault(p => p.Id == packageId);

            if (package == null)
            {
                TempData["Error"] = "Paket bulunamadı.";
                return RedirectToAction("Packages");
            }

            return View(package);
        }

        [HttpPost]
        public async Task<IActionResult> Purchase(int packageId, [FromForm] PurchaseTokenDto dto)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            dto.PackageId = packageId;

            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            var request = new HttpRequestMessage(HttpMethod.Post, "api/token/purchase");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Jeton satın alınamadı.";
                return RedirectToAction("Purchase", new { packageId });
            }

            TempData["Success"] = "Jetonlar başarıyla satın alındı!";
            return RedirectToAction("Balance");
        }

        public async Task<IActionResult> Balance()
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            var request = new HttpRequestMessage(HttpMethod.Get, "api/token/balance");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Jeton bakiyesi alınamadı.";
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);
            ViewBag.Balance = result.GetProperty("balance").GetInt32();

            // İşlem geçmişini de al
            var transactionsRequest = new HttpRequestMessage(HttpMethod.Get, "api/token/transactions");
            transactionsRequest.Headers.Add("Authorization", $"Bearer {token}");
            var transactionsResponse = await httpClient.SendAsync(transactionsRequest);
            
            if (transactionsResponse.IsSuccessStatusCode)
            {
                var transactionsJson = await transactionsResponse.Content.ReadAsStringAsync();
                ViewBag.Transactions = JsonSerializer.Deserialize<List<object>>(transactionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            return View();
        }
    }
} 