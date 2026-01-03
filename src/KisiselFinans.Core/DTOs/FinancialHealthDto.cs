namespace KisiselFinans.Core.DTOs;

/// <summary>
/// Finansal Sağlık Skoru DTO
/// </summary>
public class FinancialHealthDto
{
    public int Score { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string GradeDescription { get; set; } = string.Empty;
    public string GradeColor { get; set; } = string.Empty;
    
    // Detay skorları
    public decimal IncomeExpenseRatio { get; set; }
    public decimal SavingsRate { get; set; }
    public decimal BudgetAdherence { get; set; }
    
    // Özet veriler
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetSavings { get; set; }
    
    // Öneriler
    public List<string> Recommendations { get; set; } = new();
    
    // Geçmiş skorlar (trend için)
    public List<HealthScoreHistory> History { get; set; } = new();
}

public class HealthScoreHistory
{
    public DateTime Date { get; set; }
    public int Score { get; set; }
}

/// <summary>
/// Insight (İçgörü) DTO
/// </summary>
public class InsightDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public decimal? PercentageChange { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// CSV Import sonucu
/// </summary>
public class CsvImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int DuplicateCount { get; set; }
    public decimal TotalImportedAmount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ImportedTransactionDto> ImportedTransactions { get; set; } = new();
}

public class ImportedTransactionDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string SuggestedCategory { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public bool IsIncome { get; set; }
}

/// <summary>
/// Bütçe Uyarısı DTO
/// </summary>
public class BudgetAlertDto
{
    public int BudgetId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public decimal Percentage { get; set; }
    public string AlertLevel { get; set; } = string.Empty; // Normal, Warning, Critical
    public string Message { get; set; } = string.Empty;
}

