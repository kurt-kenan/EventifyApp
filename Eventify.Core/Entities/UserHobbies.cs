namespace Eventify.Core.Entities
{
    public class UserHobbies
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int HobbyId { get; set; }
        public Hobby Hobby { get; set; }
    }
}
