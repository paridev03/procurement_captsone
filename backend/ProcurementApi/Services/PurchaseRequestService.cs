using Microsoft.EntityFrameworkCore;
using ProcurementApi.Data;
using ProcurementApi.Domain.Entities;
using ProcurementApi.Domain.Enums;
using ProcurementApi.DTOs;
using ProcurementApi.Repositories;

namespace ProcurementApi.Services;

public interface IPurchaseRequestService
{
    Task<PurchaseRequestDetailDto> CreateDraftAsync(CreatePurchaseRequestDto dto, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> UpdateDraftAsync(Guid id, UpdatePurchaseRequestDto dto, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> SubmitAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> CancelAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> ManagerDecisionAsync(Guid id, bool approve, string? comment, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> FinanceDecisionAsync(Guid id, bool approve, string? comment, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> SelectVendorAsync(Guid id, Guid vendorId, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> TriggerPaymentAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseRequestDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PurchaseRequestSummaryDto>> GetForCurrentUserAsync(CancellationToken ct = default);
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

    public PurchaseRequestService(
        IPurchaseRequestRepository requests,
        ProcurementDbContext db,
        ICurrentUser currentUser,
        IPaymentGateway paymentGateway)
    {
        _requests = requests;
        _db = db;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
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
            EstimatedQuantity = dto.EstimatedQuantity,
            EstimatedUnitCost = dto.EstimatedUnitCost,
            EstimatedTotalCost = dto.EstimatedQuantity * dto.EstimatedUnitCost,
            Status = RequestStatus.Draft,
        };
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
        request.EstimatedQuantity = dto.EstimatedQuantity;
        request.EstimatedUnitCost = dto.EstimatedUnitCost;
        request.EstimatedTotalCost = dto.EstimatedQuantity * dto.EstimatedUnitCost;

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<PurchaseRequestDetailDto> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);
        EnsureOwner(request);

        Transition(request, RequestAction.Submit, "Submitted for manager approval.");
        request.SubmittedAt = DateTime.UtcNow;

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

        if (approve) request.FinanceApprovedAt = DateTime.UtcNow;

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<PurchaseRequestDetailDto> SelectVendorAsync(Guid id, Guid vendorId, CancellationToken ct = default)
    {
        var request = await LoadOrThrowAsync(id, ct);

        var vendorExists = await _db.Vendors.AnyAsync(v => v.Id == vendorId && v.IsActive, ct);
        if (!vendorExists)
        {
            throw new NotFoundException("Vendor not found or inactive.");
        }

        Transition(request, RequestAction.SelectVendor, "Vendor selected.");
        request.VendorId = vendorId;

        await SaveWithConcurrencyCheckAsync(ct);
        return await GetByIdAsync(id, ct);
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
            var payment = new Payment { PurchaseRequestId = request.Id, Amount = request.EstimatedTotalCost };
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
        var role = Enum.Parse<UserRole>(_currentUser.Role);

        var requests = role switch
        {
            UserRole.Employee => await _requests.GetByRequesterAsync(_currentUser.Id, ct),
            UserRole.Manager => await _requests.GetPendingManagerApprovalAsync(_currentUser.Id, ct),
            UserRole.Finance => await _requests.GetByStatusAsync(RequestStatus.ManagerApproved, ct),
            UserRole.ProcurementAdmin => await GetProcurementQueueAsync(ct),
            _ => new List<PurchaseRequest>()
        };

        return requests.Select(MapToSummary).ToList();
    }

    private async Task<List<PurchaseRequest>> GetProcurementQueueAsync(CancellationToken ct)
    {
        var financeApproved = await _requests.GetByStatusAsync(RequestStatus.FinanceApproved, ct);
        var vendorSelected = await _requests.GetByStatusAsync(RequestStatus.VendorSelected, ct);
        var paymentFailed = await _requests.GetByStatusAsync(RequestStatus.PaymentFailed, ct);
        return financeApproved.Concat(vendorSelected).Concat(paymentFailed)
            .OrderByDescending(r => r.CreatedAt).ToList();
    }

    // ---- helpers -------------------------------------------------------

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
    }

    private static PurchaseRequestSummaryDto MapToSummary(PurchaseRequest r) => new(
        r.Id, r.RequestNumber, r.Title, r.Status.ToString(),
        r.Requester?.FullName ?? string.Empty, r.Department, r.EstimatedTotalCost, r.CreatedAt);

    private static PurchaseRequestDetailDto MapToDetail(PurchaseRequest r)
    {
        var availableActions = RequestStateMachine.AvailableActions(r.Status).Select(a => a.ToString()).ToList();

        return new PurchaseRequestDetailDto(
            r.Id, r.RequestNumber, r.Title, r.BusinessJustification, r.Department,
            r.EstimatedQuantity, r.EstimatedUnitCost, r.EstimatedTotalCost, r.Status.ToString(),
            new UserDto(r.Requester!.Id, r.Requester.FullName, r.Requester.Email, r.Requester.Role.ToString(), r.Requester.Department),
            r.Vendor is null ? null : new VendorDto(r.Vendor.Id, r.Vendor.Name, r.Vendor.ContactEmail, r.Vendor.ContactPhone),
            r.Payment is null ? null : new PaymentDto(r.Payment.Id, r.Payment.Amount, r.Payment.Method.ToString(), r.Payment.Status.ToString(), r.Payment.TransactionReference, r.Payment.FailureReason, r.Payment.ProcessedAt),
            r.CreatedAt, r.SubmittedAt, r.ManagerApprovedAt, r.FinanceApprovedAt, r.CompletedAt,
            r.Approvals.OrderBy(a => a.DecidedAt).Select(a => new ApprovalDto(
                a.Stage.ToString(), a.Decision.ToString(), a.Comment, a.Approver?.FullName ?? string.Empty, a.DecidedAt)).ToList(),
            r.History.OrderBy(h => h.ChangedAt).Select(h => new HistoryDto(
                h.FromStatus?.ToString(), h.ToStatus.ToString(), h.ChangedBy?.FullName ?? string.Empty, h.ChangedAt, h.Notes)).ToList(),
            availableActions);
    }
}
