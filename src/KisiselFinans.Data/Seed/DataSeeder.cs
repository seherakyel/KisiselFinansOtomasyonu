using KisiselFinans.Core.Entities;
using KisiselFinans.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace KisiselFinans.Data.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(FinansDbContext context)
    {
        if (!await context.AccountTypes.AnyAsync())
        {
            context.AccountTypes.AddRange(
                new AccountType { Id = 1, TypeName = "Nakit" },
                new AccountType { Id = 2, TypeName = "Vadesiz Mevduat" },
                new AccountType { Id = 3, TypeName = "Kredi Kartı" },
                new AccountType { Id = 4, TypeName = "Yatırım Hesabı (Altın/Döviz)" }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.Categories.AnyAsync())
        {
            var incomeCategories = new List<Category>
            {
                new() { CategoryName = "Maaş", Type = 1, IconIndex = 1 },
                new() { CategoryName = "Ek Gelir", Type = 1, IconIndex = 2 },
                new() { CategoryName = "Yatırım Geliri", Type = 1, IconIndex = 3 },
                new() { CategoryName = "Kira Geliri", Type = 1, IconIndex = 4 },
                new() { CategoryName = "Hediye", Type = 1, IconIndex = 5 }
            };

            var expenseCategories = new List<Category>
            {
                new() { CategoryName = "Market", Type = 2, IconIndex = 10 },
                new() { CategoryName = "Kira", Type = 2, IconIndex = 11 },
                new() { CategoryName = "Faturalar", Type = 2, IconIndex = 12 },
                new() { CategoryName = "Ulaşım", Type = 2, IconIndex = 13 },
                new() { CategoryName = "Sağlık", Type = 2, IconIndex = 14 },
                new() { CategoryName = "Eğitim", Type = 2, IconIndex = 15 },
                new() { CategoryName = "Eğlence", Type = 2, IconIndex = 16 },
                new() { CategoryName = "Giyim", Type = 2, IconIndex = 17 },
                new() { CategoryName = "Yemek", Type = 2, IconIndex = 18 },
                new() { CategoryName = "Diğer", Type = 2, IconIndex = 19 }
            };

            context.Categories.AddRange(incomeCategories);
            context.Categories.AddRange(expenseCategories);
            await context.SaveChangesAsync();
        }
    }
}

