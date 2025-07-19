﻿namespace Eventify.DTOs.User
{
    public class UserRegisterDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;  
        public string Password { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public bool IsGoogleAccount { get; set; }
    }
}
