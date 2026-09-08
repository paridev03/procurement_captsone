using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementApi.DTOs;
using ProcurementApi.Repositories;

namespace ProcurementApi.Controllers;

[ApiController]
[Route("api/vendors")]
[Authorize]
public class VendorsController : ControllerBase
{
    private readonly IVendorRepository _vendors;

    public VendorsController(IVendorRepository vendors)
    {
        _vendors = vendors;
    }

    /// <summary>Lookup list for the Procurement Admin's "select vendor" step. Only one
    /// vendor is seeded in this phase, but the endpoint/table already supports many.</summary>
    [HttpGet]
    public async Task<ActionResult<List<VendorDto>>> GetActive(CancellationToken ct)
    {
        var vendors = await _vendors.GetActiveAsync(ct);
        return Ok(vendors.Select(v => new VendorDto(v.Id, v.Name, v.ContactEmail, v.ContactPhone, v.TaxId)).ToList());
    }
}
