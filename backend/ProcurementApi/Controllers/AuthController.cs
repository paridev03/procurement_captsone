using Microsoft.AspNetCore.Mvc;
using ProcurementApi.DTOs;
using ProcurementApi.Services;

namespace ProcurementApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>The only public (unauthenticated) endpoint in the API.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request.Email, request.Password, ct);
        return Ok(response);
    }
}
