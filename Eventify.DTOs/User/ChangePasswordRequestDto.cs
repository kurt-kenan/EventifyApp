﻿namespace Eventify.DTOs.User
{
    public class ChangePasswordRequestDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
