using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KisiselFinans.Core.Entities;

[Table("FinancialHealthHistory")]
public class FinancialHealthHistory
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Score { get; set; }

    public decimal? IncomeExpenseRatio { get; set; }

    public decimal? SavingsRate { get; set; }

    public decimal? BudgetAdherence { get; set; }

    public decimal? DebtRatio { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.Now;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

