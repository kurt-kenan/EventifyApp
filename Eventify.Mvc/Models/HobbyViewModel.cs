namespace Eventify.Mvc.Models
{
    public class HobbyViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class HobbiesSelectViewModel
    {
        public List<HobbyViewModel> AllHobbies { get; set; }
        public List<int> SelectedHobbyIds { get; set; }
    }
} 