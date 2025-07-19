using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Eventify.Mvc.Models;
using Eventify.DTOs.Event;
using System.ComponentModel.DataAnnotations;
using Eventify.Mvc.Attributes;

namespace Eventify.Mvc.Controllers
{
    [UserAuthorization]
    public class ProfileController : Controller
    {
        private readonly HttpClient _httpClient;
        public ProfileController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Eventify.WebAPI");
        }

        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var isAdmin = HttpContext.Session.GetString("isAdmin") == "true";
            ViewBag.IsAdmin = isAdmin;

            var request = new HttpRequestMessage(HttpMethod.Get, "api/user/me");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Profil bilgileri alınamadı.";
                return View();
            }
            var json = await response.Content.ReadAsStringAsync();
            var profile = JsonSerializer.Deserialize<ProfileViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var request = new HttpRequestMessage(HttpMethod.Put, "api/user/me");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = new StringContent(JsonSerializer.Serialize(model), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Profil güncellenemedi.";
                return RedirectToAction("Index");
            }
            TempData["Success"] = "Profil başarıyla güncellendi.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");
            var request = new HttpRequestMessage(HttpMethod.Post, "api/user/change-password");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(model), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await response.Content.ReadAsStringAsync();
                return View(model);
            }
            TempData["Success"] = "Şifre başarıyla değiştirildi.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> MyEvents()
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");
            var request = new HttpRequestMessage(HttpMethod.Get, "api/events/mine");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Etkinlikler alınamadı.";
                return View();
            }
            var json = await response.Content.ReadAsStringAsync();
            var apiEvents = System.Text.Json.JsonSerializer.Deserialize<List<EventDetailDto>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            var events = apiEvents.Select(e => new EventSummaryViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Location = e.Location,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                PhotoPath = e.PhotoPath,
                ParticipantCount = e.ParticipantCount,
                Category = e.Hobbies.FirstOrDefault()?.Name ?? "Genel"
            }).ToList();
            
            return View(events);
        }

        public async Task<IActionResult> JoinedEvents()
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");
            var request = new HttpRequestMessage(HttpMethod.Get, "api/events/joined");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Katıldığınız etkinlikler alınamadı.";
                return View();
            }
            var json = await response.Content.ReadAsStringAsync();
            var apiEvents = System.Text.Json.JsonSerializer.Deserialize<List<EventDetailDto>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            var events = apiEvents.Select(e => new EventSummaryViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Location = e.Location,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                PhotoPath = e.PhotoPath,
                ParticipantCount = e.ParticipantCount,
                Category = e.Hobbies.FirstOrDefault()?.Name ?? "Genel"
            }).ToList();
            
            return View(events);
        }



        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Bir dosya seçmelisiniz.";
                return RedirectToAction("Index");
            }
            var request = new HttpRequestMessage(HttpMethod.Post, "api/user/upload-photo");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);
            request.Content = content;
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await response.Content.ReadAsStringAsync();
                return RedirectToAction("Index");
            }
            TempData["Success"] = "Profil fotoğrafı başarıyla yüklendi.";
            return RedirectToAction("Index");
        }



        public async Task<PartialViewResult> TokenInfo()
        {
            var token = HttpContext.Session.GetString("token");
            int tokens = 0;
            if (!string.IsNullOrEmpty(token))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/user/tokens");
                request.Headers.Add("Authorization", $"Bearer {token}");
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var obj = System.Text.Json.JsonDocument.Parse(json);
                    tokens = obj.RootElement.GetProperty("tokens").GetInt32();
                }
            }
            return PartialView("_TokenInfo", tokens);
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            // Etkinliği getir
            var eventRequest = new HttpRequestMessage(HttpMethod.Get, $"api/events/{id}");
            eventRequest.Headers.Add("Authorization", $"Bearer {token}");
            var eventResponse = await _httpClient.SendAsync(eventRequest);
            
            if (!eventResponse.IsSuccessStatusCode)
            {
                TempData["Error"] = "Etkinlik bulunamadı.";
                return RedirectToAction("MyEvents");
            }

            var eventJson = await eventResponse.Content.ReadAsStringAsync();
            var eventDetail = JsonSerializer.Deserialize<EventDetailDto>(eventJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Kullanıcının kendi etkinliği mi kontrol et
            var userRequest = new HttpRequestMessage(HttpMethod.Get, "api/user/me");
            userRequest.Headers.Add("Authorization", $"Bearer {token}");
            var userResponse = await _httpClient.SendAsync(userRequest);
            
            if (userResponse.IsSuccessStatusCode)
            {
                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userResult = JsonSerializer.Deserialize<JsonElement>(userJson);
                var currentUsername = userResult.GetProperty("username").GetString();
                
                if (eventDetail.CreatorUsername != currentUsername)
                {
                    TempData["Error"] = "Bu etkinliği düzenleme yetkiniz yok.";
                    return RedirectToAction("MyEvents");
                }
            }

            // Hobileri getir
            var hobbiesRequest = new HttpRequestMessage(HttpMethod.Get, "api/Hobby");
            hobbiesRequest.Headers.Add("Authorization", $"Bearer {token}");
            var hobbiesResponse = await _httpClient.SendAsync(hobbiesRequest);
            var hobbies = new List<HobbyViewModel>();
            
            if (hobbiesResponse.IsSuccessStatusCode)
            {
                var hobbiesJson = await hobbiesResponse.Content.ReadAsStringAsync();
                hobbies = JsonSerializer.Deserialize<List<HobbyViewModel>>(hobbiesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            var model = new EventEditViewModel
            {
                Id = eventDetail.Id,
                Title = eventDetail.Title,
                Description = eventDetail.Description,
                Location = eventDetail.Location,
                StartDate = eventDetail.StartDate,
                EndDate = eventDetail.EndDate,
                MaxParticipants = eventDetail.MaxParticipants,
                PhotoPath = eventDetail.PhotoPath,
                AllHobbies = hobbies,
                SelectedHobbyIds = eventDetail.Hobbies?.Select(h => h.Id).ToList() ?? new List<int>()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditEvent(EventEditViewModel model)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                // Hobileri tekrar yükle
                var hobbiesRequest = new HttpRequestMessage(HttpMethod.Get, "api/Hobby");
                hobbiesRequest.Headers.Add("Authorization", $"Bearer {token}");
                var hobbiesResponse = await _httpClient.SendAsync(hobbiesRequest);
                var hobbies = new List<HobbyViewModel>();
                
                if (hobbiesResponse.IsSuccessStatusCode)
                {
                    var hobbiesJson = await hobbiesResponse.Content.ReadAsStringAsync();
                    hobbies = JsonSerializer.Deserialize<List<HobbyViewModel>>(hobbiesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                model.AllHobbies = hobbies;
                return View(model);
            }

            // Etkinlik güncelleme DTO'su oluştur
            var updateDto = new EventCreateDto
            {
                Title = model.Title,
                Description = model.Description,
                Location = model.Location,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                MaxParticipants = model.MaxParticipants,
                PhotoPath = model.PhotoPath,
                HobbyIds = model.SelectedHobbyIds ?? new List<int>()
            };

            var request = new HttpRequestMessage(HttpMethod.Put, $"api/events/{model.Id}");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = new StringContent(JsonSerializer.Serialize(updateDto), System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await response.Content.ReadAsStringAsync();
                return RedirectToAction("EditEvent", new { id = model.Id });
            }

            TempData["Success"] = "Etkinlik başarıyla güncellendi.";
            return RedirectToAction("MyEvents");
        }

        public async Task<IActionResult> Hobbies()
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");
            // Tüm hobiler
            var allHobbiesReq = new HttpRequestMessage(HttpMethod.Get, "api/Hobby");
            allHobbiesReq.Headers.Add("Authorization", $"Bearer {token}");
            var allHobbiesResp = await _httpClient.SendAsync(allHobbiesReq);
            var allHobbies = new List<HobbyViewModel>();
            if (allHobbiesResp.IsSuccessStatusCode)
            {
                var json = await allHobbiesResp.Content.ReadAsStringAsync();
                allHobbies = JsonSerializer.Deserialize<List<HobbyViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            // Kullanıcının hobileri
            var myHobbiesReq = new HttpRequestMessage(HttpMethod.Get, "api/Hobby/my-hobbies");
            myHobbiesReq.Headers.Add("Authorization", $"Bearer {token}");
            var myHobbiesResp = await _httpClient.SendAsync(myHobbiesReq);
            var myHobbies = new List<int>();
            if (myHobbiesResp.IsSuccessStatusCode)
            {
                var json = await myHobbiesResp.Content.ReadAsStringAsync();
                var hobbies = JsonSerializer.Deserialize<List<HobbyViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                myHobbies = hobbies.Select(h => h.Id).ToList();
            }
            var model = new HobbiesSelectViewModel { AllHobbies = allHobbies, SelectedHobbyIds = myHobbies };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Hobbies(HobbiesSelectViewModel model)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            try
            {
                // Önce mevcut hobileri al
                var myHobbiesReq = new HttpRequestMessage(HttpMethod.Get, "api/Hobby/my-hobbies");
                myHobbiesReq.Headers.Add("Authorization", $"Bearer {token}");
                var myHobbiesResp = await _httpClient.SendAsync(myHobbiesReq);
                var currentHobbies = new List<int>();
                
                if (myHobbiesResp.IsSuccessStatusCode)
                {
                    var json = await myHobbiesResp.Content.ReadAsStringAsync();
                    var hobbies = JsonSerializer.Deserialize<List<HobbyViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    currentHobbies = hobbies.Select(h => h.Id).ToList();
                }

                // Seçili hobileri al (null ise boş liste)
                var selectedHobbies = model.SelectedHobbyIds ?? new List<int>();

                // Çıkarılacak hobileri bul (mevcut - seçili)
                var hobbiesToRemove = currentHobbies.Except(selectedHobbies).ToList();

                // Eklenecek hobileri bul (seçili - mevcut)
                var hobbiesToAdd = selectedHobbies.Except(currentHobbies).ToList();

                // Çıkarılacak hobileri sil
                foreach (var hobbyId in hobbiesToRemove)
                {
                    var removeReq = new HttpRequestMessage(HttpMethod.Post, "api/Hobby/remove-from-user");
                    removeReq.Headers.Add("Authorization", $"Bearer {token}");
                    removeReq.Content = new StringContent(JsonSerializer.Serialize(new { HobbyId = hobbyId }), System.Text.Encoding.UTF8, "application/json");
                    await _httpClient.SendAsync(removeReq);
                }

                // Eklenecek hobileri ekle
                foreach (var hobbyId in hobbiesToAdd)
                {
                    var addReq = new HttpRequestMessage(HttpMethod.Post, "api/Hobby/add-to-user");
                    addReq.Headers.Add("Authorization", $"Bearer {token}");
                    addReq.Content = new StringContent(JsonSerializer.Serialize(new { HobbyId = hobbyId }), System.Text.Encoding.UTF8, "application/json");
                    await _httpClient.SendAsync(addReq);
                }

                TempData["Success"] = "Hobileriniz başarıyla güncellendi.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hobi güncelleme hatası: {ex.Message}";
            }

            return RedirectToAction("Hobbies");
        }

        public async Task<IActionResult> Favorites()
                {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");
            var request = new HttpRequestMessage(HttpMethod.Get, "api/user/favorites");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Favori etkinlikler alınamadı.";
                return View(new List<int>());
            }
            var json = await response.Content.ReadAsStringAsync();
            var favoriteEventIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            // Favori etkinlik detaylarını çek
            var events = new List<Eventify.Mvc.Models.EventSummaryViewModel>();
            foreach (var eventId in favoriteEventIds)
            {
                var eventReq = new HttpRequestMessage(HttpMethod.Get, $"api/events/{eventId}");
                eventReq.Headers.Add("Authorization", $"Bearer {token}");
                var eventResp = await _httpClient.SendAsync(eventReq);
                if (eventResp.IsSuccessStatusCode)
                {
                    var eventJson = await eventResp.Content.ReadAsStringAsync();
                    var eventObj = System.Text.Json.JsonSerializer.Deserialize<EventDetailDto>(eventJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (eventObj != null)
                    {
                        events.Add(new EventSummaryViewModel
                        {
                            Id = eventObj.Id,
                            Title = eventObj.Title,
                            Description = eventObj.Description,
                            Location = eventObj.Location,
                            StartDate = eventObj.StartDate,
                            EndDate = eventObj.EndDate,
                            PhotoPath = eventObj.PhotoPath,
                            ParticipantCount = eventObj.ParticipantCount,
                            Category = eventObj.Hobbies.FirstOrDefault()?.Name ?? "Genel"
                        });
                    }
                }
            }
            return View(events);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int eventId)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return Json(new { success = false, message = "Giriş yapmalısınız." });

            try
            {
                // Önce mevcut favori mi kontrol et
                var favCheckReq = new HttpRequestMessage(HttpMethod.Get, "api/User/favorites");
                favCheckReq.Headers.Add("Authorization", $"Bearer {token}");
                var favCheckResp = await _httpClient.SendAsync(favCheckReq);
                var isFavorite = false;
                if (favCheckResp.IsSuccessStatusCode)
                {
                    var favJson = await favCheckResp.Content.ReadAsStringAsync();
                    var favoriteEventIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(favJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    isFavorite = favoriteEventIds.Contains(eventId);
                }

                if (isFavorite)
                {
                    // Favoriden çıkar
                    var req = new HttpRequestMessage(HttpMethod.Post, $"api/User/remove-favorite");
                    req.Headers.Add("Authorization", $"Bearer {token}");
                    req.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { EventId = eventId }), System.Text.Encoding.UTF8, "application/json");
                    var resp = await _httpClient.SendAsync(req);
                    
                    if (resp.IsSuccessStatusCode)
                    {
                        return Json(new { success = true, isFavorite = false });
                    }
                    else
                    {
                        var errorContent = await resp.Content.ReadAsStringAsync();
                        return Json(new { success = false, message = $"Favoriden çıkarma hatası: {errorContent}" });
                    }
                }
                else
                {
                    // Favoriye ekle
                    var req = new HttpRequestMessage(HttpMethod.Post, $"api/User/add-favorite");
                    req.Headers.Add("Authorization", $"Bearer {token}");
                    req.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { EventId = eventId }), System.Text.Encoding.UTF8, "application/json");
                    var resp = await _httpClient.SendAsync(req);
                    
                    if (resp.IsSuccessStatusCode)
                    {
                        return Json(new { success = true, isFavorite = true });
                    }
                    else
                    {
                        var errorContent = await resp.Content.ReadAsStringAsync();
                        return Json(new { success = false, message = $"Favori ekleme hatası: {errorContent}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"İşlem hatası: {ex.Message}" });
            }
        }
    }

    public class ProfileViewModel
    {
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime BirthDate { get; set; }
        public string Role { get; set; }
        public int Tokens { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PhotoPath { get; set; }
    }

    public class ChangePasswordViewModel
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class EventEditViewModel
    {
        public int Id { get; set; }
        
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
        [Range(1, 1000, ErrorMessage = "Maksimum katılımcı sayısı 1-1000 arasında olmalıdır.")]
        public int MaxParticipants { get; set; }
        
        public string PhotoPath { get; set; }
        
        public List<HobbyViewModel> AllHobbies { get; set; } = new();
        
        [Required(ErrorMessage = "En az bir hobi seçmelisiniz.")]
        public List<int> SelectedHobbyIds { get; set; } = new();
    }

} 