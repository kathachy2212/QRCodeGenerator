using System.Threading.Tasks;
using QRCodeGenerator.Models;

public interface IUserService
{
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateUserAsync(string email, string? passwordHash, string provider);
    Task<bool> ValidateUserAsync(string email, string password);
    Task<bool> UpdatePasswordAsync(string email, string newPassword);
    Task<bool> UpdateProfileAsync(string email, string fullName, string phone, string address);
}
