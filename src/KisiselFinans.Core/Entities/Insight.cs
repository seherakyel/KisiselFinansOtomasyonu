using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KisiselFinans.Core.Entities;

[Table("Insights")]
public class Insight
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required, MaxLength(50)]
    public string InsightType { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Severity { get; set; } = "INFO";

    public int? RelatedCategoryId { get; set; }

    public decimal? RelatedAmount { get; set; }

    public decimal? PercentageChange { get; set; }

    public bool IsRead { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ExpiresAt { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RelatedCategoryId")]
    public virtual Category? RelatedCategory { get; set; }
}

// Insight türleri
public static class InsightTypes
{
    public const string SpendingIncrease = "SPENDING_INCREASE";
    public const string CategorySpike = "CATEGORY_SPIKE";
    public const string BudgetWarning = "BUDGET_WARNING";
    public const string SavingTip = "SAVING_TIP";
    public const string AchievementUnlocked = "ACHIEVEMENT_UNLOCKED";
    public const string GoalProgress = "GOAL_PROGRESS";
}

// Severity türleri
public static class InsightSeverity
{
    public const string Info = "INFO";
    public const string Warning = "WARNING";
    public const string Alert = "ALERT";
    public const string Success = "SUCCESS";
}

