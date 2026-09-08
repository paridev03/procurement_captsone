namespace ProcurementApi.Services;

public record PaymentResult(bool Success, string? TransactionReference, string? FailureReason);

/// <summary>
/// Strategy seam for payment processing (see docs/ANALYSIS.md, Candidate Patterns).
/// One implementation (a single method: bank transfer) exists today; adding a second
/// provider means adding a new class here, never editing PurchaseRequestService.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessAsync(Guid purchaseRequestId, decimal amount, CancellationToken ct = default);
}

/// <summary>Local stand-in for a real payment provider. Simulates the two outcomes a real
/// gateway can return (success / failure) so the PaymentFailed path is exercisable without
/// external dependencies. In this simple version requests over $50,000 are rejected — a
/// deliberately arbitrary rule mirroring how a real gateway might decline large transfers.</summary>
public class FakePaymentGateway : IPaymentGateway
{
    public Task<PaymentResult> ProcessAsync(Guid purchaseRequestId, decimal amount, CancellationToken ct = default)
    {
        if (amount > 50_000m)
        {
            return Task.FromResult(new PaymentResult(false, null, "Amount exceeds gateway transaction limit."));
        }

        var reference = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{purchaseRequestId.ToString()[..8]}";
        return Task.FromResult(new PaymentResult(true, reference, null));
    }
}
