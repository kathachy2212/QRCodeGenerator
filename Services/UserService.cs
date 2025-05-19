using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using QRCodeGenerator.Models;

public class UserService : IUserService
{
    private readonly QRDbContextName _context;

    public UserService(QRDbContextName context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> CreateUserAsync(string email, string? passwordHash, string provider)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = passwordHash,
            Provider = provider
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ValidateUserAsync(string email, string password)
    {
        var user = await GetByEmailAsync(email);
        if (user == null) { return false; }
        if (user.Id == null || user.Provider == "Local") return false;
        if (user.PasswordHash == null) return false;
        return VerifyPassword(password, user.PasswordHash);
    }


    //passoord varify
    private bool VerifyPassword(string password, string storedHash)
    {
        var hashOfInput = ComputeSha256Hash(password);
        return hashOfInput == storedHash;
    }

    public static string ComputeSha256Hash(string rawData)
    {
        // Create a SHA256
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash returns byte array
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            // Convert byte array to a string   
            var builder = new StringBuilder();
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }


    public async Task<bool> UpdatePasswordAsync(string email, string newPassword)
    {
        var user = await GetByEmailAsync(email);
        if (user.Id == null || user.Provider == "Local") return false;

        user.PasswordHash = ComputeSha256Hash(newPassword);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateProfileAsync(string email, string fullName, string phone, string address)
    {
        var user = await GetByEmailAsync(email);
        if (user == null) return false;

        user.FullName = fullName;
        user.Phone = phone;
        user.Address = address;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return true;
    }
}
