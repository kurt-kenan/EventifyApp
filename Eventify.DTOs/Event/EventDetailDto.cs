using Eventify.DTOs.Hobby;

namespace Eventify.DTOs.Event
{
    public class EventDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string Location { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxParticipants { get; set; }
        public string CreatorUsername { get; set; } = null!;
        public string PhotoPath { get; set; }
        public int ParticipantCount { get; set; }
        public List<HobbyDto> Hobbies { get; set; } = new();
    }
}
