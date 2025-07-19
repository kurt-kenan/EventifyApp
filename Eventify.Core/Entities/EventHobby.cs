namespace Eventify.Core.Entities
{
    public class EventHobby
    {
        public int EventId { get; set; }
        public Event Event { get; set; }
        public int HobbyId { get; set; }
        public Hobby Hobby { get; set; }
    }
} 