using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Enums;
using KisiselFinans.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KisiselFinans.Business.Services;

public class BudgetService
{
    private readonly IUnitOfWork _unitOfWork;

    public BudgetService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Budget>> GetUserBudgetsAsync(int userId)
        => await _unitOfWork.Budgets.Query()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId)
            .ToListAsync();

    public async Task<Budget?> GetByIdAsync(int id)
        => await _unitOfWork.Budgets.Query()
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Budget> CreateAsync(Budget budget)
    {
        await _unitOfWork.Budgets.AddAsync(budget);
        await _unitOfWork.SaveChangesAsync();
        return budget;
    }

    public async Task UpdateAsync(Budget budget)
    {
        _unitOfWork.Budgets.Update(budget);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var budget = await _unitOfWork.Budgets.GetByIdAsync(id);
        if (budget != null)
        {
            _unitOfWork.Budgets.Remove(budget);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<List<BudgetStatusDto>> GetBudgetStatusesAsync(int userId)
    {
        var budgets = await _unitOfWork.Budgets.Query()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.StartDate <= DateTime.Now && b.EndDate >= DateTime.Now)
            .ToListAsync();

        var result = new List<BudgetStatusDto>();

        foreach (var budget in budgets)
        {
            var spent = await _unitOfWork.Transactions.Query()
                .Where(t => t.Account.UserId == userId
                    && t.CategoryId == budget.CategoryId
                    && t.TransactionType == (byte)TransactionType.Expense
                    && t.TransactionDate >= budget.StartDate
                    && t.TransactionDate <= budget.EndDate)
                .SumAsync(t => t.Amount);

            result.Add(new BudgetStatusDto
            {
                CategoryName = budget.Category.CategoryName,
                Limit = budget.AmountLimit,
                Spent = spent,
                Remaining = budget.AmountLimit - spent,
                Percentage = budget.AmountLimit > 0 ? (double)(spent / budget.AmountLimit * 100) : 0,
                IsOverBudget = spent > budget.AmountLimit
            });
        }

        return result;
    }
}

