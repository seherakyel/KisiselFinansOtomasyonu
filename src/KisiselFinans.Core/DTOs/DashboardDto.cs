namespace KisiselFinans.Core.DTOs;

public class DashboardSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetBalance { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal NetWorth { get; set; }
    public List<CategorySpendingDto> CategorySpendings { get; set; } = new();
    public List<MonthlyTrendDto> MonthlyTrends { get; set; } = new();
    public List<AccountBalanceDto> AccountBalances { get; set; } = new();
    public List<BudgetStatusDto> BudgetStatuses { get; set; } = new();
    public List<UpcomingTransactionDto> UpcomingTransactions { get; set; } = new();
}

public class CategorySpendingDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class MonthlyTrendDto
{
    public string MonthName { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance { get; set; }
}

public class AccountBalanceDto
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
}

public class BudgetStatusDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public double Percentage { get; set; }
    public bool IsOverBudget { get; set; }
}

public class UpcomingTransactionDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}

public class ForecastDto
{
    public decimal CurrentBalance { get; set; }
    public decimal ProjectedEndOfMonthBalance { get; set; }
    public decimal AverageDailySpending { get; set; }
    public int DaysRemaining { get; set; }
    public decimal ExpectedExpenses { get; set; }
    public decimal ExpectedIncome { get; set; }
}

