using System.ComponentModel.DataAnnotations;

namespace Eventify.DTOs.Event
{
    public class EventCreateDto
    {
        [Required]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public string Location { get; set; } = null!;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxParticipants { get; set; }
        public List<int> HobbyIds { get; set; } = new();
        [Required]
        public string PhotoPath { get; set; }
    }
}
