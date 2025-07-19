namespace Eventify.DTOs.Token
{
    public class PurchaseTokenDto
    {
        public int PackageId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // "credit_card", "paypal", etc.
        public string CardNumber { get; set; } = string.Empty;
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string Cvv { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
    }
} 