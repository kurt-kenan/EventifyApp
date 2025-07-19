using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Eventify.Core.Entities
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string Location { get; set; } = null!;

        [Range(0, int.MaxValue)]
        public int MaxParticipants { get; set; }

        [Required]
        public int CreatorId { get; set; }

        [Required]
        public User Creator { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public string PhotoPath { get; set; }

        public List<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
        public ICollection<EventHobby> EventHobbies { get; set; } = new List<EventHobby>();
        public ICollection<FavoriteEvent> FavoritedByUsers { get; set; }
    }
}
