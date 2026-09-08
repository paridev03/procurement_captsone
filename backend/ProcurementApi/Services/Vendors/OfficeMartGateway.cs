using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Services.Vendors;

/// <summary>OfficeMart's own wire format is { sku, quantityRequested }.</summary>
public class OfficeMartGateway : VendorGatewayBase
{
    public override string VendorName => "OfficeMart";

    public override Task<VendorQuoteResult> RequestQuoteAsync(PurchaseRequest request, CancellationToken ct = default)
    {
        // Wire shape: { "sku": PrimaryItemCode(request), "quantityRequested": request.EstimatedQuantity }
        var quotedUnitPrice = Math.Round(AverageUnitPrice(request) * 0.98m, 2); // simulated volume discount
        return Task.FromResult(new VendorQuoteResult(VendorName, Available: true, quotedUnitPrice, EstimatedDeliveryDays: 5));
    }
}
