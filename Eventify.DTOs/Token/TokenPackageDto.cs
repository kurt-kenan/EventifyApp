namespace Eventify.DTOs.Token
{
    public class TokenPackageDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TokenAmount { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public bool IsPopular { get; set; }
        public string? BonusText { get; set; }
    }
} 