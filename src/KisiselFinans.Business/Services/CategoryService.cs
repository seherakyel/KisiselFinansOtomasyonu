using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KisiselFinans.Business.Services;

public class CategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Category>> GetUserCategoriesAsync(int userId)
        => await _unitOfWork.Categories.Query()
            .Where(c => c.UserId == null || c.UserId == userId)
            .Include(c => c.SubCategories)
            .ToListAsync();

    public async Task<IEnumerable<Category>> GetIncomeCategories(int userId)
        => await _unitOfWork.Categories.Query()
            .Where(c => (c.UserId == null || c.UserId == userId) && c.Type == 1)
            .ToListAsync();

    public async Task<IEnumerable<Category>> GetExpenseCategories(int userId)
        => await _unitOfWork.Categories.Query()
            .Where(c => (c.UserId == null || c.UserId == userId) && c.Type == 2)
            .ToListAsync();

    public async Task<Category?> GetByIdAsync(int id)
        => await _unitOfWork.Categories.GetByIdAsync(id);

    public async Task<Category> CreateAsync(Category category)
    {
        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();
        return category;
    }

    public async Task UpdateAsync(Category category)
    {
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(id);
        if (category != null)
        {
            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}

