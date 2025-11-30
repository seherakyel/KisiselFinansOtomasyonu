using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KisiselFinans.Core.Entities;

[Table("Budgets")]
public class Budget
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CategoryId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountLimit { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public virtual Category Category { get; set; } = null!;
}

