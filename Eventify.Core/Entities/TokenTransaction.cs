namespace Eventify.Core.Entities
{
    public class TokenTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public int Amount { get; set; } // Pozitif: satın alma, negatif: harcama
        public string Reason { get; set; } = string.Empty; // Örn: "Jeton satın alındı", "Etkinlik katılımı"
        public DateTime CreatedAt { get; set; }
        public string? TransactionId { get; set; } // Ödeme işlemi ID'si
        public string? PaymentMethod { get; set; } // Ödeme yöntemi
        public decimal? Price { get; set; } // Ödenen tutar
    }
}
