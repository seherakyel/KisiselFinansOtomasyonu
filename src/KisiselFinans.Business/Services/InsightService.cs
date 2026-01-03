using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Interfaces;

namespace KisiselFinans.Business.Services;

/// <summary>
/// Akıllı İçgörü (Insights) Servisi ⭐
/// </summary>
public class InsightService
{
    private readonly IUnitOfWork _unitOfWork;

    public InsightService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Kullanıcı için içgörüler oluşturur
    /// </summary>
    public async Task GenerateInsightsAsync(int userId)
    {
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth.AddDays(-1);

        // Kullanıcının hesaplarını al
        var accounts = await _unitOfWork.Accounts.FindAsync(a => a.UserId == userId && a.IsActive);
        var accountIds = accounts.Select(a => a.Id).ToList();

        // Bu ay ve geçen ay işlemleri
        var currentMonthTx = await _unitOfWork.Transactions.FindAsync(t =>
            accountIds.Contains(t.AccountId) &&
            t.TransactionDate >= startOfMonth);

        var lastMonthTx = await _unitOfWork.Transactions.FindAsync(t =>
            accountIds.Contains(t.AccountId) &&
            t.TransactionDate >= startOfLastMonth &&
            t.TransactionDate <= endOfLastMonth);

        // Mevcut içgörüleri temizle (7 günden eski olanları)
        var oldInsights = await _unitOfWork.Repository<Insight>()
            .FindAsync(i => i.UserId == userId && i.CreatedAt < now.AddDays(-7));
        foreach (var insight in oldInsights)
        {
            _unitOfWork.Repository<Insight>().Delete(insight);
        }

        // 1. Genel harcama artışı kontrolü
        var currentExpense = currentMonthTx.Where(t => t.TransactionType == 2).Sum(t => t.Amount);
        var lastExpense = lastMonthTx.Where(t => t.TransactionType == 2).Sum(t => t.Amount);

        if (lastExpense > 0 && currentExpense > lastExpense * 1.2m)
        {
            var changePercent = ((currentExpense - lastExpense) / lastExpense) * 100;
            await CreateInsightAsync(userId, new InsightDto
            {
                Type = InsightTypes.SpendingIncrease,
                Title = $"Harcamalarınız %{changePercent:F0} arttı! 📈",
                Description = $"Bu ay toplam ₺{currentExpense:N2} harcadınız. Geçen aya göre ₺{currentExpense - lastExpense:N2} daha fazla.",
                Severity = changePercent > 50 ? InsightSeverity.Alert : InsightSeverity.Warning,
                Amount = currentExpense,
                PercentageChange = changePercent
            });
        }

        // 2. Kategori bazlı analiz
        var categories = await _unitOfWork.Categories.FindAsync(c => c.UserId == null || c.UserId == userId);
        
        foreach (var category in categories.Where(c => c.Type == 2)) // Sadece gider kategorileri
        {
            var currentCatExpense = currentMonthTx
                .Where(t => t.TransactionType == 2 && t.CategoryId == category.Id)
                .Sum(t => t.Amount);
            
            var lastCatExpense = lastMonthTx
                .Where(t => t.TransactionType == 2 && t.CategoryId == category.Id)
                .Sum(t => t.Amount);

            // %50'den fazla artış varsa uyar
            if (lastCatExpense > 100 && currentCatExpense > lastCatExpense * 1.5m)
            {
                var changePercent = ((currentCatExpense - lastCatExpense) / lastCatExpense) * 100;
                await CreateInsightAsync(userId, new InsightDto
                {
                    Type = InsightTypes.CategorySpike,
                    Title = $"{category.CategoryName} harcaması %{changePercent:F0} arttı! ⚠️",
                    Description = $"Bu ay {category.CategoryName} kategorisinde ₺{currentCatExpense:N2} harcadınız.",
                    Severity = InsightSeverity.Alert,
                    CategoryName = category.CategoryName,
                    Amount = currentCatExpense,
                    PercentageChange = changePercent
                });
            }
        }

        // 3. Bütçe uyarıları
        await CheckBudgetAlertsAsync(userId, currentMonthTx);

        // 4. Pozitif içgörüler
        if (currentExpense < lastExpense * 0.9m && lastExpense > 0)
        {
            var savedPercent = ((lastExpense - currentExpense) / lastExpense) * 100;
            await CreateInsightAsync(userId, new InsightDto
            {
                Type = InsightTypes.SavingTip,
                Title = $"Harika! %{savedPercent:F0} daha az harcadınız 🎉",
                Description = $"Geçen aya göre ₺{lastExpense - currentExpense:N2} tasarruf ettiniz. Böyle devam!",
                Severity = InsightSeverity.Success,
                Amount = lastExpense - currentExpense,
                PercentageChange = -savedPercent
            });
        }

        // 5. Tasarruf ipucu
        if (currentExpense > 0)
        {
            var dailyAvg = currentExpense / now.Day;
            await CreateInsightAsync(userId, new InsightDto
            {
                Type = InsightTypes.SavingTip,
                Title = "Günlük Harcama Özeti 💡",
                Description = $"Günlük ortalama ₺{dailyAvg:N2} harcıyorsunuz. Ay sonuna kadar yaklaşık ₺{dailyAvg * (DateTime.DaysInMonth(now.Year, now.Month) - now.Day):N2} daha harcayabilirsiniz.",
                Severity = InsightSeverity.Info,
                Amount = dailyAvg
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Bütçe uyarılarını kontrol eder
    /// </summary>
    private async Task CheckBudgetAlertsAsync(int userId, IEnumerable<Transaction> currentMonthTx)
    {
        var budgets = await _unitOfWork.Budgets.FindAsync(b =>
            b.UserId == userId &&
            b.StartDate <= DateTime.Now &&
            b.EndDate >= DateTime.Now);

        foreach (var budget in budgets)
        {
            var spent = currentMonthTx
                .Where(t => t.TransactionType == 2 && t.CategoryId == budget.CategoryId)
                .Sum(t => t.Amount);

            var percentage = (spent / budget.AmountLimit) * 100;
            var category = await _unitOfWork.Categories.GetByIdAsync(budget.CategoryId);

            if (percentage >= 100)
            {
                await CreateInsightAsync(userId, new InsightDto
                {
                    Type = InsightTypes.BudgetWarning,
                    Title = $"{category?.CategoryName} bütçesi aşıldı! 🚨",
                    Description = $"₺{budget.AmountLimit:N2} limitinizi ₺{spent - budget.AmountLimit:N2} aştınız.",
                    Severity = InsightSeverity.Alert,
                    CategoryName = category?.CategoryName ?? "",
                    Amount = spent,
                    PercentageChange = percentage
                });
            }
            else if (percentage >= 80)
            {
                await CreateInsightAsync(userId, new InsightDto
                {
                    Type = InsightTypes.BudgetWarning,
                    Title = $"{category?.CategoryName} bütçesi dolmak üzere! ⚠️",
                    Description = $"₺{budget.AmountLimit:N2} limitinizin %{percentage:F0}'ini kullandınız. ₺{budget.AmountLimit - spent:N2} kaldı.",
                    Severity = InsightSeverity.Warning,
                    CategoryName = category?.CategoryName ?? "",
                    Amount = spent,
                    PercentageChange = percentage
                });
            }
        }
    }

    /// <summary>
    /// Kullanıcının aktif içgörülerini getirir
    /// </summary>
    public async Task<List<InsightDto>> GetUserInsightsAsync(int userId)
    {
        var insights = await _unitOfWork.Repository<Insight>()
            .FindAsync(i => i.UserId == userId && i.IsActive);

        return insights
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InsightDto
            {
                Id = i.Id,
                Type = i.InsightType,
                Title = i.Title,
                Description = i.Description,
                Severity = i.Severity,
                Icon = GetInsightIcon(i.InsightType, i.Severity),
                CategoryName = i.RelatedCategory?.CategoryName ?? "",
                Amount = i.RelatedAmount,
                PercentageChange = i.PercentageChange,
                IsRead = i.IsRead,
                CreatedAt = i.CreatedAt
            })
            .ToList();
    }

    /// <summary>
    /// İçgörüyü okundu olarak işaretler
    /// </summary>
    public async Task MarkAsReadAsync(int insightId)
    {
        var insight = await _unitOfWork.Repository<Insight>().GetByIdAsync(insightId);
        if (insight != null)
        {
            insight.IsRead = true;
            await _unitOfWork.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Bütçe uyarılarını getirir
    /// </summary>
    public async Task<List<BudgetAlertDto>> GetBudgetAlertsAsync(int userId)
    {
        var alerts = new List<BudgetAlertDto>();
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var budgets = await _unitOfWork.Budgets.FindAsync(b =>
            b.UserId == userId &&
            b.StartDate <= now &&
            b.EndDate >= now);

        var accounts = await _unitOfWork.Accounts.FindAsync(a => a.UserId == userId && a.IsActive);
        var accountIds = accounts.Select(a => a.Id).ToList();

        var transactions = await _unitOfWork.Transactions.FindAsync(t =>
            accountIds.Contains(t.AccountId) &&
            t.TransactionType == 2 &&
            t.TransactionDate >= startOfMonth);

        foreach (var budget in budgets)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(budget.CategoryId);
            var spent = transactions.Where(t => t.CategoryId == budget.CategoryId).Sum(t => t.Amount);
            var percentage = budget.AmountLimit > 0 ? (spent / budget.AmountLimit) * 100 : 0;

            alerts.Add(new BudgetAlertDto
            {
                BudgetId = budget.Id,
                CategoryName = category?.CategoryName ?? "Bilinmeyen",
                Limit = budget.AmountLimit,
                Spent = spent,
                Remaining = Math.Max(0, budget.AmountLimit - spent),
                Percentage = percentage,
                AlertLevel = percentage >= 100 ? "Critical" : percentage >= 80 ? "Warning" : "Normal",
                Message = percentage >= 100
                    ? $"Bütçe aşıldı! ₺{spent - budget.AmountLimit:N2} fazla harcandı."
                    : percentage >= 80
                        ? $"Dikkat! Bütçenin %{percentage:F0}'i kullanıldı."
                        : $"₺{budget.AmountLimit - spent:N2} kalan bütçe."
            });
        }

        return alerts.OrderByDescending(a => a.Percentage).ToList();
    }

    private async Task CreateInsightAsync(int userId, InsightDto dto)
    {
        // Aynı tip içgörü son 24 saatte oluşturulduysa tekrar oluşturma
        var existing = await _unitOfWork.Repository<Insight>().FirstOrDefaultAsync(i =>
            i.UserId == userId &&
            i.InsightType == dto.Type &&
            i.CreatedAt >= DateTime.Now.AddHours(-24));

        if (existing != null) return;

        var insight = new Insight
        {
            UserId = userId,
            InsightType = dto.Type,
            Title = dto.Title,
            Description = dto.Description,
            Severity = dto.Severity,
            RelatedAmount = dto.Amount,
            PercentageChange = dto.PercentageChange,
            IsRead = false,
            IsActive = true,
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddDays(7)
        };

        await _unitOfWork.Repository<Insight>().AddAsync(insight);
    }

    private string GetInsightIcon(string type, string severity)
    {
        return type switch
        {
            InsightTypes.SpendingIncrease => "📈",
            InsightTypes.CategorySpike => "⚠️",
            InsightTypes.BudgetWarning => severity == InsightSeverity.Alert ? "🚨" : "⚠️",
            InsightTypes.SavingTip => "💡",
            InsightTypes.AchievementUnlocked => "🏆",
            InsightTypes.GoalProgress => "🎯",
            _ => "ℹ️"
        };
    }
}

