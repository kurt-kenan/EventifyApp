using Eventify.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eventify.DataAccess.Data
{
    public static class TokenPackageSeed
    {
        public static void SeedTokenPackages(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TokenPackage>().HasData(
                new TokenPackage
                {
                    Id = 1,
                    Name = "500 Jeton",
                    Description = "Küçük paket - Hızlı başlangıç",
                    TokenAmount = 500,
                    Price = 19.99m,
                    IsActive = true,
                    SortOrder = 1,
                    IsPopular = false,
                    BonusText = null
                },
                new TokenPackage
                {
                    Id = 2,
                    Name = "1000 Jeton",
                    Description = "Orta paket - En popüler seçim",
                    TokenAmount = 1000,
                    Price = 34.99m,
                    IsActive = true,
                    SortOrder = 2,
                    IsPopular = true,
                    BonusText = "En Popüler"
                },
                new TokenPackage
                {
                    Id = 3,
                    Name = "5000 Jeton",
                    Description = "Büyük paket - %10 bonus",
                    TokenAmount = 5000,
                    Price = 149.99m,
                    IsActive = true,
                    SortOrder = 3,
                    IsPopular = false,
                    BonusText = "%10 Bonus"
                },
                new TokenPackage
                {
                    Id = 4,
                    Name = "10000 Jeton",
                    Description = "Mega paket - %15 bonus",
                    TokenAmount = 10000,
                    Price = 279.99m,
                    IsActive = true,
                    SortOrder = 4,
                    IsPopular = false,
                    BonusText = "%15 Bonus"
                },
                new TokenPackage
                {
                    Id = 5,
                    Name = "50000 Jeton",
                    Description = "Ultra paket - %20 bonus",
                    TokenAmount = 50000,
                    Price = 1199.99m,
                    IsActive = true,
                    SortOrder = 5,
                    IsPopular = false,
                    BonusText = "%20 Bonus"
                },
                new TokenPackage
                {
                    Id = 6,
                    Name = "100000 Jeton",
                    Description = "Maksimum paket - %25 bonus",
                    TokenAmount = 100000,
                    Price = 1999.99m,
                    IsActive = true,
                    SortOrder = 6,
                    IsPopular = false,
                    BonusText = "%25 Bonus"
                }
            );
        }
    }
} 