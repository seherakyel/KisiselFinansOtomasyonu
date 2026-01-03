using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Interfaces;
using KisiselFinans.Data.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace KisiselFinans.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly FinansDbContext _context;
    private IDbContextTransaction? _transaction;
    private readonly Dictionary<Type, object> _repositories = new();

    private IRepository<User>? _users;
    private IRepository<Account>? _accounts;
    private IRepository<AccountType>? _accountTypes;
    private IRepository<Category>? _categories;
    private IRepository<Transaction>? _transactions;
    private IRepository<ScheduledTransaction>? _scheduledTransactions;
    private IRepository<Budget>? _budgets;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<FinancialHealthHistory>? _financialHealthHistories;
    private IRepository<Insight>? _insights;
    private IRepository<SavingsGoal>? _savingsGoals;

    public UnitOfWork(FinansDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Account> Accounts => _accounts ??= new Repository<Account>(_context);
    public IRepository<AccountType> AccountTypes => _accountTypes ??= new Repository<AccountType>(_context);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
    public IRepository<Transaction> Transactions => _transactions ??= new Repository<Transaction>(_context);
    public IRepository<ScheduledTransaction> ScheduledTransactions => _scheduledTransactions ??= new Repository<ScheduledTransaction>(_context);
    public IRepository<Budget> Budgets => _budgets ??= new Repository<Budget>(_context);
    
    // Yeni repository'ler ⭐
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);
    public IRepository<FinancialHealthHistory> FinancialHealthHistories => _financialHealthHistories ??= new Repository<FinancialHealthHistory>(_context);
    public IRepository<Insight> Insights => _insights ??= new Repository<Insight>(_context);
    public IRepository<SavingsGoal> SavingsGoals => _savingsGoals ??= new Repository<SavingsGoal>(_context);

    // Generic repository erişimi ⭐
    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.ContainsKey(type))
        {
            _repositories[type] = new Repository<T>(_context);
        }
        return (IRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
