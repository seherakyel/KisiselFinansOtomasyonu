using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Enums;
using KisiselFinans.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KisiselFinans.Business.Services;

public class DashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AccountService _accountService;
    private readonly BudgetService _budgetService;
    private readonly ScheduledTransactionService _scheduledService;

    public DashboardService(IUnitOfWork unitOfWork, AccountService accountService, 
        BudgetService budgetService, ScheduledTransactionService scheduledService)
    {
        _unitOfWork = unitOfWork;
        _accountService = accountService;
        _budgetService = budgetService;
        _scheduledService = scheduledService;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int userId)
    {
        var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var summary = new DashboardSummaryDto
        {
            TotalIncome = await GetTotalIncomeAsync(userId, startOfMonth, endOfMonth),
            TotalExpense = await GetTotalExpenseAsync(userId, startOfMonth, endOfMonth),
            TotalAssets = await _accountService.GetTotalAssetsAsync(userId),
            TotalLiabilities = await _accountService.GetTotalLiabilitiesAsync(userId),
            CategorySpendings = await GetCategorySpendingsAsync(userId, startOfMonth, endOfMonth),
            MonthlyTrends = await GetMonthlyTrendsAsync(userId, 6),
            AccountBalances = await _accountService.GetAccountBalancesAsync(userId),
            BudgetStatuses = await _budgetService.GetBudgetStatusesAsync(userId),
            UpcomingTransactions = await _scheduledService.GetUpcomingTransactionsAsync(userId, 7)
        };

        summary.NetBalance = summary.TotalIncome - summary.TotalExpense;
        summary.NetWorth = summary.TotalAssets - summary.TotalLiabilities;

        return summary;
    }

    private async Task<decimal> GetTotalIncomeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _unitOfWork.Transactions.Query()
            .Where(t => t.Account.UserId == userId
                && t.TransactionType == (byte)TransactionType.Income
                && t.TransactionDate >= startDate
                && t.TransactionDate <= endDate)
            .SumAsync(t => t.Amount);
    }

    private async Task<decimal> GetTotalExpenseAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _unitOfWork.Transactions.Query()
            .Where(t => t.Account.UserId == userId
                && t.TransactionType == (byte)TransactionType.Expense
                && t.TransactionDate >= startDate
                && t.TransactionDate <= endDate)
            .SumAsync(t => t.Amount);
    }

    private async Task<List<CategorySpendingDto>> GetCategorySpendingsAsync(int userId, DateTime startDate, DateTime endDate)
    {
        var data = await _unitOfWork.Transactions.Query()
            .Include(t => t.Category)
            .Where(t => t.Account.UserId == userId
                && t.TransactionType == (byte)TransactionType.Expense
                && t.TransactionDate >= startDate
                && t.TransactionDate <= endDate
                && t.CategoryId != null)
            .GroupBy(t => t.Category!.CategoryName)
            .Select(g => new { CategoryName = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToListAsync();

        var total = data.Sum(d => d.Amount);
        return data.Select(d => new CategorySpendingDto
        {
            CategoryName = d.CategoryName,
            Amount = d.Amount,
            Percentage = total > 0 ? d.Amount / total * 100 : 0
        }).OrderByDescending(d => d.Amount).ToList();
    }

    private async Task<List<MonthlyTrendDto>> GetMonthlyTrendsAsync(int userId, int months)
    {
        var result = new List<MonthlyTrendDto>();
        var culture = new System.Globalization.CultureInfo("tr-TR");

        for (int i = months - 1; i >= 0; i--)
        {
            var date = DateTime.Now.AddMonths(-i);
            var start = new DateTime(date.Year, date.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);

            var income = await GetTotalIncomeAsync(userId, start, end);
            var expense = await GetTotalExpenseAsync(userId, start, end);

            result.Add(new MonthlyTrendDto
            {
                MonthName = culture.DateTimeFormat.GetMonthName(date.Month),
                Income = income,
                Expense = expense,
                Balance = income - expense
            });
        }

        return result;
    }

    public async Task<ForecastDto> GetForecastAsync(int userId)
    {
        var today = DateTime.Now;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        var daysRemaining = (endOfMonth - today).Days;
        var daysPassed = (today - startOfMonth).Days + 1;

        var currentExpense = await GetTotalExpenseAsync(userId, startOfMonth, today);
        var currentIncome = await GetTotalIncomeAsync(userId, startOfMonth, today);

        var avgDailySpending = daysPassed > 0 ? currentExpense / daysPassed : 0;
        var totalAssets = await _accountService.GetTotalAssetsAsync(userId);

        var upcomingIncome = await _unitOfWork.ScheduledTransactions.Query()
            .Include(s => s.Category)
            .Where(s => s.UserId == userId 
                && s.IsActive 
                && s.Category.Type == 1
                && s.NextExecutionDate <= endOfMonth)
            .SumAsync(s => s.Amount);

        var upcomingExpense = await _unitOfWork.ScheduledTransactions.Query()
            .Include(s => s.Category)
            .Where(s => s.UserId == userId 
                && s.IsActive 
                && s.Category.Type == 2
                && s.NextExecutionDate <= endOfMonth)
            .SumAsync(s => s.Amount);

        var projectedExpense = currentExpense + (avgDailySpending * daysRemaining);

        return new ForecastDto
        {
            CurrentBalance = totalAssets,
            AverageDailySpending = avgDailySpending,
            DaysRemaining = daysRemaining,
            ExpectedExpenses = projectedExpense + upcomingExpense,
            ExpectedIncome = currentIncome + upcomingIncome,
            ProjectedEndOfMonthBalance = totalAssets + upcomingIncome - upcomingExpense - (avgDailySpending * daysRemaining)
        };
    }
}

