using System.ComponentModel.DataAnnotations;

namespace ProcurementApi.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, DateTime ExpiresAt, UserDto User);

public record UserDto(Guid Id, string FullName, string Email, string Role, string Department);

public record RegisterRequest(
    [Required, MaxLength(200)] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string Role,
    [Required, MaxLength(100)] string Department
);
