using Eventify.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eventify.DataAccess.Data
{
    public static class SubscriptionPlanSeed
    {
        public static void SeedSubscriptionPlans(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = 1,
                    Name = "Basic",
                    Description = "Küçük etkinlikler için ideal başlangıç paketi",
                    Price = 29.99m,
                    DurationInDays = 30,
                    MaxDailyJoins = 3,
                    MaxEventsPerMonth = 5,
                    MaxParticipantsPerEvent = 50,
                    CanCreateEvents = true,
                    HasPrioritySupport = false,
                    HasAdvancedAnalytics = false,
                    IsActive = true,
                    SortOrder = 1
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "Pro",
                    Description = "Orta ölçekli etkinlikler için profesyonel paket",
                    Price = 79.99m,
                    DurationInDays = 30,
                    MaxDailyJoins = 10,
                    MaxEventsPerMonth = 20,
                    MaxParticipantsPerEvent = 200,
                    CanCreateEvents = true,
                    HasPrioritySupport = true,
                    HasAdvancedAnalytics = false,
                    IsActive = true,
                    SortOrder = 2
                },
                new SubscriptionPlan
                {
                    Id = 3,
                    Name = "Organizer+",
                    Description = "Büyük etkinlikler için premium organizatör paketi",
                    Price = 199.99m,
                    DurationInDays = 30,
                    MaxDailyJoins = 50,
                    MaxEventsPerMonth = 100,
                    MaxParticipantsPerEvent = 1000,
                    CanCreateEvents = true,
                    HasPrioritySupport = true,
                    HasAdvancedAnalytics = true,
                    IsActive = true,
                    SortOrder = 3
                }
            );
        }
    }
} 