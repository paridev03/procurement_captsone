using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Services.Vendors;

/// <summary>Shared helpers for deriving a quote request's product code/quantity/price from
/// a PurchaseRequest, regardless of whether it came from the plain single-line Employee
/// flow or the itemized Procurement Admin flow. Each concrete gateway builds its own
/// vendor-specific request shape from these.</summary>
public abstract class VendorGatewayBase : IVendorGateway
{
    public abstract string VendorName { get; }
    public abstract Task<VendorQuoteResult> RequestQuoteAsync(PurchaseRequest request, CancellationToken ct = default);

    protected static string PrimaryItemCode(PurchaseRequest request) =>
        request.Items.OrderBy(i => i.CreatedAt).FirstOrDefault()?.ItemCode ?? request.Title;

    protected static decimal AverageUnitPrice(PurchaseRequest request) =>
        request.EstimatedQuantity > 0 ? request.EstimatedTotalCost / request.EstimatedQuantity : request.EstimatedUnitCost;
}
