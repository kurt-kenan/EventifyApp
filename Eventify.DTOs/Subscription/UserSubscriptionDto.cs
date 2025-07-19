namespace Eventify.DTOs.Subscription
{
    public class UserSubscriptionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int DaysRemaining { get; set; }
        public SubscriptionPlanDto Plan { get; set; } = new SubscriptionPlanDto();
    }
} 