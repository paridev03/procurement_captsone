using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementApi.Domain.Enums;
using ProcurementApi.DTOs;
using ProcurementApi.Services;

namespace ProcurementApi.Controllers;

[ApiController]
[Route("api/purchase-requests")]
[Authorize]
public class PurchaseRequestsController : ControllerBase
{
    private readonly IPurchaseRequestService _service;

    public PurchaseRequestsController(IPurchaseRequestService service)
    {
        _service = service;
    }

    /// <summary>Role-scoped worklist: Employee sees their own requests, Manager sees
    /// requests awaiting their approval, Finance sees manager-approved requests, and
    /// Procurement sees the procurement queue. See PurchaseRequestService.GetForCurrentUserAsync.</summary>
    [HttpGet]
    public async Task<ActionResult<List<PurchaseRequestSummaryDto>>> GetMyWorklist(CancellationToken ct) =>
        Ok(await _service.GetForCurrentUserAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseRequestDetailDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> Create([FromBody] CreatePurchaseRequestDto dto, CancellationToken ct)
    {
        var result = await _service.CreateDraftAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> Update(Guid id, [FromBody] UpdatePurchaseRequestDto dto, CancellationToken ct) =>
        Ok(await _service.UpdateDraftAsync(id, dto, ct));

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> Submit(Guid id, CancellationToken ct) =>
        Ok(await _service.SubmitAsync(id, ct));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = nameof(UserRole.Employee))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> Cancel(Guid id, CancellationToken ct) =>
        Ok(await _service.CancelAsync(id, ct));

    [HttpPost("{id:guid}/manager-decision")]
    [Authorize(Roles = nameof(UserRole.Manager))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> ManagerDecision(Guid id, [FromQuery] bool approve, [FromBody] DecisionDto dto, CancellationToken ct) =>
        Ok(await _service.ManagerDecisionAsync(id, approve, dto.Comment, ct));

    [HttpPost("{id:guid}/finance-decision")]
    [Authorize(Roles = nameof(UserRole.Finance))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> FinanceDecision(Guid id, [FromQuery] bool approve, [FromBody] DecisionDto dto, CancellationToken ct) =>
        Ok(await _service.FinanceDecisionAsync(id, approve, dto.Comment, ct));

    [HttpPost("{id:guid}/select-vendor")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> SelectVendor(Guid id, [FromBody] SelectVendorDto dto, CancellationToken ct) =>
        Ok(await _service.SelectVendorAsync(id, dto.VendorId, ct));

    [HttpPost("{id:guid}/trigger-payment")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> TriggerPayment(Guid id, CancellationToken ct) =>
        Ok(await _service.TriggerPaymentAsync(id, ct));
}
