namespace Eventify.Mvc.Models
{
    public class EventSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Description { get; set; }
        public bool IsFavorite { get; set; }
        public string PhotoPath { get; set; }
        public int ParticipantCount { get; set; }
        public string Category { get; set; }
    }
} 