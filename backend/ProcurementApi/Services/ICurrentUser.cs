namespace ProcurementApi.Services;

/// <summary>Reads the authenticated caller's identity out of JWT claims. Services depend on
/// this abstraction rather than HttpContext directly, keeping them testable.</summary>
public interface ICurrentUser
{
    Guid Id { get; }
    string Role { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid Id
    {
        get
        {
            var raw = _accessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }

    public string Role => _accessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
}
