using Microsoft.AspNetCore.Http;

namespace Eventify.Mvc.Models
{
    public class AdminPanelViewModel
    {
        public List<AdminUserViewModel> Users { get; set; }
        public List<AdminEventViewModel> Events { get; set; }
    }
    public class AdminUserViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class AdminEventViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxParticipants { get; set; }
        public string CreatorUsername { get; set; }
        public string CreatorFullName { get; set; }
        public List<int> SelectedHobbyIds { get; set; } = new();
        public List<HobbyViewModel> AllHobbies { get; set; } = new();
        public string PhotoPath { get; set; }
        public IFormFile EventPhoto { get; set; }
    }
    public class AdminEventCreateViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxParticipants { get; set; }
        public List<int> SelectedHobbyIds { get; set; } = new();
        public List<HobbyViewModel> AllHobbies { get; set; } = new();
        public IFormFile EventPhoto { get; set; }
    }
} 