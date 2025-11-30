using KisiselFinans.Core.Entities;
using KisiselFinans.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace KisiselFinans.Business.Services;

public class UserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => 
            u.Username == username && u.IsActive);

        if (user == null) return null;

        var hash = HashPassword(password);
        return user.PasswordHash == hash ? user : null;
    }

    public async Task<User> RegisterAsync(string username, string password, string? email, string? fullName)
    {
        if (await _unitOfWork.Users.ExistsAsync(u => u.Username == username))
            throw new InvalidOperationException("Bu kullanıcı adı zaten mevcut.");

        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            Email = email,
            FullName = fullName,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return user;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
        => await _unitOfWork.Users.FindAsync(u => u.IsActive);

    public async Task<User?> GetByIdAsync(int id)
        => await _unitOfWork.Users.GetByIdAsync(id);

    public async Task UpdateAsync(User user)
    {
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        if (user.PasswordHash != HashPassword(currentPassword))
            throw new InvalidOperationException("Mevcut şifre yanlış.");

        user.PasswordHash = HashPassword(newPassword);
        await _unitOfWork.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

