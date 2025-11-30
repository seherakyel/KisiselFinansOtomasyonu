namespace KisiselFinans.Core.DTOs;

public class TransactionDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public byte TransactionType { get; set; }
    public string TransactionTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTransactionDto
{
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public byte TransactionType { get; set; }
    public string? Description { get; set; }
}

public class TransferDto
{
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
}

