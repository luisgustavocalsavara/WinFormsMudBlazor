using WinFormsMudBlazor.Models.Entities;

namespace WinFormsMudBlazor.Models.Interfaces;

public interface IAuthService
{
    User? CurrentUser { get; }
    bool IsAuthenticated { get; }
    event Action? AuthStateChanged;

    Task<(bool Success, string Message)> LoginAsync(string username, string password);
    Task<(bool Success, string Message)> RegisterUserAsync(string username, string password, string fullName, UserRole role);
    Task<List<User>> GetAllUsersAsync();
    Task DeleteUserAsync(int userId);
    void Logout();
    bool HasRole(params UserRole[] allowedRoles);
}
