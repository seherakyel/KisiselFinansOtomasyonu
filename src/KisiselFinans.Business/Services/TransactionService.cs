using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Enums;
using KisiselFinans.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KisiselFinans.Business.Services;

public class TransactionService
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Transaction> AddTransactionAsync(CreateTransactionDto dto)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(dto.AccountId)
                ?? throw new InvalidOperationException("Hesap bulunamadı.");

            var transaction = new Transaction
            {
                AccountId = dto.AccountId,
                CategoryId = dto.CategoryId,
                TransactionDate = dto.TransactionDate,
                Amount = dto.Amount,
                TransactionType = dto.TransactionType,
                Description = dto.Description,
                CreatedAt = DateTime.Now
            };

            // Hesap bakiyesini güncelle
            if (dto.TransactionType == (byte)TransactionType.Income)
                account.CurrentBalance += dto.Amount;
            else if (dto.TransactionType == (byte)TransactionType.Expense)
                account.CurrentBalance -= dto.Amount;

            await _unitOfWork.Transactions.AddAsync(transaction);
            _unitOfWork.Accounts.Update(account);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return transaction;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<(Transaction from, Transaction to)> TransferAsync(TransferDto dto)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var fromAccount = await _unitOfWork.Accounts.GetByIdAsync(dto.FromAccountId)
                ?? throw new InvalidOperationException("Kaynak hesap bulunamadı.");
            var toAccount = await _unitOfWork.Accounts.GetByIdAsync(dto.ToAccountId)
                ?? throw new InvalidOperationException("Hedef hesap bulunamadı.");

            var fromTransaction = new Transaction
            {
                AccountId = dto.FromAccountId,
                TransactionDate = dto.TransactionDate,
                Amount = dto.Amount,
                TransactionType = (byte)TransactionType.Transfer,
                Description = $"Transfer: {toAccount.AccountName} hesabına",
                CreatedAt = DateTime.Now
            };

            var toTransaction = new Transaction
            {
                AccountId = dto.ToAccountId,
                TransactionDate = dto.TransactionDate,
                Amount = dto.Amount,
                TransactionType = (byte)TransactionType.Transfer,
                Description = $"Transfer: {fromAccount.AccountName} hesabından",
                CreatedAt = DateTime.Now
            };

            fromAccount.CurrentBalance -= dto.Amount;
            toAccount.CurrentBalance += dto.Amount;

            await _unitOfWork.Transactions.AddAsync(fromTransaction);
            await _unitOfWork.SaveChangesAsync();

            toTransaction.RelatedTransactionId = fromTransaction.Id;
            await _unitOfWork.Transactions.AddAsync(toTransaction);

            fromTransaction.RelatedTransactionId = toTransaction.Id;
            _unitOfWork.Transactions.Update(fromTransaction);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return (fromTransaction, toTransaction);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteTransactionAsync(int id)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(id)
                ?? throw new InvalidOperationException("İşlem bulunamadı.");
            var account = await _unitOfWork.Accounts.GetByIdAsync(transaction.AccountId)!;

            // Bakiyeyi geri al
            if (transaction.TransactionType == (byte)TransactionType.Income)
                account!.CurrentBalance -= transaction.Amount;
            else if (transaction.TransactionType == (byte)TransactionType.Expense)
                account!.CurrentBalance += transaction.Amount;

            _unitOfWork.Transactions.Remove(transaction);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _unitOfWork.Transactions.Query()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.Account.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.TransactionDate <= endDate.Value);

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                AccountId = t.AccountId,
                AccountName = t.Account.AccountName,
                CategoryId = t.CategoryId,
                CategoryName = t.Category != null ? t.Category.CategoryName : null,
                TransactionDate = t.TransactionDate,
                Amount = t.Amount,
                TransactionType = t.TransactionType,
                TransactionTypeName = t.TransactionType == 1 ? "Gelir" : t.TransactionType == 2 ? "Gider" : "Transfer",
                Description = t.Description,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<decimal> GetTotalIncomeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _unitOfWork.Transactions.Query()
            .Where(t => t.Account.UserId == userId 
                && t.TransactionType == (byte)TransactionType.Income
                && t.TransactionDate >= startDate 
                && t.TransactionDate <= endDate)
            .SumAsync(t => t.Amount);
    }

    public async Task<decimal> GetTotalExpenseAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _unitOfWork.Transactions.Query()
            .Where(t => t.Account.UserId == userId 
                && t.TransactionType == (byte)TransactionType.Expense
                && t.TransactionDate >= startDate 
                && t.TransactionDate <= endDate)
            .SumAsync(t => t.Amount);
    }
}

