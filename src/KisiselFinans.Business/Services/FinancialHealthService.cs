using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Interfaces;

namespace KisiselFinans.Business.Services;

/// <summary>
/// Finansal Sağlık Skoru Hesaplama Servisi ⭐
/// </summary>
public class FinancialHealthService
{
    private readonly IUnitOfWork _unitOfWork;

    public FinancialHealthService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Kullanıcının finansal sağlık skorunu hesaplar (0-100)
    /// </summary>
    public async Task<FinancialHealthDto> CalculateHealthScoreAsync(int userId)
    {
        var result = new FinancialHealthDto();
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var last30Days = now.AddDays(-30);

        // Kullanıcının hesaplarını al
        var accounts = await _unitOfWork.Accounts.FindAsync(a => a.UserId == userId && a.IsActive);
        var accountIds = accounts.Select(a => a.Id).ToList();

        // Son 30 günlük gelir
        var income = await _unitOfWork.Transactions.FindAsync(t =>
            accountIds.Contains(t.AccountId) &&
            t.TransactionType == 1 &&
            t.TransactionDate >= last30Days);
        result.TotalIncome = income.Sum(t => t.Amount);

        // Son 30 günlük gider
        var expenses = await _unitOfWork.Transactions.FindAsync(t =>
            accountIds.Contains(t.AccountId) &&
            t.TransactionType == 2 &&
            t.TransactionDate >= last30Days);
        result.TotalExpense = expenses.Sum(t => t.Amount);

        // Net tasarruf
        result.NetSavings = result.TotalIncome - result.TotalExpense;

        // 1. Gelir/Gider Oranı Skoru (max 40 puan)
        decimal incomeExpenseScore = 0;
        if (result.TotalIncome > 0)
        {
            result.IncomeExpenseRatio = ((result.TotalIncome - result.TotalExpense) / result.TotalIncome) * 100;
            incomeExpenseScore = Math.Min(40, Math.Max(0, result.IncomeExpenseRatio * 0.4m));
        }

        // 2. Tasarruf Oranı Skoru (max 30 puan)
        decimal savingsScore = 0;
        if (result.TotalIncome > 0)
        {
            result.SavingsRate = (result.NetSavings / result.TotalIncome) * 100;
            savingsScore = Math.Min(30, Math.Max(0, result.SavingsRate * 0.3m));
        }

        // 3. Bütçe Uyumu Skoru (max 30 puan)
        decimal budgetScore = 20; // Varsayılan orta puan
        var budgets = await _unitOfWork.Budgets.FindAsync(b =>
            b.UserId == userId &&
            b.StartDate <= now &&
            b.EndDate >= now);

        if (budgets.Any())
        {
            decimal totalLimit = budgets.Sum(b => b.AmountLimit);
            decimal totalSpent = 0;

            foreach (var budget in budgets)
            {
                var categoryExpenses = expenses.Where(e => e.CategoryId == budget.CategoryId);
                totalSpent += categoryExpenses.Sum(e => e.Amount);
            }

            if (totalLimit > 0)
            {
                result.BudgetAdherence = Math.Max(0, (1 - (totalSpent / totalLimit)) * 100);
                budgetScore = Math.Min(30, Math.Max(0, result.BudgetAdherence * 0.3m));
            }
        }
        else
        {
            result.BudgetAdherence = 100; // Bütçe yoksa tam puan
        }

        // Toplam skor
        result.Score = (int)Math.Round(incomeExpenseScore + savingsScore + budgetScore);
        result.Score = Math.Max(0, Math.Min(100, result.Score));

        // Not ve renk belirle
        (result.Grade, result.GradeDescription, result.GradeColor) = GetGradeInfo(result.Score);

        // Öneriler oluştur
        result.Recommendations = GenerateRecommendations(result);

        // Geçmiş skorları al
        var history = await _unitOfWork.Repository<FinancialHealthHistory>()
            .FindAsync(h => h.UserId == userId);
        result.History = history
            .OrderByDescending(h => h.CalculatedAt)
            .Take(12)
            .Select(h => new HealthScoreHistory { Date = h.CalculatedAt, Score = h.Score })
            .Reverse()
            .ToList();

        // Skoru kaydet
        await SaveHealthScore(userId, result);

        return result;
    }

    private (string grade, string description, string color) GetGradeInfo(int score)
    {
        return score switch
        {
            >= 90 => ("A+", "Mükemmel! Finansal sağlığınız çok iyi.", "#10B981"),
            >= 80 => ("A", "Harika! Finansal durumunuz oldukça sağlıklı.", "#34D399"),
            >= 70 => ("B+", "İyi gidiyorsunuz! Küçük iyileştirmeler yapabilirsiniz.", "#60A5FA"),
            >= 60 => ("B", "Ortalama üstü. Biraz daha tasarruf düşünebilirsiniz.", "#818CF8"),
            >= 50 => ("C+", "Ortalama. İyileştirme alanları mevcut.", "#FBBF24"),
            >= 40 => ("C", "Dikkat! Harcamalarınızı gözden geçirin.", "#F97316"),
            >= 30 => ("D", "Uyarı! Bütçe planlaması yapmanız önerilir.", "#FB7185"),
            _ => ("F", "Kritik! Acil önlem almanız gerekiyor.", "#EF4444")
        };
    }

    private List<string> GenerateRecommendations(FinancialHealthDto health)
    {
        var recommendations = new List<string>();

        // Gelir/Gider oranı önerileri
        if (health.IncomeExpenseRatio < 10)
            recommendations.Add("💡 Harcamalarınız gelirinize çok yakın. Gereksiz giderleri azaltmayı deneyin.");
        else if (health.IncomeExpenseRatio < 20)
            recommendations.Add("💰 Tasarruf oranınızı artırmak için küçük harcamaları gözden geçirin.");

        // Tasarruf önerileri
        if (health.SavingsRate < 10)
            recommendations.Add("🎯 Gelirinizin en az %10'unu biriktirmeyi hedefleyin.");
        else if (health.SavingsRate >= 20)
            recommendations.Add("🌟 Harika tasarruf oranı! Bu parayı yatırıma yönlendirmeyi düşünün.");

        // Bütçe önerileri
        if (health.BudgetAdherence < 50)
            recommendations.Add("⚠️ Bütçe limitlerini aşıyorsunuz. Harcama alışkanlıklarınızı gözden geçirin.");
        else if (health.BudgetAdherence < 80)
            recommendations.Add("📊 Bütçe takibi yapıyorsunuz ama bazı kategorilerde dikkatli olun.");

        // Genel öneriler
        if (health.Score >= 70)
            recommendations.Add("✨ Acil durum fonu oluşturmayı veya yatırım yapmayı düşünün.");

        if (!recommendations.Any())
            recommendations.Add("🎉 Tebrikler! Finansal sağlığınız gayet iyi görünüyor.");

        return recommendations;
    }

    private async Task SaveHealthScore(int userId, FinancialHealthDto health)
    {
        var historyEntry = new FinancialHealthHistory
        {
            UserId = userId,
            Score = health.Score,
            IncomeExpenseRatio = health.IncomeExpenseRatio,
            SavingsRate = health.SavingsRate,
            BudgetAdherence = health.BudgetAdherence,
            CalculatedAt = DateTime.Now
        };

        await _unitOfWork.Repository<FinancialHealthHistory>().AddAsync(historyEntry);
        await _unitOfWork.SaveChangesAsync();
    }
}

