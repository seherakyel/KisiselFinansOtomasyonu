using KisiselFinans.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace KisiselFinans.Data.Context;

public class FinansDbContext : DbContext
{
    public FinansDbContext(DbContextOptions<FinansDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AccountType> AccountTypes => Set<AccountType>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ScheduledTransaction> ScheduledTransactions => Set<ScheduledTransaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    
    // Yeni tablolar ⭐
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FinancialHealthHistory> FinancialHealthHistories => Set<FinancialHealthHistory>();
    public DbSet<Insight> Insights => Set<Insight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Account
        modelBuilder.Entity<Account>(e =>
        {
            e.HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.AccountType)
                .WithMany(at => at.Accounts)
                .HasForeignKey(a => a.AccountTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Category (self-referencing)
        modelBuilder.Entity<Category>(e =>
        {
            e.HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Transaction
        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.RelatedTransaction)
                .WithOne()
                .HasForeignKey<Transaction>(t => t.RelatedTransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            e.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ScheduledTransaction
        modelBuilder.Entity<ScheduledTransaction>(e =>
        {
            e.HasOne(s => s.User)
                .WithMany(u => u.ScheduledTransactions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Account)
                .WithMany(a => a.ScheduledTransactions)
                .HasForeignKey(s => s.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Category)
                .WithMany(c => c.ScheduledTransactions)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Budget
        modelBuilder.Entity<Budget>(e =>
        {
            e.HasOne(b => b.User)
                .WithMany(u => u.Budgets)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Category)
                .WithMany(c => c.Budgets)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog ⭐
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // FinancialHealthHistory ⭐
        modelBuilder.Entity<FinancialHealthHistory>(e =>
        {
            e.Property(f => f.CalculatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Insight ⭐
        modelBuilder.Entity<Insight>(e =>
        {
            e.Property(i => i.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.RelatedCategory)
                .WithMany()
                .HasForeignKey(i => i.RelatedCategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
