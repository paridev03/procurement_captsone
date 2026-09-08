using Microsoft.EntityFrameworkCore;
using ProcurementApi.Data;
using ProcurementApi.Domain.Entities;
using ProcurementApi.Domain.Enums;
using ProcurementApi.DTOs;
using ProcurementApi.Repositories;
using ProcurementApi.Services.Vendors;

namespace ProcurementApi.Services;

public interface IPurchaseRequestService
{
    Task<PurchaseRequestDetailDto> CreateDraftAsync(CreatePurchaseRequestDto dto, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> UpdateDraftAsync(Guid id, UpdatePurchaseRequestDto dto, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> CreateItemizedDraftAsync(CreateItemizedPurchaseRequestDto dto, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> UpdateItemizedDraftAsync(Guid id, UpdateItemizedPurchaseRequestDto dto, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> SubmitAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> CancelAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> ManagerDecisionAsync(Guid id, bool approve, string? comment, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> FinanceDecisionAsync(Guid id, bool approve, string? comment, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> SelectVendorAsync(Guid id, Guid vendorId, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> TriggerPaymentAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PurchaseRequestSummaryDto>> GetForCurrentUserAsync(CancellationToken ct = default);

    // ---- Payment Page / Invoice Page (Procurement -> Payment -> Invoice workflow) -----
    Task<PurchaseRequestDetailDto> SavePaymentDraftAsync(Guid id, SavePaymentDto dto, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> MarkAsPaidAsync(Guid id, SavePaymentDto dto, CancellationToken ct = default);
    Task<InvoiceDto> GenerateInvoiceAsync(Guid id, GenerateInvoiceDto dto, CancellationToken ct = default);
    Task<InvoiceDto> GetInvoiceAsync(Guid id, CancellationToken ct = default);

    // ---- Budget Service / Vendor quote / Dashboard (Phase 2 additions) -----------------
    Task<BudgetCheckDto> CheckBudgetAsync(Guid id, CancellationToken ct = default);
    Task<VendorQuoteDto> RequestVendorQuoteAsync(Guid id, CancellationToken ct = default);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default);
}

/// <summary>
/// All business logic for the request lifecycle lives here (never in the controller).
/// Every mutation: (1) checks the caller is allowed to act on *this* request,
/// (2) asks RequestStateMachine whether the action is legal from the current status,
/// (3) applies the change, (4) writes an audit-trail row, (5) saves.
/// </summary>
public class PurchaseRequestService : IPurchaseRequestService
{
    private readonly IPurchaseRequestRepository _requests;
    private readonly ProcurementDbContext _db; // used only for cross-aggregate reads (Users) needed for authorization checks
    private readonly ICurrentUser _currentUser;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IItemRepository _items;
    private readonly IBudgetService _budgetService;
    private readonly IVendorGatewayFactory _vendorGateways;
    private readonly IEnumerable<IRequestEventObserver> _eventObservers;

    // Events raised by Transition() during this unit of work, dispatched to observers only
    // after SaveWithConcurrencyCheckAsync confirms the change was actually persisted — never
    // notify about a transition that got rolled back by a concurrency conflict.
    private readonly List<(PurchaseRequest Request, NotificationEvent Event, string Note)> _pendingEvents = new();

    private static readonly Dictionary<RequestAction, NotificationEvent> ActionToNotificationEvent = new()
    {
        [RequestAction.Submit] = NotificationEvent.RequestSubmitted,
        [RequestAction.ManagerApprove] = NotificationEvent.ManagerApproved,
        [RequestAction.ManagerReject] = NotificationEvent.ManagerRejected,
        [RequestAction.FinanceApprove] = NotificationEvent.FinanceApproved,
        [RequestAction.FinanceReject] = NotificationEvent.FinanceRejected,
        [RequestAction.SelectVendor] = NotificationEvent.VendorSelected,
        [RequestAction.TriggerPayment] = NotificationEvent.PaymentStarted,
        [RequestAction.RetryPayment] = NotificationEvent.PaymentStarted,
        [RequestAction.PaymentSucceeded] = NotificationEvent.PaymentCompleted,
        [RequestAction.PaymentFailed] = NotificationEvent.PaymentFailed,
    };

    public PurchaseRequestService(
        IPurchaseRequestRepository requests,
        ProcurementDbContext db,
        ICurrentUser currentUser,
        IPaymentGateway paymentGateway,
        IItemRepository items,
        IBudgetService budgetService,
        IVendorGatewayFactory vendorGateways,
        IEnumerable<IRequestEventObserver> eventObservers)
    {
        _requests = requests;
        _db = db;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
        _items = items;
        _budgetService = budgetService;
        _vendorGateways = vendorGateways;
        _eventObservers = eventObservers;
    }

    public async Task<PurchaseRequestDetailDto> CreateDraftAsync(CreatePurchaseRequestDto dto, CancellationToken ct = default)
    {
        var requester = await _db.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.Id, ct)
            ?? throw new NotFoundException("Current user not found.");

        var year = DateTime.UtcNow.Year;
        var sequence = await _requests.GetNextSequenceForYearAsync(year, ct);

        var request = new PurchaseRequest
        {
            RequestNumber = $"PR-{year}-{sequence:D6}",
            RequesterId = requester.Id,
            Title = dto.Title,
            BusinessJustification = dto.BusinessJustification,
            Department = dto.Department,
            Category = ParseCategory(dto.Category),
            EstimatedQuantity = dto.EstimatedQuantity,
            EstimatedUnitCost = dto.EstimatedUnitCost,
            EstimatedTotalCost = dto.EstimatedQuantity * dto.EstimatedUnitCost,
            Status = RequestStatus.Draft,
        };
        request.TotalAmount = request.EstimatedTotalCost; // no tax concept in the plain flow
        await _requests.AddAsync(request, ct);
        AddHistoryRow(request.Id, null, RequestStatus.Draft, "Draft created.");
        await _requests.SaveChangesAsync(ct);

        return await GetByIdAsync(request.Id, ct);
    }

    public async Task<PurchaseRequestDetailDto> UpdateDraftAsync(Guid id, UpdatePurchaseRequestDto dto, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);
        EnsureOwner(request);

        if (request.Status != RequestStatus.Draft)
        {
            throw new InvalidStateTransitionException("Only DRAFT requests can be edited.");
        }

        request.Title = dto.Title;
        request.BusinessJustification = dto.BusinessJustification;
        request.Department = dto.Department;
        request.Category = ParseCategory(dto.Category);
        request.EstimatedQuantity = dto.EstimatedQuantity;
        request.EstimatedUnitCost = dto.EstimatedUnitCost;
        request.EstimatedTotalCost = dto.EstimatedQuantity * dto.EstimatedUnitCost;
        request.TotalAmount = request.EstimatedTotalCost;

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    /// <summary>Procurement Admin's itemized draft creation — same DRAFT-status aggregate
    /// root and RequestStateMachine as the plain Employee flow (see CreateDraftAsync), just
    /// with a list of catalog-backed lines instead of one inline quantity/unit cost.</summary>
    public async Task<PurchaseRequestDetailDto> CreateItemizedDraftAsync(CreateItemizedPurchaseRequestDto dto, CancellationToken ct = default)
    {
        var requester = await _db.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.Id, ct)
            ?? throw new NotFoundException("Current user not found.");

        var year = DateTime.UtcNow.Year;
        var sequence = await _requests.GetNextSequenceForYearAsync(year, ct);

        var request = new PurchaseRequest
        {
            RequestNumber = $"PR-{year}-{sequence:D6}",
            RequesterId = requester.Id,
            Title = dto.Title,
            BusinessJustification = dto.BusinessJustification,
            Department = dto.Department,
            Category = ParseCategory(dto.Category),
            Status = RequestStatus.Draft,
        };
        await _requests.AddAsync(request, ct);

        var lines = await BuildLineItemsAsync(request.Id, dto.Items, ct);
        _db.PurchaseRequestItems.AddRange(lines);
        RecalculateItemizedTotals(request, lines, dto.TaxRate);

        AddHistoryRow(request.Id, null, RequestStatus.Draft, "Draft created (itemized).");
        await _requests.SaveChangesAsync(ct);

        return await GetByIdAsync(request.Id, ct);
    }

    public async Task<PurchaseRequestDetailDto> UpdateItemizedDraftAsync(Guid id, UpdateItemizedPurchaseRequestDto dto, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);
        EnsureOwner(request);

        if (request.Status != RequestStatus.Draft)
        {
            throw new InvalidStateTransitionException("Only DRAFT requests can be edited.");
        }

        request.Title = dto.Title;
        request.BusinessJustification = dto.BusinessJustification;
        request.Department = dto.Department;
        request.Category = ParseCategory(dto.Category);

        await ReplaceLineItemsAsync(request, dto.Items, ct);
        RecalculateItemizedTotals(request, request.Items, dto.TaxRate);

        request.ModifiedByUserId = _currentUser.Id;
        request.ModifiedAt = DateTime.UtcNow;

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    /// <summary>Resolves each submitted line against the item catalog and builds the child
    /// entities, added via the DbSet directly rather than through a navigation collection —
    /// see AddHistoryRow for why that matters when the parent is a freshly-tracked new
    /// entity. `line.Description`/`UnitPrice` are honoured when the caller supplied them
    /// (a business-approved override, e.g. a negotiated price); otherwise the catalog's
    /// current values are auto-populated, per the itemized flow's "select an item, get its
    /// details for free, edit only where allowed" rule.</summary>
    private async Task<List<PurchaseRequestItem>> BuildLineItemsAsync(
        Guid purchaseRequestId, List<PurchaseRequestItemLineDto> lines, CancellationToken ct)
    {
        var result = new List<PurchaseRequestItem>();
        foreach (var line in lines)
        {
            var catalogItem = await _items.GetByIdAsync(line.ItemId, ct);
            if (catalogItem is null || !catalogItem.IsActive)
            {
                throw new ValidationException($"Item '{line.ItemId}' was not found or is no longer active.");
            }

            var unitPrice = line.UnitPrice ?? catalogItem.UnitPrice;
            result.Add(new PurchaseRequestItem
            {
                PurchaseRequestId = purchaseRequestId,
                ItemId = catalogItem.Id,
                ItemCode = catalogItem.Code,
                Description = string.IsNullOrWhiteSpace(line.Description) ? catalogItem.Description : line.Description,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = line.Quantity * unitPrice,
            });
        }
        return result;
    }

    /// <summary>Applies an edited item list to an existing draft without duplicating rows:
    /// lines whose Id matches an existing one are updated in place, lines with no Id (or an
    /// Id that no longer matches) are added as new, and existing lines missing from the
    /// incoming list are removed. This is the update-in-place counterpart to
    /// BuildLineItemsAsync's insert-only path.</summary>
    private async Task ReplaceLineItemsAsync(PurchaseRequest request, List<PurchaseRequestItemLineDto> lines, CancellationToken ct)
    {
        var existingById = request.Items.ToDictionary(i => i.Id);
        var keptIds = new HashSet<Guid>();

        foreach (var line in lines)
        {
            var catalogItem = await _items.GetByIdAsync(line.ItemId, ct);
            if (catalogItem is null || !catalogItem.IsActive)
            {
                throw new ValidationException($"Item '{line.ItemId}' was not found or is no longer active.");
            }

            var unitPrice = line.UnitPrice ?? catalogItem.UnitPrice;
            var description = string.IsNullOrWhiteSpace(line.Description) ? catalogItem.Description : line.Description;

            if (line.Id is Guid existingId && existingById.TryGetValue(existingId, out var existing))
            {
                existing.ItemId = catalogItem.Id;
                existing.ItemCode = catalogItem.Code;
                existing.Description = description;
                existing.Quantity = line.Quantity;
                existing.UnitPrice = unitPrice;
                existing.TotalPrice = line.Quantity * unitPrice;
                existing.ModifiedAt = DateTime.UtcNow;
                keptIds.Add(existingId);
            }
            else
            {
                var added = new PurchaseRequestItem
                {
                    PurchaseRequestId = request.Id,
                    ItemId = catalogItem.Id,
                    ItemCode = catalogItem.Code,
                    Description = description,
                    Quantity = line.Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = line.Quantity * unitPrice,
                };
                // Not also added to request.Items here: EF Core's own relationship
                // fixup already appends a newly-tracked child whose FK matches an
                // already-loaded parent collection — adding it a second time here
                // duplicated every new line in the very next read within this request.
                _db.PurchaseRequestItems.Add(added);
                keptIds.Add(added.Id);
            }
        }

        foreach (var stale in existingById.Values.Where(i => !keptIds.Contains(i.Id)).ToList())
        {
            _db.PurchaseRequestItems.Remove(stale);
            request.Items.Remove(stale);
        }
    }

    /// <summary>Server-authoritative rollup of the header's TotalItems/TotalQuantity/
    /// EstimatedTotalCost (= SubTotal) from its line items — recomputed here (never trusted
    /// from the client) and again defensively in SubmitAsync before the status transition.
    /// `taxRate` is a percentage (0-100): when supplied, TaxAmount is (re)computed from it;
    /// when omitted (e.g. the defensive recheck in SubmitAsync, which has no rate to hand),
    /// the previously-stored TaxAmount is kept as-is and only TotalAmount is refreshed
    /// against the freshest SubTotal.</summary>
    private static void RecalculateItemizedTotals(PurchaseRequest request, IEnumerable<PurchaseRequestItem> lines, decimal? taxRate = null)
    {
        var items = lines.ToList();
        request.TotalItems = items.Count;
        request.TotalQuantity = items.Sum(i => i.Quantity);
        request.EstimatedQuantity = request.TotalQuantity;
        request.EstimatedUnitCost = 0; // not meaningful across multiple distinct items
        request.EstimatedTotalCost = items.Sum(i => i.TotalPrice);

        if (taxRate.HasValue)
        {
            request.TaxAmount = Math.Round(request.EstimatedTotalCost * taxRate.Value / 100m, 2);
        }
        request.TotalAmount = request.EstimatedTotalCost + request.TaxAmount;
    }

    public async Task<PurchaseRequestDetailDto> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);
        EnsureOwner(request);

        Transition(request, RequestAction.Submit, "Submitted for manager approval.");
        request.SubmittedAt = DateTime.UtcNow;

        // Defense in depth (Section 8 technical requirements): recalculate from the items
        // actually persisted right before the transition, even though Create/Update already
        // did so — the request should never be submitted with stale or client-supplied totals.
        if (request.Items.Count > 0)
        {
            RecalculateItemizedTotals(request, request.Items);
        }

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<PurchaseRequestDetailDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);
        EnsureOwner(request);

        Transition(request, RequestAction.Cancel, "Cancelled by requester.");

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<PurchaseRequestDetailDto> ManagerDecisionAsync(Guid id, bool approve, string? comment, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);

        var requesterManagerId = await _db.Users
            .Where(u => u.Id == request.RequesterId)
            .Select(u => u.ManagerId)
            .FirstOrDefaultAsync(ct);

        if (requesterManagerId != _currentUser.Id)
        {
            throw new ForbiddenException("You are not the manager for this request's requester.");
        }

        var action = approve ? RequestAction.ManagerApprove : RequestAction.ManagerReject;
        Transition(request, action, approve ? "Manager approved." : "Manager rejected.");

        // See AddHistoryRow for why this goes through the DbSet directly rather than
        // request.Approvals.Add(...).
        _db.RequestApprovals.Add(new RequestApproval
        {
            PurchaseRequestId = request.Id,
            ApproverId = _currentUser.Id,
            Stage = ApprovalStage.ManagerApproval,
            Decision = approve ? ApprovalDecision.Approved : ApprovalDecision.Rejected,
            Comment = comment,
        });

        if (approve) request.ManagerApprovedAt = DateTime.UtcNow;

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<PurchaseRequestDetailDto> FinanceDecisionAsync(Guid id, bool approve, string? comment, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);

        // Defense in depth: Finance's "Check Budget" action (CheckBudgetAsync) already lets
        // them preview this before deciding, but approval itself re-validates independently
        // rather than trusting that the UI called it first.
        if (approve)
        {
            var budget = await CheckBudgetWithRetryAsync(request.Department, request.TotalAmount, ct);
            if (!budget.Available)
            {
                throw new ValidationException(
                    $"Department budget is insufficient (remaining: ${budget.RemainingBudget.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)}).");
            }
        }

        var action = approve ? RequestAction.FinanceApprove : RequestAction.FinanceReject;
        Transition(request, action, approve ? "Finance approved budget." : "Finance rejected.");

        _db.RequestApprovals.Add(new RequestApproval
        {
            PurchaseRequestId = request.Id,
            ApproverId = _currentUser.Id,
            Stage = ApprovalStage.FinanceApproval,
            Decision = approve ? ApprovalDecision.Approved : ApprovalDecision.Rejected,
            Comment = comment,
        });

        if (approve)
        {
            request.FinanceApprovedAt = DateTime.UtcNow;
            await _budgetService.SpendAsync(request.Department, request.TotalAmount, ct);
        }

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<BudgetCheckDto> CheckBudgetAsync(Guid id, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);
        var result = await CheckBudgetWithRetryAsync(request.Department, request.TotalAmount, ct);
        return new BudgetCheckDto(result.Available, result.RemainingBudget);
    }

    /// <summary>Resilience for the simulated external Budget Service (Phase 2 spec, Section
    /// 20/21): one retry on a transient failure before giving up, matching the spec's own
    /// "Budget Service → Timeout → Retry → Failure Response" example.</summary>
    private async Task<BudgetCheckResult> CheckBudgetWithRetryAsync(string department, decimal amount, CancellationToken ct)
    {
        try
        {
            return await _budgetService.CheckAsync(department, amount, ct);
        }
        catch (ExternalServiceException)
        {
            await Task.Delay(300, ct);
            try
            {
                return await _budgetService.CheckAsync(department, amount, ct);
            }
            catch (ExternalServiceException)
            {
                throw new ExternalServiceException("Budget Service is currently unavailable. Please try again shortly.");
            }
        }
    }

    public async Task<PurchaseRequestDetailDto> SelectVendorAsync(Guid id, Guid vendorId, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);

        var vendorExists = await _db.Vendors.AnyAsync(v => v.Id == vendorId && v.IsActive, ct);
        if (!vendorExists)
        {
            throw new NotFoundException("Vendor not found or inactive.");
        }

        var isOverride = request.Status == RequestStatus.VendorSelected;
        Transition(request, RequestAction.SelectVendor, isOverride ? "Vendor overridden." : "Vendor selected.");
        request.VendorId = vendorId;

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    /// <summary>Adapter + Factory in action: which vendor-specific request/response shape
    /// to speak is resolved purely from the selected vendor's name, never branched on here.</summary>
    public async Task<VendorQuoteDto> RequestVendorQuoteAsync(Guid id, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);

        if (request.Vendor is null)
        {
            throw new ValidationException("Select a vendor before requesting a quote.");
        }

        var gateway = _vendorGateways.TryGet(request.Vendor.Name)
            ?? throw new ValidationException($"'{request.Vendor.Name}' has no automated quote integration.");

        var quote = await gateway.RequestQuoteAsync(request, ct);
        return new VendorQuoteDto(quote.VendorName, quote.Available, quote.QuotedUnitPrice, quote.EstimatedDeliveryDays);
    }

    public async Task<PurchaseRequestDetailDto> TriggerPaymentAsync(Guid id, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);

        // Idempotency guard: only VendorSelected/PaymentFailed can start a payment, so a
        // duplicate click while PaymentInProgress is already true is rejected by Transition().
        var isRetry = request.Status == RequestStatus.PaymentFailed;
        Transition(request, isRetry ? RequestAction.RetryPayment : RequestAction.TriggerPayment, "Payment triggered.");

        if (request.Payment is null)
        {
            // Added via the DbSet directly, not request.Payment = new Payment {...} — see
            // AddHistoryRow for why that matters for a brand-new child of an already-tracked parent.
            var payment = new Payment { PurchaseRequestId = request.Id, Amount = request.TotalAmount };
            _db.Payments.Add(payment);
            request.Payment = payment;
        }
        request.Payment.Status = PaymentStatus.Pending;

        await SaveWithConcurrencyCheckAsync(ct);

        var result = await _paymentGateway.ProcessAsync(request.Id, request.Payment.Amount, ct);

        request.Payment.ProcessedAt = DateTime.UtcNow;
        if (result.Success)
        {
            request.Payment.Status = PaymentStatus.Success;
            request.Payment.TransactionReference = result.TransactionReference;
            Transition(request, RequestAction.PaymentSucceeded, "Payment succeeded.");
            request.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            request.Payment.Status = PaymentStatus.Failed;
            request.Payment.FailureReason = result.FailureReason;
            Transition(request, RequestAction.PaymentFailed, $"Payment failed: {result.FailureReason}");
        }

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    // ---- Payment Page ----------------------------------------------------------------

    public async Task<PurchaseRequestDetailDto> SavePaymentDraftAsync(Guid id, SavePaymentDto dto, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);
        EnsurePaymentEligible(request);

        var payment = GetOrCreatePayment(request);
        payment.Amount = dto.Amount ?? request.TotalAmount;
        if (dto.Method is not null)
        {
            if (!Enum.TryParse<PaymentMethod>(dto.Method, ignoreCase: true, out var method))
            {
                throw new ValidationException($"Unknown payment method '{dto.Method}'.");
            }
            payment.Method = method;
        }
        payment.PaymentReference = dto.PaymentReference;
        payment.PaymentDate = dto.PaymentDate;
        payment.TransactionReference = dto.TransactionReference;
        payment.Notes = dto.Notes;
        payment.Status = PaymentStatus.Draft;
        payment.ModifiedByUserId = _currentUser.Id;
        payment.ModifiedAt = DateTime.UtcNow;

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    /// <summary>The manual counterpart to TriggerPaymentAsync: instead of calling the
    /// simulated payment gateway, the Procurement Admin's own entered details ARE the
    /// result. Advances the procurement through the exact same existing state-machine
    /// transitions (TriggerPayment/RetryPayment, then PaymentSucceeded) — no new workflow.</summary>
    public async Task<PurchaseRequestDetailDto> MarkAsPaidAsync(Guid id, SavePaymentDto dto, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);
        EnsurePaymentEligible(request);

        if (string.IsNullOrWhiteSpace(dto.Method))
        {
            throw new ValidationException("Payment method is required to mark as paid.");
        }
        if (!Enum.TryParse<PaymentMethod>(dto.Method, ignoreCase: true, out var method))
        {
            throw new ValidationException($"Unknown payment method '{dto.Method}'.");
        }
        var amount = dto.Amount ?? request.TotalAmount;
        if (amount <= 0)
        {
            throw new ValidationException("Payment amount must be greater than zero.");
        }

        var payment = GetOrCreatePayment(request);
        payment.Amount = amount;
        payment.Method = method;
        payment.PaymentReference = dto.PaymentReference;
        payment.PaymentDate = dto.PaymentDate ?? DateTime.UtcNow;
        payment.TransactionReference = dto.TransactionReference;
        payment.Notes = dto.Notes;
        payment.Status = PaymentStatus.Success;
        payment.ProcessedAt = DateTime.UtcNow;
        payment.ModifiedByUserId = _currentUser.Id;
        payment.ModifiedAt = DateTime.UtcNow;

        var isRetry = request.Status == RequestStatus.PaymentFailed;
        Transition(request, isRetry ? RequestAction.RetryPayment : RequestAction.TriggerPayment, "Payment recorded manually.");
        Transition(request, RequestAction.PaymentSucceeded, "Marked as paid.");
        request.CompletedAt = DateTime.UtcNow;

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    private Payment GetOrCreatePayment(PurchaseRequest request)
    {
        if (request.Payment is null)
        {
            // See AddHistoryRow for why a brand-new child of an already-tracked parent goes
            // through the DbSet directly rather than request.Payment = new Payment {...}.
            var payment = new Payment { PurchaseRequestId = request.Id, CreatedByUserId = _currentUser.Id };
            _db.Payments.Add(payment);
            request.Payment = payment;
        }
        return request.Payment;
    }

    /// <summary>Payment (draft or mark-as-paid) is only ever available once a vendor has
    /// been selected — i.e. exactly the statuses from which the existing TriggerPayment/
    /// RetryPayment actions are legal. This is what keeps an unsubmitted or not-yet-approved
    /// procurement from ever reaching payment (Section "Do not create a payment for an
    /// unsubmitted procurement" of the requirements).</summary>
    private static void EnsurePaymentEligible(PurchaseRequest request)
    {
        var eligible = RequestStateMachine.CanApply(request.Status, RequestAction.TriggerPayment)
            || RequestStateMachine.CanApply(request.Status, RequestAction.RetryPayment);
        if (!eligible)
        {
            throw new InvalidStateTransitionException(
                $"Payment is not available while the procurement is '{request.Status}'. A vendor must be selected first.");
        }
    }

    // ---- Invoice Page -----------------------------------------------------------------

    public async Task<InvoiceDto> GenerateInvoiceAsync(Guid id, GenerateInvoiceDto dto, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);

        if (request.Payment is null || request.Payment.Status != PaymentStatus.Success)
        {
            throw new InvalidStateTransitionException("An invoice can only be generated once the procurement has been marked as paid.");
        }

        // Idempotent: generating again just returns the existing invoice instead of
        // creating a duplicate (Section 7.10 technical requirements).
        if (request.Invoice is not null)
        {
            return MapToInvoiceDto(request, request.Invoice);
        }

        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";
        var sequence = await _db.Invoices.CountAsync(i => i.InvoiceNumber.StartsWith(prefix), ct) + 1;

        var invoice = new Invoice
        {
            PurchaseRequestId = request.Id,
            PaymentId = request.Payment.Id,
            InvoiceNumber = $"{prefix}{sequence:D6}",
            SubTotal = request.EstimatedTotalCost,
            TaxAmount = request.TaxAmount,
            DiscountAmount = dto.DiscountAmount,
            OtherCharges = dto.OtherCharges,
            GrandTotal = request.EstimatedTotalCost + request.TaxAmount - dto.DiscountAmount + dto.OtherCharges,
            CreatedByUserId = _currentUser.Id,
        };
        _db.Invoices.Add(invoice);
        request.Invoice = invoice;

        await _requests.SaveChangesAsync(ct);
        return MapToInvoiceDto(request, invoice);
    }

