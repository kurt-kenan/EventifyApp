using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Eventify.Mvc.Models;
using System.Linq;
using System.IO;
using System;
using System.ComponentModel.DataAnnotations;
using Eventify.Mvc.Attributes;

namespace Eventify.Mvc.Controllers
{
    [UserAuthorization]
    public class EventController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        
        public EventController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Eventify.WebAPI");
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            // Kullanıcının etkinlik oluşturma hakkını kontrol et
            var httpClient = _httpClientFactory.CreateClient("Eventify.WebAPI");
            
            // Önce kullanıcının rolünü kontrol et
            var userRequest = new HttpRequestMessage(HttpMethod.Get, "api/user/me");
            userRequest.Headers.Add("Authorization", $"Bearer {token}");
            var userResponse = await httpClient.SendAsync(userRequest);
            
            if (userResponse.IsSuccessStatusCode)
            {
                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userResult = JsonSerializer.Deserialize<JsonElement>(userJson);
                var userRole = userResult.GetProperty("role").GetString();
                
                // Admin değilse abonelik kontrolü yap
                if (userRole != "Admin")
                {
                    var subscriptionRequest = new HttpRequestMessage(HttpMethod.Get, "api/subscription/my-subscription");
                    subscriptionRequest.Headers.Add("Authorization", $"Bearer {token}");
                    var subscriptionResponse = await httpClient.SendAsync(subscriptionRequest);
                    
                    if (subscriptionResponse.IsSuccessStatusCode)
                    {
                        var subscriptionJson = await subscriptionResponse.Content.ReadAsStringAsync();
                        var subscriptionResult = JsonSerializer.Deserialize<JsonElement>(subscriptionJson);
                        var hasSubscription = subscriptionResult.GetProperty("hasSubscription").GetBoolean();
                        
                        if (hasSubscription)
                        {
                            var subscription = JsonSerializer.Deserialize<Eventify.DTOs.Subscription.UserSubscriptionDto>(subscriptionResult.GetProperty("subscription").GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            
                            // Abonelik aktif mi kontrol et
                            if (!subscription.IsActive)
                            {
                                TempData["Error"] = "Etkinlik oluşturmak için aktif bir aboneliğiniz olmalıdır.";
                                return RedirectToAction("Plans", "Subscription");
                            }
                        }
                        else
                        {
                            TempData["Error"] = "Etkinlik oluşturmak için aktif bir aboneliğiniz olmalıdır.";
                            return RedirectToAction("Plans", "Subscription");
                        }
                    }
                    else
                    {
                        TempData["Error"] = "Etkinlik oluşturmak için aktif bir aboneliğiniz olmalıdır.";
                        return RedirectToAction("Plans", "Subscription");
                    }
                }
            }
            else
            {
                TempData["Error"] = "Kullanıcı bilgileri alınamadı.";
                return RedirectToAction("Login", "Auth");
            }

            // Hobileri çek
            var hobbiesReq = new HttpRequestMessage(HttpMethod.Get, "api/Hobby");
            hobbiesReq.Headers.Add("Authorization", $"Bearer {token}");
            var hobbiesResp = await _httpClient.SendAsync(hobbiesReq);
            var hobbies = new List<HobbyViewModel>();
            if (hobbiesResp.IsSuccessStatusCode)
            {
                var json = await hobbiesResp.Content.ReadAsStringAsync();
                hobbies = JsonSerializer.Deserialize<List<HobbyViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            // Kullanım istatistiklerini çek
            var usageRequest = new HttpRequestMessage(HttpMethod.Get, "api/subscription/usage-stats");
            usageRequest.Headers.Add("Authorization", $"Bearer {token}");
            var usageResponse = await httpClient.SendAsync(usageRequest);
            if (usageResponse.IsSuccessStatusCode)
            {
                try
                {
                    var usageJson = await usageResponse.Content.ReadAsStringAsync();
                    ViewBag.UsageStats = JsonSerializer.Deserialize<JsonElement>(usageJson);
                }
                catch (Exception ex)
                {
                    ViewBag.UsageStats = null;
                }
            }
            else
            {
                ViewBag.UsageStats = null;
            }

            var model = new EventCreateViewModel { AllHobbies = hobbies };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(EventCreateViewModel model)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");



            if (!ModelState.IsValid || model.SelectedHobbyIds == null || !model.SelectedHobbyIds.Any())
            {
                if (model.SelectedHobbyIds == null || !model.SelectedHobbyIds.Any())
                    ModelState.AddModelError("SelectedHobbyIds", "En az bir hobi seçmelisiniz.");
                if (model.EventPhoto == null || model.EventPhoto.Length == 0)
                    ModelState.AddModelError("EventPhoto", "Etkinlik fotoğrafı zorunludur.");
                
                // Hobileri tekrar yükle
                var hobbiesReq = new HttpRequestMessage(HttpMethod.Get, "api/Hobby");
                hobbiesReq.Headers.Add("Authorization", $"Bearer {token}");
                var hobbiesResp = await _httpClient.SendAsync(hobbiesReq);
                var hobbies = new List<HobbyViewModel>();
                if (hobbiesResp.IsSuccessStatusCode)
                {
                    var json = await hobbiesResp.Content.ReadAsStringAsync();
                    hobbies = JsonSerializer.Deserialize<List<HobbyViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                model.AllHobbies = hobbies;
                return View(model);
            }

            if (model.EventPhoto == null || model.EventPhoto.Length == 0)
            {
                ModelState.AddModelError("EventPhoto", "Etkinlik fotoğrafı zorunludur.");
                return View(model);
            }

            // Fotoğrafı yükle
            var photoRequest = new HttpRequestMessage(HttpMethod.Post, "api/events/upload-photo");
            photoRequest.Headers.Add("Authorization", $"Bearer {token}");
            var photoContent = new MultipartFormDataContent();
            photoContent.Add(new StreamContent(model.EventPhoto.OpenReadStream()), "file", model.EventPhoto.FileName);
            photoRequest.Content = photoContent;
            var photoResponse = await _httpClient.SendAsync(photoRequest);
            
            string photoPath = "";
            if (photoResponse.IsSuccessStatusCode)
            {
                var photoJson = await photoResponse.Content.ReadAsStringAsync();
                var photoResult = JsonSerializer.Deserialize<JsonElement>(photoJson);
                photoPath = photoResult.GetProperty("path").GetString();
            }
            else
            {
                TempData["Error"] = "Fotoğraf yüklenemedi.";
                return View(model);
            }

            // Etkinlik oluştur
            var dto = new
            {
                Title = model.Title,
                Description = model.Description,
                Location = model.Location,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                MaxParticipants = model.MaxParticipants,
                HobbyIds = model.SelectedHobbyIds,
                PhotoPath = photoPath
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "api/events");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["Error"] = errorContent;
                return View(model);
            }

            TempData["Success"] = "Etkinlik başarıyla oluşturuldu.";
            return RedirectToAction("Index", "Home");
        }
    }

    public class EventCreateViewModel
    {
        [Required(ErrorMessage = "Etkinlik başlığı gereklidir.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Etkinlik açıklaması gereklidir.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Etkinlik konumu gereklidir.")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi gereklidir.")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Maksimum katılımcı sayısı gereklidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Maksimum katılımcı sayısı en az 1 olmalıdır.")]
        public int MaxParticipants { get; set; }

        [Required(ErrorMessage = "Etkinlik fotoğrafı gereklidir.")]
        public IFormFile EventPhoto { get; set; }

        public List<HobbyViewModel> AllHobbies { get; set; } = new();

        [Required(ErrorMessage = "En az bir hobi seçmelisiniz.")]
        public List<int> SelectedHobbyIds { get; set; } = new();
    }
} 