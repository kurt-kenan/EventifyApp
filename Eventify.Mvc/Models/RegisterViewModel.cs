using System.ComponentModel.DataAnnotations;

namespace Eventify.Mvc.Models
{
    public class RegisterViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required]
        public DateTime BirthDate { get; set; }
    }

}
