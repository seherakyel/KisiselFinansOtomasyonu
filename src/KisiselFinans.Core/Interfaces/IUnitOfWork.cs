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
    
    // Yeni entity'ler için ⭐
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<FinancialHealthHistory> FinancialHealthHistories { get; }
    IRepository<Insight> Insights { get; }
    
    // Generic repository erişimi
    IRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
