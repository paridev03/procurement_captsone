using ProcurementApi.Domain.Entities;
using ProcurementApi.Domain.Enums;

namespace ProcurementApi.Services;

/// <summary>
/// Observer pattern: PurchaseRequestService raises "this request just changed" without
/// knowing or caring what (if anything) reacts to it — it depends only on this interface
/// (DIP), never on NotificationObserver or any other concrete reactor.
///
/// Today there's exactly one implementation (NotificationObserver). Adding a second
/// reactor — an audit-log observer, an email observer, a webhook observer — is purely
/// additive: register another IRequestEventObserver in Program.cs and it starts receiving
/// every event alongside the existing one, with zero changes to PurchaseRequestService or
/// to any other observer (OCP).
/// </summary>
public interface IRequestEventObserver
{
    Task OnRequestEventAsync(PurchaseRequest request, NotificationEvent evt, string note, CancellationToken ct = default);
}
