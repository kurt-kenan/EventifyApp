using Eventify.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eventify.DataAccess.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
        public DbSet<EmailVerification> EmailVerifications { get; set; }
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
        public DbSet<TokenTransaction> TokenTransactions { get; set; }
        public DbSet<TokenPackage> TokenPackages => Set<TokenPackage>();
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<EventHobby> EventHobbies { get; set; }
        public DbSet<FavoriteEvent> FavoriteEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Kullanıcı-Abonelik ilişkisi
            modelBuilder.Entity<UserSubscription>()
                .HasOne(us => us.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(us => us.UserId);

            modelBuilder.Entity<UserSubscription>()
                .HasOne(us => us.Plan)
                .WithMany(p => p.UserSubscriptions)
                .HasForeignKey(us => us.PlanId);

            // Abonelik fiyatı decimal ayarı
            modelBuilder.Entity<SubscriptionPlan>()
                .Property(sp => sp.Price)
                .HasColumnType("decimal(18,2)");

            // Event -> CreatedBy (User) ilişkisi
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Creator)
                .WithMany(u => u.CreatedEvents)
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed data
            SubscriptionPlanSeed.SeedSubscriptionPlans(modelBuilder);
            TokenPackageSeed.SeedTokenPackages(modelBuilder);

            // EventParticipant -> User
            modelBuilder.Entity<EventParticipant>()
                .HasOne(ep => ep.User)
                .WithMany(u => u.ParticipatedEvents)
                .HasForeignKey(ep => ep.UserId);

            // EventParticipant -> Event
            modelBuilder.Entity<EventParticipant>()
                .HasOne(ep => ep.Event)
                .WithMany(e => e.Participants)
                .HasForeignKey(ep => ep.EventId);

            modelBuilder.Entity<TokenTransaction>()
                .HasOne(tt => tt.User)
                .WithMany(u => u.TokenTransactions)
                .HasForeignKey(tt => tt.UserId);

            modelBuilder.Entity<UserHobbies>()
                .HasKey(uh => new { uh.UserId, uh.HobbyId });

            modelBuilder.Entity<UserHobbies>()
                .HasOne(uh => uh.User)
                .WithMany(u => u.UserHobbies)
                .HasForeignKey(uh => uh.UserId);

            modelBuilder.Entity<UserHobbies>()
                .HasOne(uh => uh.Hobby)
                .WithMany(h => h.UserHobbies)
                .HasForeignKey(uh => uh.HobbyId);

            modelBuilder.Entity<EventHobby>()
                .HasKey(eh => new { eh.EventId, eh.HobbyId });
            modelBuilder.Entity<EventHobby>()
                .HasOne(eh => eh.Event)
                .WithMany(e => e.EventHobbies)
                .HasForeignKey(eh => eh.EventId);
            modelBuilder.Entity<EventHobby>()
                .HasOne(eh => eh.Hobby)
                .WithMany(h => h.EventHobbies)
                .HasForeignKey(eh => eh.HobbyId);

            modelBuilder.Entity<FavoriteEvent>()
                .HasKey(fe => fe.Id);
            modelBuilder.Entity<FavoriteEvent>()
                .HasOne(fe => fe.User)
                .WithMany(u => u.FavoriteEvents)
                .HasForeignKey(fe => fe.UserId);
            modelBuilder.Entity<FavoriteEvent>()
                .HasOne(fe => fe.Event)
                .WithMany(e => e.FavoritedByUsers)
                .HasForeignKey(fe => fe.EventId);
        }
    }
}
