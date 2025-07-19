using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Eventify.Core.Entities
{
    public class EmailVerification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public bool IsUsed { get; set; } = false;

        // Navigation property
        public User User { get; set; }
    }
}
