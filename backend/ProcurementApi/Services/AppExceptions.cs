namespace ProcurementApi.Services;

/// <summary>Base for exceptions the global exception middleware knows how to translate into
/// an HTTP status code, so services stay HTTP-agnostic (Section 7/8 of docs/ANALYSIS.md).</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Bad credentials at login -> HTTP 401.</summary>
public class InvalidCredentialsException : AppException
{
    public InvalidCredentialsException(string message) : base(message) { }
}

/// <summary>Valid credentials/role, but not allowed to act on *this* resource
/// (e.g. a Manager who isn't this employee's manager) -> HTTP 403.</summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>Requested action isn't valid from the request's current status -> HTTP 409.</summary>
public class InvalidStateTransitionException : AppException
{
    public InvalidStateTransitionException(string message) : base(message) { }
}

public class ConcurrencyConflictException : AppException
{
    public ConcurrencyConflictException(string message) : base(message) { }
}

/// <summary>Well-formed request, but fails a business validation rule the DTO's data
/// annotations can't express on their own (e.g. an item line referencing a catalog item
/// that doesn't exist or is inactive) -> HTTP 400.</summary>
public class ValidationException : AppException
{
    public ValidationException(string message) : base(message) { }
}

/// <summary>A simulated/real external dependency (Budget Service, Vendor gateway, ...)
/// failed even after retry -> HTTP 503. Distinct from a business rejection (e.g.
/// insufficient budget), which is a normal ValidationException, not a dependency failure.</summary>
public class ExternalServiceException : AppException
{
    public ExternalServiceException(string message) : base(message) { }
}
