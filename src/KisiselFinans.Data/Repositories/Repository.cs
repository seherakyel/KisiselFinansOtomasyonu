using System.Linq.Expressions;
using KisiselFinans.Core.Interfaces;
using KisiselFinans.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace KisiselFinans.Data.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly FinansDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(FinansDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) 
        => await _dbSet.Where(predicate).ToListAsync();

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate) 
        => await _dbSet.FirstOrDefaultAsync(predicate);

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public void Update(T entity) => _dbSet.Update(entity);

    public void Remove(T entity) => _dbSet.Remove(entity);

    public void Delete(T entity) => _dbSet.Remove(entity);

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) 
        => await _dbSet.AnyAsync(predicate);

    public IQueryable<T> Query() => _dbSet.AsQueryable();
}

