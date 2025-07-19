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
using Eventify.Mvc.Attributes;

namespace Eventify.Mvc.Controllers
{
    [AdminAuthorization]
    public class AdminController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        
        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("Eventify.WebAPI");
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        [SkipAdminAuthorization]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("isAdmin") == "true")
                return RedirectToAction("Panel");
            return View();
        }

        [HttpPost]
        [SkipAdminAuthorization]
        public async Task<IActionResult> Login(string usernameOrEmail, string password)
        {
            var loginDto = new { UsernameOrEmail = usernameOrEmail, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/user/admin-login", loginDto);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var token = root.GetProperty("token").GetString();
                var fullName = root.TryGetProperty("fullName", out var fn) ? fn.GetString() : null;
                
                // Session'ları ayarla
                HttpContext.Session.SetString("token", token);
                HttpContext.Session.SetString("Username", fullName ?? usernameOrEmail);
                HttpContext.Session.SetString("isAdmin", "true");
                
                // Session'ları hemen commit et
                HttpContext.Session.CommitAsync().Wait();
                
                return RedirectToAction("Panel");
            }
            ViewBag.Error = await response.Content.ReadAsStringAsync();
            return View();
        }

        [SkipAdminAuthorization]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }



        [HttpGet]
        public IActionResult Panel()
        {
            if (HttpContext.Session.GetString("isAdmin") != "true")
                return RedirectToAction("Login");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Users(string search, string role)
        {
            if (HttpContext.Session.GetString("isAdmin") != "true")
                return RedirectToAction("Login");
            var token = HttpContext.Session.GetString("token");
            
            var usersRequest = new HttpRequestMessage(HttpMethod.Get, "api/admin/users");
            usersRequest.Headers.Add("Authorization", $"Bearer {token}");
            var usersResponse = await _httpClient.SendAsync(usersRequest);
            
            var users = new List<AdminUserViewModel>();
            if (usersResponse.IsSuccessStatusCode)
            {
                var json = await usersResponse.Content.ReadAsStringAsync();
                users = JsonSerializer.Deserialize<List<AdminUserViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                var errorContent = await usersResponse.Content.ReadAsStringAsync();
                TempData["Error"] = $"Kullanıcılar alınamadı: {errorContent}";
            }
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                users = users.Where(u =>
                    (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(s)) ||
                    (!string.IsNullOrEmpty(u.Username) && u.Username.ToLower().Contains(s)) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(s))
                ).ToList();
            }
            if (!string.IsNullOrWhiteSpace(role) && role != "Hepsi")
            {
                users = users.Where(u => u.Role == role).ToList();
            }
            ViewBag.Search = search;
            ViewBag.Role = role;
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (HttpContext.Session.GetString("isAdmin") != "true")
                return RedirectToAction("Login");
            var token = HttpContext.Session.GetString("token");
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/admin/users/{id}");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                TempData["Error"] = await response.Content.ReadAsStringAsync();
            else
                TempData["Success"] = "Kullanıcı silindi.";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> Events(string search, int? minParticipants, int? maxParticipants, string creator)
        {
            if (HttpContext.Session.GetString("isAdmin") != "true")
                return RedirectToAction("Login");
            var token = HttpContext.Session.GetString("token");
            var eventsRequest = new HttpRequestMessage(HttpMethod.Get, "api/events");
            eventsRequest.Headers.Add("Authorization", $"Bearer {token}");
            var eventsResponse = await _httpClient.SendAsync(eventsRequest);
            var events = new List<AdminEventViewModel>();
            if (eventsResponse.IsSuccessStatusCode)
            {
                var json = await eventsResponse.Content.ReadAsStringAsync();
                events = JsonSerializer.Deserialize<List<AdminEventViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                events = events.Where(ev =>
                    (!string.IsNullOrEmpty(ev.Title) && ev.Title.ToLower().Contains(s)) ||
                    (!string.IsNullOrEmpty(ev.Location) && ev.Location.ToLower().Contains(s)) ||
                    (!string.IsNullOrEmpty(ev.CreatorUsername) && ev.CreatorUsername.ToLower().Contains(s)) ||
                    (!string.IsNullOrEmpty(ev.CreatorFullName) && ev.CreatorFullName.ToLower().Contains(s))
                ).ToList();
            }
            if (minParticipants.HasValue)
            {
                events = events.Where(ev => ev.MaxParticipants >= minParticipants.Value).ToList();
            }
            if (maxParticipants.HasValue)
            {
                events = events.Where(ev => ev.MaxParticipants <= maxParticipants.Value).ToList();
            }
            if (!string.IsNullOrWhiteSpace(creator))
            {
                var c = creator.ToLower();
                events = events.Where(ev =>
                    (!string.IsNullOrEmpty(ev.CreatorUsername) && ev.CreatorUsername.ToLower().Contains(c)) ||
                    (!string.IsNullOrEmpty(ev.CreatorFullName) && ev.CreatorFullName.ToLower().Contains(c))
                ).ToList();
            }
            ViewBag.Search = search;
            ViewBag.MinParticipants = minParticipants;
            ViewBag.MaxParticipants = maxParticipants;
            ViewBag.Creator = creator;
            return View(events);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            if (HttpContext.Session.GetString("isAdmin") != "true")
                return RedirectToAction("Login");
            var token = HttpContext.Session.GetString("token");
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/events/{id}");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                TempData["Error"] = await response.Content.ReadAsStringAsync();
            else
                TempData["Success"] = "Etkinlik silindi.";
            return RedirectToAction("Events");
        }

        [HttpGet]
        public async Task<IActionResult> AddEvent()
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
                
                // Admin ise devam et
                if (userRole == "Admin")
                {
                    // Devam et
                }
                else
                {
                    // Admin değilse abonelik kontrolü yap
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

            // Admin için usage stats gerekmez

            var model = new AdminEventCreateViewModel { AllHobbies = hobbies };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddEvent(AdminEventCreateViewModel model)
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
                else
                {
                    // Admin ise abonelik kontrolü yapma, direkt devam et
                }
            }
            else
            {
                TempData["Error"] = "Kullanıcı bilgileri alınamadı.";
                return RedirectToAction("Login", "Auth");
            }
            // Hobileri tekrar çek (validasyon için)
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
            if (!ModelState.IsValid || model.SelectedHobbyIds == null || !model.SelectedHobbyIds.Any())
            {
                if (model.SelectedHobbyIds == null || !model.SelectedHobbyIds.Any())
                    ModelState.AddModelError("SelectedHobbyIds", "En az bir hobi seçmelisiniz.");
                if (model.EventPhoto == null || model.EventPhoto.Length == 0)
                    ModelState.AddModelError("EventPhoto", "Etkinlik fotoğrafı zorunludur.");
                return View(model);
            }
            if (model.EventPhoto == null || model.EventPhoto.Length == 0)
            {
                ModelState.AddModelError("EventPhoto", "Etkinlik fotoğrafı zorunludur.");
                return View(model);
            }
            // Fotoğrafı wwwroot/eventphotos'a kaydet
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/eventphotos");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);
            var ext = Path.GetExtension(model.EventPhoto.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploads, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.EventPhoto.CopyToAsync(stream);
            }
            var photoPath = $"/eventphotos/{fileName}";
            var dto = new {
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
            request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await response.Content.ReadAsStringAsync();
                return View(model);
            }
            TempData["Success"] = "Etkinlik eklendi.";
            return RedirectToAction("Events");
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            if (HttpContext.Session.GetString("isAdmin") != "true")
                return RedirectToAction("Login");
            var token = HttpContext.Session.GetString("token");
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/events/{id}");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Events");
            var json = await response.Content.ReadAsStringAsync();
            // Önce EventDetailDto'ya deserialize et
            var eventDetail = JsonSerializer.Deserialize<Eventify.DTOs.Event.EventDetailDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var ev = new AdminEventViewModel
            {
                Id = eventDetail.Id,
                Title = eventDetail.Title,
                Description = eventDetail.Description,
                Location = eventDetail.Location,
                StartDate = eventDetail.StartDate,
                EndDate = eventDetail.EndDate,
                MaxParticipants = eventDetail.MaxParticipants,
                CreatorUsername = eventDetail.CreatorUsername,
                SelectedHobbyIds = eventDetail.Hobbies?.Select(h => h.Id).ToList() ?? new List<int>()
            };
            // Hobileri çek
            var hobbiesReq = new HttpRequestMessage(HttpMethod.Get, "api/Hobby");
            hobbiesReq.Headers.Add("Authorization", $"Bearer {token}");
            var hobbiesResp = await _httpClient.SendAsync(hobbiesReq);
            var hobbies = new List<HobbyViewModel>();
            if (hobbiesResp.IsSuccessStatusCode)
            {
                var hobbiesJson = await hobbiesResp.Content.ReadAsStringAsync();
                hobbies = JsonSerializer.Deserialize<List<HobbyViewModel>>(hobbiesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            ev.AllHobbies = hobbies;
            return View(ev);
        }

        [HttpPost]
        public async Task<IActionResult> EditEvent(AdminEventViewModel model)
        {
            if (HttpContext.Session.GetString("isAdmin") != "true")
                return RedirectToAction("Login");
            var token = HttpContext.Session.GetString("token");
            ModelState.Remove("PhotoPath");
            ModelState.Remove("CreatorFullName");
            ModelState.Remove("CreatorUsername");
            
            // Debug bilgileri
            TempData["DebugModelState"] = ModelState.IsValid ? "Valid" : "Invalid";
            TempData["DebugHobbies"] = model.SelectedHobbyIds == null ? "null" : string.Join(",", model.SelectedHobbyIds);
            TempData["DebugModelId"] = model.Id.ToString();
            
            // Hobileri tekrar çek (validasyon için)
            var hobbiesReq = new HttpRequestMessage(HttpMethod.Get, "api/Hobby");
            hobbiesReq.Headers.Add("Authorization", $"Bearer {token}");
            var hobbiesResp = await _httpClient.SendAsync(hobbiesReq);
            var hobbies = new List<HobbyViewModel>();
            if (hobbiesResp.IsSuccessStatusCode)
            {
                var hobbiesJson = await hobbiesResp.Content.ReadAsStringAsync();
                hobbies = JsonSerializer.Deserialize<List<HobbyViewModel>>(hobbiesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            model.AllHobbies = hobbies;
            
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Model validasyon hatası: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }
            
            if (model.SelectedHobbyIds == null || !model.SelectedHobbyIds.Any())
            {
                ModelState.AddModelError("SelectedHobbyIds", "En az bir hobi seçmelisiniz.");
                TempData["Error"] = "En az bir hobi seçmelisiniz.";
                return View(model);
            }
            
            string photoPath = model.PhotoPath;
            if (model.EventPhoto != null && model.EventPhoto.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/eventphotos");
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);
                var ext = Path.GetExtension(model.EventPhoto.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.EventPhoto.CopyToAsync(stream);
                }
                photoPath = $"/eventphotos/{fileName}";
            }
            
            var dto = new {
                Title = model.Title,
                Description = model.Description,
                Location = model.Location,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                MaxParticipants = model.MaxParticipants,
                HobbyIds = model.SelectedHobbyIds,
                PhotoPath = photoPath
            };
            
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/events/{model.Id}");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"API Hatası ({response.StatusCode}): {errorContent}";
                return View(model);
            }
            
            TempData["Success"] = "Etkinlik güncellendi.";
            return RedirectToAction("Events");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            if (HttpContext.Session.GetString("isAdmin") != "true")
                return RedirectToAction("Login");
            var token = HttpContext.Session.GetString("token");
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/admin/users");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return RedirectToAction("Users");
            var json = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<AdminUserViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var user = users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return RedirectToAction("Users");
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(AdminUserViewModel model)
        {
            if (HttpContext.Session.GetString("isAdmin") != "true")
                return RedirectToAction("Login");
            var token = HttpContext.Session.GetString("token");
            var dto = new {
                FullName = model.FullName,
                Email = model.Email,
                Role = model.Role
            };
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/admin/users/{model.Id}");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await response.Content.ReadAsStringAsync();
                return View(model);
            }
            TempData["Success"] = "Kullanıcı güncellendi.";
            return RedirectToAction("Users");
        }
    }
} 