using ProcurementApi.Domain.Entities;

namespace ProcurementApi.Services.Vendors;

/// <summary>TechSource's own wire format is { productCode, units } — built here and never
/// leaks past this adapter.</summary>
public class TechSourceGateway : VendorGatewayBase
{
    public override string VendorName => "TechSource";

    public override Task<VendorQuoteResult> RequestQuoteAsync(PurchaseRequest request, CancellationToken ct = default)
    {
        // Wire shape: { "productCode": PrimaryItemCode(request), "units": request.EstimatedQuantity }
        var quotedUnitPrice = Math.Round(AverageUnitPrice(request) * 1.02m, 2); // simulated small markup
        return Task.FromResult(new VendorQuoteResult(VendorName, Available: true, quotedUnitPrice, EstimatedDeliveryDays: 3));
    }
}
