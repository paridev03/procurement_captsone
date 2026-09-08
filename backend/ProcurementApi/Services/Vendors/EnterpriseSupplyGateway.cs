using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Services.Vendors;

/// <summary>EnterpriseSupply's own wire format is { item: { code, qty } }.</summary>
public class EnterpriseSupplyGateway : VendorGatewayBase
{
    public override string VendorName => "EnterpriseSupply";

    public override Task<VendorQuoteResult> RequestQuoteAsync(PurchaseRequest request, CancellationToken ct = default)
    {
        // Wire shape: { "item": { "code": PrimaryItemCode(request), "qty": request.EstimatedQuantity } }
        var quotedUnitPrice = Math.Round(AverageUnitPrice(request) * 1.00m, 2); // at-cost, no markup
        return Task.FromResult(new VendorQuoteResult(VendorName, Available: true, quotedUnitPrice, EstimatedDeliveryDays: 7));
    }
}
