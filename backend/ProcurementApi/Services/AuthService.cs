using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcurementApi.Data;
using ProcurementApi.Domain.Entities;
using ProcurementApi.Domain.Enums;
using ProcurementApi.DTOs;

namespace ProcurementApi.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
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

    /// <summary>Self-registration for the demo/capstone: any of the four roles can be
    /// chosen at signup rather than requiring a separate admin-provisioning flow. Returns
    /// the same shape as LoginAsync so a successful registration logs the user straight in.</summary>
    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            throw new ValidationException($"Unknown role '{request.Role}'.");
        }

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (emailTaken)
        {
            throw new ValidationException("An account with this email already exists.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Role = role,
            Department = request.Department,
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _jwt.CreateToken(user);
        var userDto = new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.Department);
        return new LoginResponse(token, expiresAt, userDto);
    }
}
