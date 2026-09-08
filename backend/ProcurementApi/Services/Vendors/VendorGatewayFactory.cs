namespace ProcurementApi.Services.Vendors;

/// <summary>Factory pattern: which concrete IVendorGateway to use depends entirely on the
/// vendor's name, and that decision is centralized here rather than the caller
/// (PurchaseRequestService) branching on vendor name itself.</summary>
public class VendorGatewayFactory : IVendorGatewayFactory
{
    private readonly Dictionary<string, IVendorGateway> _gateways;

    public VendorGatewayFactory(IEnumerable<IVendorGateway> gateways)
    {
        _gateways = gateways.ToDictionary(g => g.VendorName, StringComparer.OrdinalIgnoreCase);
    }

    public IVendorGateway? TryGet(string vendorName) =>
        _gateways.TryGetValue(vendorName, out var gateway) ? gateway : null;
}
