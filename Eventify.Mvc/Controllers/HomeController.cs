using Eventify.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Eventify.DTOs.Event;
using Eventify.DTOs.Hobby;

namespace Eventify.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("Eventify.WebAPI");
        }

        public async Task<IActionResult> Index(string search, string location, DateTime? startDate, DateTime? endDate, int? hobbyId)
        {
            // Etkinlikler
            var response = await _httpClient.GetAsync("api/events");
            List<EventViewModel> events = new();
            List<int> favoriteEventIds = new();
            List<int> joinedEventIds = new();
            var token = HttpContext.Session.GetString("token");
            
            // Kullanıcı giriş yapmışsa favori ve katıldığı etkinlikleri çek
            if (!string.IsNullOrEmpty(token))
            {
                // Favori etkinlikler
                var favReq = new HttpRequestMessage(HttpMethod.Get, "api/User/favorites");
                favReq.Headers.Add("Authorization", $"Bearer {token}");
                var favResp = await _httpClient.SendAsync(favReq);
                if (favResp.IsSuccessStatusCode)
                {
                    var favJson = await favResp.Content.ReadAsStringAsync();
                    favoriteEventIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(favJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                
                // Katıldığı etkinlikler
                var joinedReq = new HttpRequestMessage(HttpMethod.Get, "api/events/joined");
                joinedReq.Headers.Add("Authorization", $"Bearer {token}");
                var joinedResp = await _httpClient.SendAsync(joinedReq);
                if (joinedResp.IsSuccessStatusCode)
                {
                    var joinedJson = await joinedResp.Content.ReadAsStringAsync();
                    var joined = System.Text.Json.JsonSerializer.Deserialize<List<EventSummaryViewModel>>(joinedJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    joinedEventIds = joined.Select(e => e.Id).ToList();
                }
            }
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiEvents = System.Text.Json.JsonSerializer.Deserialize<List<EventDetailDto>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                events = apiEvents.Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Description,
                    Location = e.Location,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    MaxParticipants = e.MaxParticipants,
                    CreatorUsername = e.CreatorUsername,
                    PhotoPath = e.PhotoPath,
                    Hobbies = e.Hobbies?.Select(h => new HobbyViewModel { Id = h.Id, Name = h.Name }).ToList() ?? new List<HobbyViewModel>(),
                    IsFavorite = favoriteEventIds.Contains(e.Id)
                }).ToList();
            }
            
            // Filtreleme
            if (!string.IsNullOrWhiteSpace(search))
                events = events.Where(e => e.Title.ToLower().Contains(search.ToLower())).ToList();
            if (!string.IsNullOrWhiteSpace(location))
                events = events.Where(e => e.Location.ToLower().Contains(location.ToLower())).ToList();
            if (startDate.HasValue)
                events = events.Where(e => e.StartDate >= startDate.Value).ToList();
            if (endDate.HasValue)
                events = events.Where(e => e.EndDate <= endDate.Value).ToList();
            if (hobbyId.HasValue)
                events = events.Where(e => e.Hobbies.Any(h => h.Id == hobbyId.Value)).ToList();
            
            // Tüm hobiler (filtre için)
            var hobbiesResp = await _httpClient.GetAsync("api/Hobby");
            var allHobbies = new List<HobbyViewModel>();
            if (hobbiesResp.IsSuccessStatusCode)
            {
                var json = await hobbiesResp.Content.ReadAsStringAsync();
                allHobbies = System.Text.Json.JsonSerializer.Deserialize<List<HobbyViewModel>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            ViewBag.Search = search;
            ViewBag.Location = location;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.HobbyId = hobbyId;
            ViewBag.AllHobbies = allHobbies;
            var model = new HomeIndexViewModel { Events = events, JoinedEventIds = joinedEventIds, FavoriteEventIds = favoriteEventIds };
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> JoinEvent(int eventId)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/events/{eventId}/join");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await response.Content.ReadAsStringAsync();
                return RedirectToAction("Index");
            }
            TempData["Success"] = "Etkinliğe başarıyla katıldınız. 200 jeton düşüldü.";
            return RedirectToAction("Index");
        }
    }

    public class HomeIndexViewModel
    {
        public List<EventViewModel> Events { get; set; }
        public List<int> JoinedEventIds { get; set; }
        public List<int> FavoriteEventIds { get; set; }
    }

    public class EventViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxParticipants { get; set; }
        public string CreatorUsername { get; set; }
        public string PhotoPath { get; set; }
        public List<HobbyViewModel> Hobbies { get; set; } = new();
        public bool IsFavorite { get; set; }
    }
}
