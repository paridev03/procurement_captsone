using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementApi.Data;
using ProcurementApi.Domain.Entities;
using ProcurementApi.DTOs;

namespace ProcurementApi.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly ProcurementDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(ProcurementDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        var (token, expiresAt) = _jwt.CreateToken(user);
        var userDto = new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.Department);
        return new LoginResponse(token, expiresAt, userDto);
    }
}
