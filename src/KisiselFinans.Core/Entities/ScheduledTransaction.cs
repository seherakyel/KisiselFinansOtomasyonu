using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KisiselFinans.Core.Entities;

[Table("ScheduledTransactions")]
public class ScheduledTransaction
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int AccountId { get; set; }

    public int CategoryId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    [Required, MaxLength(20)]
    public string FrequencyType { get; set; } = "Monthly"; // Daily, Weekly, Monthly, Yearly

    public int? DayOfMonth { get; set; }

    public DateTime NextExecutionDate { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(AccountId))]
    public virtual Account Account { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public virtual Category Category { get; set; } = null!;
}

