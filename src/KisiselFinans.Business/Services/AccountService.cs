using KisiselFinans.Core.DTOs;
using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KisiselFinans.Business.Services;

public class AccountService
{
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Account>> GetUserAccountsAsync(int userId)
        => await _unitOfWork.Accounts
            .Query()
            .Include(a => a.AccountType)
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync();

    public async Task<Account?> GetByIdAsync(int id)
        => await _unitOfWork.Accounts
            .Query()
            .Include(a => a.AccountType)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Account> CreateAsync(Account account)
    {
        account.CurrentBalance = account.InitialBalance;
        await _unitOfWork.Accounts.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(Account account)
    {
        _unitOfWork.Accounts.Update(account);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id);
        if (account != null)
        {
            account.IsActive = false;
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<AccountType>> GetAccountTypesAsync()
        => await _unitOfWork.AccountTypes.GetAllAsync();

    public async Task<List<AccountBalanceDto>> GetAccountBalancesAsync(int userId)
    {
        return await _unitOfWork.Accounts
            .Query()
            .Include(a => a.AccountType)
            .Where(a => a.UserId == userId && a.IsActive)
            .Select(a => new AccountBalanceDto
            {
                AccountName = a.AccountName,
                AccountType = a.AccountType.TypeName,
                Balance = a.CurrentBalance,
                CurrencyCode = a.CurrencyCode
            })
            .ToListAsync();
    }

    public async Task<decimal> GetTotalAssetsAsync(int userId)
    {
        return await _unitOfWork.Accounts
            .Query()
            .Where(a => a.UserId == userId && a.IsActive && a.AccountTypeId != 3)
            .SumAsync(a => a.CurrentBalance);
    }

    public async Task<decimal> GetTotalLiabilitiesAsync(int userId)
    {
        return await _unitOfWork.Accounts
            .Query()
            .Where(a => a.UserId == userId && a.IsActive && a.AccountTypeId == 3 && a.CurrentBalance < 0)
            .SumAsync(a => Math.Abs(a.CurrentBalance));
    }
}

