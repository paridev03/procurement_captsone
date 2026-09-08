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

    /// <summary>Status-tile counts for the landing Dashboard, scoped to the same
    /// role-based visibility as the worklist above.</summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboard(CancellationToken ct) =>
        Ok(await _service.GetDashboardSummaryAsync(ct));

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

    /// <summary>Procurement Admin's itemized draft creation — same aggregate/state machine
    /// as Create above, with a catalog-backed line-item list instead of one inline
    /// quantity/unit cost. See PurchaseRequestService.CreateItemizedDraftAsync.</summary>
    [HttpPost("itemized")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> CreateItemized([FromBody] CreateItemizedPurchaseRequestDto dto, CancellationToken ct)
    {
        var result = await _service.CreateItemizedDraftAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/itemized")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> UpdateItemized(Guid id, [FromBody] UpdateItemizedPurchaseRequestDto dto, CancellationToken ct) =>
        Ok(await _service.UpdateItemizedDraftAsync(id, dto, ct));

    // Submit/Cancel are shared by both the plain Employee flow and the Procurement Admin
    // itemized flow — both produce the same PurchaseRequest aggregate, so the same
    // state-machine-driven actions (and the same downstream Manager/Finance approval
    // process) apply to either one without any new workflow.
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = $"{nameof(UserRole.Employee)},{nameof(UserRole.ProcurementAdmin)}")]
    public async Task<ActionResult<PurchaseRequestDetailDto>> Submit(Guid id, CancellationToken ct) =>
        Ok(await _service.SubmitAsync(id, ct));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = $"{nameof(UserRole.Employee)},{nameof(UserRole.ProcurementAdmin)}")]
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

    /// <summary>Finance's "Check Budget" action — a preview before deciding; approval
    /// itself re-validates independently (see FinanceDecisionAsync).</summary>
    [HttpGet("{id:guid}/budget-check")]
    [Authorize(Roles = nameof(UserRole.Finance))]
    public async Task<ActionResult<BudgetCheckDto>> CheckBudget(Guid id, CancellationToken ct) =>
        Ok(await _service.CheckBudgetAsync(id, ct));

    [HttpPost("{id:guid}/select-vendor")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> SelectVendor(Guid id, [FromBody] SelectVendorDto dto, CancellationToken ct) =>
        Ok(await _service.SelectVendorAsync(id, dto.VendorId, ct));

    [HttpPost("{id:guid}/vendor-quote")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<VendorQuoteDto>> RequestVendorQuote(Guid id, CancellationToken ct) =>
        Ok(await _service.RequestVendorQuoteAsync(id, ct));

    [HttpPost("{id:guid}/trigger-payment")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> TriggerPayment(Guid id, CancellationToken ct) =>
        Ok(await _service.TriggerPaymentAsync(id, ct));

    // ---- Payment Page ------------------------------------------------------------

    [HttpPost("{id:guid}/payment/draft")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> SavePaymentDraft(Guid id, [FromBody] SavePaymentDto dto, CancellationToken ct) =>
        Ok(await _service.SavePaymentDraftAsync(id, dto, ct));

    [HttpPost("{id:guid}/payment/mark-paid")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<PurchaseRequestDetailDto>> MarkAsPaid(Guid id, [FromBody] SavePaymentDto dto, CancellationToken ct) =>
        Ok(await _service.MarkAsPaidAsync(id, dto, ct));

    // ---- Invoice Page ------------------------------------------------------------

    [HttpPost("{id:guid}/invoice/generate")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<InvoiceDto>> GenerateInvoice(Guid id, [FromBody] GenerateInvoiceDto dto, CancellationToken ct) =>
        Ok(await _service.GenerateInvoiceAsync(id, dto, ct));

    [HttpGet("{id:guid}/invoice")]
    [Authorize(Roles = nameof(UserRole.ProcurementAdmin))]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(Guid id, CancellationToken ct) =>
        Ok(await _service.GetInvoiceAsync(id, ct));
}
