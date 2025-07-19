using System.ComponentModel.DataAnnotations;

namespace Eventify.Mvc.Models
{
    public class LoginViewModel
    {
        [Required]
        public string UsernameOrEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
