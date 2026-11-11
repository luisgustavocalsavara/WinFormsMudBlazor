using Microsoft.EntityFrameworkCore;
using WinFormsMudBlazor.Models.Entities;
using WinFormsMudBlazor.Models.Interfaces;
using WinFormsMudBlazor.Services.Data;

namespace WinFormsMudBlazor.Services;

public class AuthService(IDbContextFactory<AppDbContext> dbFactory) : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;

    public event Action? AuthStateChanged;

    public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "Informe o usuário e a senha.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var hash = AppDbContext.HashPassword(password);
        var normalizedUser = username.Trim().ToLower();

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUser && u.PasswordHash == hash);

        if (user is null)
            return (false, "Usuário ou senha inválidos.");

        CurrentUser = user;
        AuthStateChanged?.Invoke();
        return (true, $"Bem-vindo, {user.FullName}!");
    }

    public async Task<(bool Success, string Message)> RegisterUserAsync(string username, string password, string fullName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
            return (false, "Preencha todos os campos obrigatórios.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var normalized = username.Trim().ToLower();

        var exists = await db.Users.AnyAsync(u => u.Username.ToLower() == normalized);
        if (exists)
            return (false, "Este nome de usuário já está cadastrado.");

        var newUser = new User
        {
            Username = username.Trim(),
            PasswordHash = AppDbContext.HashPassword(password),
            FullName = fullName.Trim(),
            Role = role,
            CreatedAt = DateTime.Now
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();
        return (true, "Usuário cadastrado com sucesso!");
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync();
    }

    public async Task DeleteUserAsync(int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user is not null && user.Username.ToLower() != "admin")
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }
    }

    public void Logout()
    {
        CurrentUser = null;
        AuthStateChanged?.Invoke();
    }

    public bool HasRole(params UserRole[] allowedRoles)
    {
        if (CurrentUser is null) return false;
        return allowedRoles.Contains(CurrentUser.Role);
    }
}
