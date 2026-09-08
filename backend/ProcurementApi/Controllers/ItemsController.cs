using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementApi.DTOs;
using ProcurementApi.Repositories;

namespace ProcurementApi.Controllers;

[ApiController]
[Route("api/items")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly IItemRepository _items;

    public ItemsController(IItemRepository items)
    {
        _items = items;
    }

    /// <summary>Item/material catalog lookup for the Procurement Admin itemized flow's
    /// searchable item selector — mirrors VendorsController's shape.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ItemDto>>> GetActive(CancellationToken ct)
    {
        var items = await _items.GetActiveAsync(ct);
        return Ok(items.Select(i => new ItemDto(i.Id, i.Code, i.Name, i.Description, i.UnitPrice)).ToList());
    }
}