    public async Task<InvoiceDto> GetInvoiceAsync(Guid id, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);
        if (request.Invoice is null)
        {
            throw new NotFoundException("No invoice has been generated for this procurement yet.");
        }
        return MapToInvoiceDto(request, request.Invoice);
    }

    private static InvoiceDto MapToInvoiceDto(PurchaseRequest r, Invoice inv) => new(
        inv.Id, inv.InvoiceNumber, inv.InvoiceDate,
        r.RequestNumber, r.Title, r.Department,
        new UserDto(r.Requester!.Id, r.Requester.FullName, r.Requester.Email, r.Requester.Role.ToString(), r.Requester.Department),
        r.Vendor is null ? null : new VendorDto(r.Vendor.Id, r.Vendor.Name, r.Vendor.ContactEmail, r.Vendor.ContactPhone, r.Vendor.TaxId),
        r.Items.OrderBy(i => i.CreatedAt).Select(i => new PurchaseRequestItemDto(
            i.Id, i.ItemId, i.ItemCode, i.Description, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
        inv.SubTotal, inv.TaxAmount, inv.DiscountAmount, inv.OtherCharges, inv.GrandTotal,
        inv.Status.ToString(),
        r.Payment?.PaymentReference, r.Payment?.Method.ToString(), r.Payment?.PaymentDate, r.Payment?.Status.ToString() ?? "Pending");

    public async Task<PurchaseRequestDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);

        // Employees may only see their own requests — everyone else (Manager/Finance/
        // ProcurementAdmin) needs visibility across requests to do their job.
        if (_currentUser.Role == UserRole.Employee.ToString() && request.RequesterId != _currentUser.Id)
        {
            throw new ForbiddenException("You do not own this request.");
        }

        return MapToDetail(request);
    }

    public async Task<List<PurchaseRequestSummaryDto>> GetForCurrentUserAsync(CancellationToken ct = default)
    {
        var requests = await GetVisibleRequestsAsync(ct);
        return requests.Select(MapToSummary).ToList();
    }

    /// <summary>Your Dashboard's status tiles, scoped to the same role-based visibility as
    /// the worklist above (Employee: own requests, Manager: their team's, etc.) — there is
    /// no separate "see everything" permission for any role today, so the dashboard counts
    /// what you can already see, not a system-wide total.</summary>
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default)
    {
        var requests = await GetVisibleRequestsAsync(ct);

        var inProgress = new[]
        {
            RequestStatus.ManagerApproved, RequestStatus.FinanceApproved,
            RequestStatus.VendorSelected, RequestStatus.PaymentInProgress,
        };

        return new DashboardSummaryDto(
            Total: requests.Count,
            Draft: requests.Count(r => r.Status == RequestStatus.Draft),
            Pending: requests.Count(r => r.Status == RequestStatus.Submitted),
            Approved: requests.Count(r => inProgress.Contains(r.Status)),
            Rejected: requests.Count(r => r.Status == RequestStatus.Rejected),
            Completed: requests.Count(r => r.Status == RequestStatus.Completed),
            PaymentFailed: requests.Count(r => r.Status == RequestStatus.PaymentFailed));
    }

    private async Task<List<PurchaseRequest>> GetVisibleRequestsAsync(CancellationToken ct)
    {
        var role = Enum.Parse<UserRole>(_currentUser.Role);

        return role switch
        {
            UserRole.Employee => await _requests.GetByRequesterAsync(_currentUser.Id, ct),
            UserRole.Manager => await _requests.GetPendingManagerApprovalAsync(_currentUser.Id, ct),
            UserRole.Finance => await _requests.GetByStatusAsync(RequestStatus.ManagerApproved, ct),
            UserRole.ProcurementAdmin => await GetProcurementQueueAsync(ct),
            _ => new List<PurchaseRequest>()
        };
    }

    private async Task<List<PurchaseRequest>> GetProcurementQueueAsync(CancellationToken ct)
    {
        var financeApproved = await _requests.GetByStatusAsync(RequestStatus.FinanceApproved, ct);
        var vendorSelected = await _requests.GetByStatusAsync(RequestStatus.VendorSelected, ct);
        var paymentFailed = await _requests.GetByStatusAsync(RequestStatus.PaymentFailed, ct);

        // Plus whatever the Procurement Admin has itself raised via the itemized flow —
        // its own drafts/in-flight requests need to stay visible the same way an
        // Employee's own requests do, in addition to the fulfilment queue above.
        var ownRequests = await _requests.GetByRequesterAsync(_currentUser.Id, ct);

        return financeApproved.Concat(vendorSelected).Concat(paymentFailed).Concat(ownRequests)
            .DistinctBy(r => r.Id)
            .OrderByDescending(r => r.CreatedAt).ToList();
    }

    // ---- helpers -------------------------------------------------------

    private static Category ParseCategory(string category) =>
        Enum.TryParse<Category>(category, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ValidationException($"Unknown category '{category}'.");

    private async Task<PurchaseRequest> LoadOrThrowAsync(Guid id, CancellationToken ct) =>
        await _requests.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Purchase request '{id}' was not found.");

    private void EnsureOwner(PurchaseRequest request)
    {
        if (request.RequesterId != _currentUser.Id)
        {
            throw new ForbiddenException("You do not own this request.");
        }
    }

    private void Transition(PurchaseRequest request, RequestAction action, string note)
    {
        if (!RequestStateMachine.CanApply(request.Status, action))
        {
            throw new InvalidStateTransitionException(
                $"Cannot perform '{action}' while request is '{request.Status}'.");
        }

        var from = request.Status;
        var to = RequestStateMachine.Apply(from, action);
        request.Status = to;
        AddHistoryRow(request.Id, from, to, note);

        // Queued, not dispatched yet — see _pendingEvents and SaveWithConcurrencyCheckAsync.
        if (ActionToNotificationEvent.TryGetValue(action, out var evt))
        {
            _pendingEvents.Add((request, evt, note));
        }
        if (to == RequestStatus.Completed)
        {
            _pendingEvents.Add((request, NotificationEvent.RequestCompleted, note));
        }
    }

    /// <summary>
    /// Adds a history row via the DbContext directly (DbSet.Add) rather than through the
    /// request.History navigation collection. When `request` was loaded by a query (already
    /// tracked as Unchanged) and we hand EF a brand-new child object with a client-assigned
    /// Guid key, change-detection's navigation-fixup path was observed to mark it Modified
    /// instead of Added — EF then emits an UPDATE for a row that doesn't exist yet, which
    /// SQLite's affected-row check reports back as a spurious concurrency conflict.
    /// DbSet.Add() sets EntityState.Added unambiguously, sidestepping that heuristic.
    /// </summary>
    private void AddHistoryRow(Guid requestId, RequestStatus? from, RequestStatus to, string note)
    {
        _db.RequestStatusHistories.Add(new RequestStatusHistory
        {
            PurchaseRequestId = requestId,
            FromStatus = from,
            ToStatus = to,
            ChangedByUserId = _currentUser.Id,
            Notes = note,
        });
    }

    private async Task SaveWithConcurrencyCheckAsync(CancellationToken ct)
    {
        try
        {
            await _requests.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "This request was changed by someone else. Reload and try again.");
        }

        await DispatchPendingEventsAsync(ct);
    }

    /// <summary>Fans each queued transition out to every registered IRequestEventObserver
    /// (Observer pattern) — today that's just NotificationObserver, but this method never
    /// needs to change to add a second one. Dispatched only after the save above succeeds,
    /// so a rolled-back concurrency conflict never produces a phantom notification.</summary>
    private async Task DispatchPendingEventsAsync(CancellationToken ct)
    {
        if (_pendingEvents.Count == 0) return;

        var events = _pendingEvents.ToList();
        _pendingEvents.Clear();

        foreach (var (request, evt, note) in events)
        {
            foreach (var observer in _eventObservers)
            {
                await observer.OnRequestEventAsync(request, evt, note, ct);
            }
        }
    }

    private static PurchaseRequestSummaryDto MapToSummary(PurchaseRequest r) => new(
        r.Id, r.RequestNumber, r.Title, r.Status.ToString(),
        r.Requester?.FullName ?? string.Empty, r.Department, r.EstimatedTotalCost, r.TotalAmount, r.Category.ToString(), r.CreatedAt, r.SubmittedAt);

    private static PurchaseRequestDetailDto MapToDetail(PurchaseRequest r)
    {
        var availableActions = RequestStateMachine.AvailableActions(r.Status).Select(a => a.ToString()).ToList();

        return new PurchaseRequestDetailDto(
            r.Id, r.RequestNumber, r.Title, r.BusinessJustification, r.Department,
            r.EstimatedQuantity, r.EstimatedUnitCost, r.EstimatedTotalCost, r.Status.ToString(),
            new UserDto(r.Requester!.Id, r.Requester.FullName, r.Requester.Email, r.Requester.Role.ToString(), r.Requester.Department),
            r.Vendor is null ? null : new VendorDto(r.Vendor.Id, r.Vendor.Name, r.Vendor.ContactEmail, r.Vendor.ContactPhone, r.Vendor.TaxId),
            r.Payment is null ? null : new PaymentDto(
                r.Payment.Id, r.Payment.Amount, r.Payment.Method.ToString(), r.Payment.Status.ToString(), r.Payment.TransactionReference, r.Payment.FailureReason, r.Payment.ProcessedAt,
                r.Payment.PaymentReference, r.Payment.PaymentDate, r.Payment.Notes),
            r.CreatedAt, r.SubmittedAt, r.ManagerApprovedAt, r.FinanceApprovedAt, r.CompletedAt,
            r.Approvals.OrderBy(a => a.DecidedAt).Select(a => new ApprovalDto(
                a.Stage.ToString(), a.Decision.ToString(), a.Comment, a.Approver?.FullName ?? string.Empty, a.DecidedAt)).ToList(),
            r.History.OrderBy(h => h.ChangedAt).Select(h => new HistoryDto(
                h.FromStatus?.ToString(), h.ToStatus.ToString(), h.ChangedBy?.FullName ?? string.Empty, h.ChangedAt, h.Notes)).ToList(),
            availableActions,
            r.TotalItems,
            r.TotalQuantity,
            r.Items.OrderBy(i => i.CreatedAt).Select(i => new PurchaseRequestItemDto(
                i.Id, i.ItemId, i.ItemCode, i.Description, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
            r.ModifiedByUser?.FullName,
            r.ModifiedAt,
            r.TaxAmount,
            r.TotalAmount,
            r.Invoice is not null,
            r.Category.ToString(),
            VendorRecommendationRules.Recommend(r.Category));
    }
}
