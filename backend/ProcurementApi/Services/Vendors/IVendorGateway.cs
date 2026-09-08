using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Services.Vendors;

public record VendorQuoteResult(string VendorName, bool Available, decimal QuotedUnitPrice, int EstimatedDeliveryDays);

/// <summary>Adapter seam: each supported vendor speaks a different wire format (see
/// Section 8 of the Phase 2 spec) but Procurement Admin's "Request Vendor Quote" action
/// only ever needs to know about this one shape — the vendor-specific request/response
/// translation lives entirely inside each concrete gateway (TechSourceGateway etc.),
/// mirroring how IPaymentGateway already isolates the payment provider's own shape.</summary>
public interface IVendorGateway
{
    string VendorName { get; }
    Task<VendorQuoteResult> RequestQuoteAsync(PurchaseRequest request, CancellationToken ct = default);
}
