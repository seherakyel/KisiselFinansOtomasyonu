using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KisiselFinans.Core.Entities;

[Table("Transactions")]
public class Transaction
{
    [Key]
    public int Id { get; set; }

    public int AccountId { get; set; }

    public int? CategoryId { get; set; }

    public int? RelatedTransactionId { get; set; }

    public DateTime TransactionDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public byte TransactionType { get; set; } // 1: Gelir, 2: Gider, 3: Transfer

    [MaxLength(255)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(AccountId))]
    public virtual Account Account { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }

    [ForeignKey(nameof(RelatedTransactionId))]
    public virtual Transaction? RelatedTransaction { get; set; }
}

