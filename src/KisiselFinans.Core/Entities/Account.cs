using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KisiselFinans.Core.Entities;

[Table("Accounts")]
public class Account
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int AccountTypeId { get; set; }

    [Required, MaxLength(100)]
    public string AccountName { get; set; } = string.Empty;

    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "TRY";

    [Column(TypeName = "decimal(18,2)")]
    public decimal InitialBalance { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentBalance { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditLimit { get; set; } = 0;

    public int CutoffDay { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(AccountTypeId))]
    public virtual AccountType AccountType { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<ScheduledTransaction> ScheduledTransactions { get; set; } = new List<ScheduledTransaction>();
}

