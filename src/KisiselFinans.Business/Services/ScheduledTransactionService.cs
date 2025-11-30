using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Enums;
using KisiselFinans.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KisiselFinans.Business.Services;

public class ScheduledTransactionService
{
    private readonly IUnitOfWork _unitOfWork;

    public ScheduledTransactionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ScheduledTransaction>> GetUserScheduledTransactionsAsync(int userId)
        => await _unitOfWork.ScheduledTransactions.Query()
            .Include(s => s.Account)
            .Include(s => s.Category)
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.NextExecutionDate)
            .ToListAsync();

    public async Task<ScheduledTransaction?> GetByIdAsync(int id)
        => await _unitOfWork.ScheduledTransactions.Query()
            .Include(s => s.Account)
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<ScheduledTransaction> CreateAsync(ScheduledTransaction scheduled)
    {
        await _unitOfWork.ScheduledTransactions.AddAsync(scheduled);
        await _unitOfWork.SaveChangesAsync();
        return scheduled;
    }

    public async Task UpdateAsync(ScheduledTransaction scheduled)
    {
        _unitOfWork.ScheduledTransactions.Update(scheduled);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var scheduled = await _unitOfWork.ScheduledTransactions.GetByIdAsync(id);
        if (scheduled != null)
        {
            scheduled.IsActive = false;
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<List<UpcomingTransactionDto>> GetUpcomingTransactionsAsync(int userId, int days)
    {
        var endDate = DateTime.Now.AddDays(days);
        
        return await _unitOfWork.ScheduledTransactions.Query()
            .Include(s => s.Account)
            .Include(s => s.Category)
            .Where(s => s.UserId == userId && s.IsActive && s.NextExecutionDate <= endDate)
            .OrderBy(s => s.NextExecutionDate)
            .Select(s => new UpcomingTransactionDto
            {
                Id = s.Id,
                Description = s.Description ?? s.Category.CategoryName,
                Amount = s.Amount,
                DueDate = s.NextExecutionDate,
                CategoryName = s.Category.CategoryName,
                AccountName = s.Account.AccountName
            })
            .ToListAsync();
    }

    public async Task ExecuteScheduledTransactionsAsync()
    {
        var dueTransactions = await _unitOfWork.ScheduledTransactions.Query()
            .Include(s => s.Account)
            .Include(s => s.Category)
            .Where(s => s.IsActive && s.NextExecutionDate <= DateTime.Now)
            .ToListAsync();

        foreach (var scheduled in dueTransactions)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var transaction = new Transaction
                {
                    AccountId = scheduled.AccountId,
                    CategoryId = scheduled.CategoryId,
                    TransactionDate = scheduled.NextExecutionDate,
                    Amount = scheduled.Amount,
                    TransactionType = scheduled.Category.Type,
                    Description = scheduled.Description,
                    CreatedAt = DateTime.Now
                };

                if (scheduled.Category.Type == (byte)TransactionType.Income)
                    scheduled.Account.CurrentBalance += scheduled.Amount;
                else
                    scheduled.Account.CurrentBalance -= scheduled.Amount;

                await _unitOfWork.Transactions.AddAsync(transaction);

                // Sonraki tarihi hesapla
                scheduled.NextExecutionDate = CalculateNextDate(scheduled);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }

    private static DateTime CalculateNextDate(ScheduledTransaction scheduled)
    {
        return scheduled.FrequencyType switch
        {
            "Daily" => scheduled.NextExecutionDate.AddDays(1),
            "Weekly" => scheduled.NextExecutionDate.AddDays(7),
            "Monthly" => scheduled.DayOfMonth.HasValue
                ? new DateTime(scheduled.NextExecutionDate.AddMonths(1).Year, 
                    scheduled.NextExecutionDate.AddMonths(1).Month, 
                    Math.Min(scheduled.DayOfMonth.Value, DateTime.DaysInMonth(
                        scheduled.NextExecutionDate.AddMonths(1).Year, 
                        scheduled.NextExecutionDate.AddMonths(1).Month)))
                : scheduled.NextExecutionDate.AddMonths(1),
            "Yearly" => scheduled.NextExecutionDate.AddYears(1),
            _ => scheduled.NextExecutionDate.AddMonths(1)
        };
    }
}

