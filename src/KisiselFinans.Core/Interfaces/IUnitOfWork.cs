using KisiselFinans.Core.Entities;

namespace KisiselFinans.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Account> Accounts { get; }
    IRepository<AccountType> AccountTypes { get; }
    IRepository<Category> Categories { get; }
    IRepository<Transaction> Transactions { get; }
    IRepository<ScheduledTransaction> ScheduledTransactions { get; }
    IRepository<Budget> Budgets { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

