using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Eventify.DTOs.Subscription;
using Eventify.Mvc.Attributes;

namespace Eventify.Mvc.Controllers
{
    [UserAuthorization]
    public class SubscriptionController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SubscriptionController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Plans()
        {
            var isAdmin = HttpContext.Session.GetString("isAdmin") == "true";
            if (isAdmin)
            {
                TempData["ToastError"] = "Admin kullanıcılar abonelik satın alamaz. Zaten tüm özelliklere erişiminiz var!";
                return RedirectToAction("Index", "Home");
            }

            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            var response = await httpClient.GetAsync("api/subscription/plans");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Abonelik planları alınamadı.";
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();
            var plans = JsonSerializer.Deserialize<List<SubscriptionPlanDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(plans);
        }

        public async Task<IActionResult> CheckSubscriptionBeforePurchase(int planId)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            // Kullanıcının mevcut aboneliğini kontrol et
            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            var request = new HttpRequestMessage(HttpMethod.Get, "api/subscription/my-subscription");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);
                var hasSubscription = result.GetProperty("hasSubscription").GetBoolean();
                
                if (hasSubscription)
                {
                    // Zaten aboneliği varsa "Aboneliğim" sayfasına yönlendir
                    TempData["Info"] = "Zaten aktif bir aboneliğiniz bulunmaktadır.";
                    return RedirectToAction("MySubscription");
                }
            }

            // Aboneliği yoksa satın alma sayfasına yönlendir
            return RedirectToAction("Purchase", new { planId });
        }

        public async Task<IActionResult> Purchase(int planId)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            var response = await httpClient.GetAsync($"api/subscription/plans");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Plan bilgileri alınamadı.";
                return RedirectToAction("Plans");
            }

            var json = await response.Content.ReadAsStringAsync();
            var plans = JsonSerializer.Deserialize<List<SubscriptionPlanDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var plan = plans?.FirstOrDefault(p => p.Id == planId);

            if (plan == null)
            {
                TempData["Error"] = "Plan bulunamadı.";
                return RedirectToAction("Plans");
            }

            return View(plan);
        }

        [HttpPost]
        public async Task<IActionResult> Purchase(int planId, [FromForm] PurchaseSubscriptionDto dto)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            dto.PlanId = planId;

            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            var request = new HttpRequestMessage(HttpMethod.Post, "api/subscription/purchase");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Abonelik satın alınamadı.";
                return RedirectToAction("Purchase", new { planId });
            }

            TempData["Success"] = "Abonelik başarıyla satın alındı!";
            return RedirectToAction("MySubscription");
        }

        public async Task<IActionResult> MySubscription()
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            var request = new HttpRequestMessage(HttpMethod.Get, "api/subscription/my-subscription");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Abonelik bilgileri alınamadı.";
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);
            
            ViewBag.HasSubscription = result.GetProperty("hasSubscription").GetBoolean();
            if (ViewBag.HasSubscription)
            {
                ViewBag.Subscription = JsonSerializer.Deserialize<UserSubscriptionDto>(result.GetProperty("subscription").GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                // Kullanım istatistiklerini al
                var statsRequest = new HttpRequestMessage(HttpMethod.Get, "api/subscription/usage-stats");
                statsRequest.Headers.Add("Authorization", $"Bearer {token}");
                var statsResponse = await httpClient.SendAsync(statsRequest);
                
                if (statsResponse.IsSuccessStatusCode)
                {
                    var statsJson = await statsResponse.Content.ReadAsStringAsync();
                    ViewBag.UsageStats = JsonSerializer.Deserialize<JsonElement>(statsJson);
                }
                else
                {
                    // Varsayılan değerler
                    ViewBag.UsageStats = new
                    {
                        eventsCreatedThisMonth = 0,
                        totalParticipants = 0,
                        favoriteEvents = 0,
                        eventsJoinedThisMonth = 0
                    };
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Cancel()
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            var request = new HttpRequestMessage(HttpMethod.Post, "api/subscription/cancel");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Abonelik iptal edilemedi.";
                return RedirectToAction("MySubscription");
            }

            TempData["Success"] = "Abonelik iptal edildi.";
            return RedirectToAction("MySubscription");
        }
    }
} 