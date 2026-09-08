namespace ProcurementApi.Services.Vendors;

public interface IVendorGatewayFactory
{
    /// <summary>Resolves the gateway for a vendor by name, or null if this vendor has no
    /// live quote integration (e.g. a vendor added to the Vendors table without one of the
    /// three supported adapters) — Procurement Admin can still select such a vendor, they
    /// just can't request an automated quote for it.</summary>
    IVendorGateway? TryGet(string vendorName);
}
