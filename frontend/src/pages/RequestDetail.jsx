import { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { requestService, vendorService } from '../api/requestService';
import { useAuth } from '../context/AuthContext';
import StatusBadge from '../components/StatusBadge';
import { ErrorBanner, Spinner } from '../components/Feedback';

const money = (n) => `$${Number(n).toLocaleString(undefined, { minimumFractionDigits: 2 })}`;
const when = (d) => (d ? new Date(d).toLocaleString() : '—');

export default function RequestDetail() {
  const { id } = useParams();
  const { user } = useAuth();

  const [request, setRequest] = useState(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [comment, setComment] = useState('');
  const [vendors, setVendors] = useState(null);
  const [vendorId, setVendorId] = useState('');

  const load = useCallback(() => {
    return requestService
      .getById(id)
      .then(setRequest)
      .catch((err) => setError(err.message));
  }, [id]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    if (
      user.role === 'ProcurementAdmin' &&
      request?.availableActions.includes('SelectVendor') &&
      vendors === null
    ) {
      vendorService.getActive().then(setVendors).catch((err) => setError(err.message));
    }
  }, [user.role, request, vendors]);

  if (!request && !error) return <Spinner />;
  if (!request) return <ErrorBanner message={error} />;

  const isOwner = request.requester.id === user.id;
  const can = (action) => request.availableActions.includes(action);

  async function act(action, fn) {
    setBusy(true);
    setError('');
    try {
      const updated = await fn();
      setRequest(updated);
      setComment('');
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div>
      <Link to="/" className="back-link">
        ← Back to worklist
      </Link>

      <div className="page-header">
        <div>
          <h1>{request.requestNumber} — {request.title}</h1>
          <p><StatusBadge status={request.status} /></p>
        </div>
        {user.role === 'Employee' && isOwner && request.status === 'Draft' && (
          <Link to={`/requests/${request.id}/edit`} className="btn btn--ghost">
            Edit
          </Link>
        )}
      </div>

      <ErrorBanner message={error} />

      <div className="card">
        <h2>Details</h2>
        <div className="detail-grid">
          <div className="detail-item">
            <span className="label">Requester</span>
            <span className="value">{request.requester.fullName}</span>
          </div>
          <div className="detail-item">
            <span className="label">Department</span>
            <span className="value">{request.department}</span>
          </div>
          <div className="detail-item">
            <span className="label">Quantity</span>
            <span className="value">{request.estimatedQuantity}</span>
          </div>
          <div className="detail-item">
            <span className="label">Unit cost</span>
            <span className="value">{money(request.estimatedUnitCost)}</span>
          </div>
          <div className="detail-item">
            <span className="label">Total cost</span>
            <span className="value">{money(request.estimatedTotalCost)}</span>
          </div>
          <div className="detail-item">
            <span className="label">Created</span>
            <span className="value">{when(request.createdAt)}</span>
          </div>
        </div>
        <p style={{ marginTop: 14, fontSize: 13 }}>{request.businessJustification}</p>
      </div>

      {(request.vendor || request.payment) && (
        <div className="card">
          <h2>Fulfilment</h2>
          <div className="detail-grid">
            {request.vendor && (
              <div className="detail-item">
                <span className="label">Vendor</span>
                <span className="value">{request.vendor.name}</span>
              </div>
            )}
            {request.payment && (
              <>
                <div className="detail-item">
                  <span className="label">Payment status</span>
                  <span className="value">{request.payment.status}</span>
                </div>
                {request.payment.failureReason && (
                  <div className="detail-item">
                    <span className="label">Failure reason</span>
                    <span className="value">{request.payment.failureReason}</span>
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      )}

      {/* ---- role-scoped actions ------------------------------------ */}
      {user.role === 'Employee' && isOwner && (can('Submit') || can('Cancel')) && (
        <div className="card">
          <h2>Actions</h2>
          <div className="btn-row">
            {can('Submit') && (
              <button className="btn" disabled={busy} onClick={() => act('Submit', () => requestService.submit(request.id))}>
                Submit for approval
              </button>
            )}
            {can('Cancel') && (
              <button className="btn btn--danger" disabled={busy} onClick={() => act('Cancel', () => requestService.cancel(request.id))}>
                Cancel
              </button>
            )}
          </div>
        </div>
      )}

      {user.role === 'Manager' && (can('ManagerApprove') || can('ManagerReject')) && (
        <div className="card">
          <h2>Manager decision</h2>
          <div className="form-field">
            <label htmlFor="comment">Comment (optional)</label>
            <textarea id="comment" rows={3} value={comment} onChange={(e) => setComment(e.target.value)} />
          </div>
          <div className="btn-row">
            <button className="btn" disabled={busy} onClick={() => act('ManagerApprove', () => requestService.managerDecision(request.id, true, comment))}>
              Approve
            </button>
            <button className="btn btn--danger" disabled={busy} onClick={() => act('ManagerReject', () => requestService.managerDecision(request.id, false, comment))}>
              Reject
            </button>
          </div>
        </div>
      )}

      {user.role === 'Finance' && (can('FinanceApprove') || can('FinanceReject')) && (
        <div className="card">
          <h2>Finance decision</h2>
          <div className="form-field">
            <label htmlFor="comment">Comment (optional)</label>
            <textarea id="comment" rows={3} value={comment} onChange={(e) => setComment(e.target.value)} />
          </div>
          <div className="btn-row">
            <button className="btn" disabled={busy} onClick={() => act('FinanceApprove', () => requestService.financeDecision(request.id, true, comment))}>
              Approve budget
            </button>
            <button className="btn btn--danger" disabled={busy} onClick={() => act('FinanceReject', () => requestService.financeDecision(request.id, false, comment))}>
              Reject
            </button>
          </div>
        </div>
      )}

      {user.role === 'ProcurementAdmin' && can('SelectVendor') && (
        <div className="card">
          <h2>Select vendor</h2>
          {vendors === null ? (
            <Spinner />
          ) : (
            <>
              <div className="form-field">
                <label htmlFor="vendor">Vendor</label>
                <select id="vendor" value={vendorId} onChange={(e) => setVendorId(e.target.value)}>
                  <option value="">Choose a vendor…</option>
                  {vendors.map((v) => (
                    <option key={v.id} value={v.id}>
                      {v.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="btn-row">
                <button
                  className="btn"
                  disabled={busy || !vendorId}
                  onClick={() => act('SelectVendor', () => requestService.selectVendor(request.id, vendorId))}
                >
                  Select vendor
                </button>
              </div>
            </>
          )}
        </div>
      )}

      {user.role === 'ProcurementAdmin' && (can('TriggerPayment') || can('RetryPayment')) && (
        <div className="card">
          <h2>Payment</h2>
          <div className="btn-row">
            <button className="btn" disabled={busy} onClick={() => act('TriggerPayment', () => requestService.triggerPayment(request.id))}>
              {can('RetryPayment') ? 'Retry payment' : 'Trigger payment'}
            </button>
          </div>
        </div>
      )}

      <div className="card">
        <h2>History</h2>
        {request.history.length === 0 ? (
          <p style={{ fontSize: 13 }}>No history yet.</p>
        ) : (
          <ul className="timeline">
            {request.history.map((h, i) => (
              <li key={i}>
                {h.fromStatus ? `${h.fromStatus} → ${h.toStatus}` : h.toStatus}
                {h.notes && ` — ${h.notes}`}
                <div className="meta">
                  {h.changedByName} · {when(h.changedAt)}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      {request.approvals.length > 0 && (
        <div className="card">
          <h2>Approvals</h2>
          <ul className="timeline">
            {request.approvals.map((a, i) => (
              <li key={i}>
                {a.stage}: {a.decision}
                {a.comment && ` — "${a.comment}"`}
                <div className="meta">
                  {a.approverName} · {when(a.decidedAt)}
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
