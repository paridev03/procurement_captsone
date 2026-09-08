import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { requestService, paymentService } from '../api/requestService';
import { ErrorBanner, Spinner } from '../components/Feedback';
import StatusBadge from '../components/StatusBadge';
import WorkflowStage from '../components/WorkflowStage';

const money = (n) => `$${Number(n || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
const when = (d) => (d ? new Date(d).toLocaleString() : '—');
const toDateInput = (d) => (d ? new Date(d).toISOString().slice(0, 10) : '');

const METHODS = ['BankTransfer', 'UPI', 'Cheque', 'Cash', 'Other'];
const METHOD_LABELS = { BankTransfer: 'Bank Transfer', UPI: 'UPI', Cheque: 'Cheque', Cash: 'Cash', Other: 'Other' };

// Statuses from which the backend allows payment at all (mirrors
// PurchaseRequestService.EnsurePaymentEligible on the server — this is a UX pre-check, not
// the authority: the server re-validates independently).
const PAYMENT_ELIGIBLE = ['VendorSelected', 'PaymentInProgress', 'PaymentFailed', 'Completed'];

export default function PaymentPage() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [request, setRequest] = useState(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [busy, setBusy] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);

  const [form, setForm] = useState({
    paymentReference: '',
    paymentDate: toDateInput(new Date()),
    method: 'BankTransfer',
    amount: '',
    transactionReference: '',
    notes: '',
  });

  useEffect(() => {
    let cancelled = false;
    requestService
      .getById(id)
      .then((r) => {
        if (cancelled) return;
        setRequest(r);
        const p = r.payment;
        setForm({
          paymentReference: p?.paymentReference || '',
          paymentDate: toDateInput(p?.paymentDate) || toDateInput(new Date()),
          method: p?.method || 'BankTransfer',
          amount: p?.amount ?? r.totalAmount,
          transactionReference: p?.transactionReference || '',
          notes: p?.notes || '',
        });
      })
      .catch((err) => setError(err.message));
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (!request && !error) return <Spinner />;
  if (!request) return <ErrorBanner message={error} />;

  const eligible = PAYMENT_ELIGIBLE.includes(request.status);
  const alreadyPaid = request.payment?.status === 'Success';

  function update(field, value) {
    setForm((f) => ({ ...f, [field]: value }));
  }

  function buildDto() {
    return {
      paymentReference: form.paymentReference || null,
      paymentDate: form.paymentDate ? new Date(form.paymentDate).toISOString() : null,
      method: form.method || null,
      amount: form.amount === '' ? null : Number(form.amount),
      transactionReference: form.transactionReference || null,
      notes: form.notes || null,
    };
  }

  async function handleSaveDraft() {
    setError('');
    setSuccess('');
    setBusy(true);
    try {
      const updated = await paymentService.saveDraft(id, buildDto());
      setRequest(updated);
      setSuccess('Payment saved as draft.');
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function handleMarkAsPaid() {
    setConfirmOpen(false);
    if (!form.method) {
      setError('Payment method is required.');
      return;
    }
    if (!form.amount || Number(form.amount) <= 0) {
      setError('Payment amount must be greater than zero.');
      return;
    }
    setError('');
    setSuccess('');
    setBusy(true);
    try {
      const updated = await paymentService.markAsPaid(id, buildDto());
      setRequest(updated);
      navigate(`/invoice/${id}`, { state: { flash: `Payment recorded for ${updated.requestNumber}. Ready to generate the invoice.` } });
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div>
      <Link to={`/requests/${id}`} className="back-link">
        ← Back to procurement
      </Link>

      <div className="page-header">
        <div>
          <h1>Payment</h1>
          <p>{request.requestNumber} — {request.title}</p>
        </div>
      </div>

      <WorkflowStage
        procurementStatus={request.status}
        paymentStatus={request.payment?.status}
        hasInvoice={request.hasInvoice}
      />

      <ErrorBanner message={error} />
      {success && <div className="success-banner">{success}</div>}

      {!eligible && (
        <div className="state-block">
          Payment isn't available yet — a vendor must be selected for this procurement first.
        </div>
      )}

      {eligible && (
        <>
          <div className="card">
            <h2>Procurement summary</h2>
            <div className="detail-grid">
              <div className="detail-item">
                <span className="label">Procurement number</span>
                <span className="value">{request.requestNumber}</span>
              </div>
              <div className="detail-item">
                <span className="label">Supplier / Vendor</span>
                <span className="value">{request.vendor?.name || '—'}</span>
              </div>
              <div className="detail-item">
                <span className="label">Procurement date</span>
                <span className="value">{when(request.createdAt)}</span>
              </div>
              <div className="detail-item">
                <span className="label">Total items</span>
                <span className="value">{request.totalItems || '—'}</span>
              </div>
              <div className="detail-item">
                <span className="label">Total quantity</span>
                <span className="value">{request.totalQuantity || '—'}</span>
              </div>
              <div className="detail-item">
                <span className="label">Total amount</span>
                <span className="value">{money(request.totalAmount)}</span>
              </div>
              <div className="detail-item">
                <span className="label">Status</span>
                <span className="value"><StatusBadge status={request.status} /></span>
              </div>
            </div>
          </div>

          <div className="card">
            <h2>Payment details</h2>
            {alreadyPaid && (
              <div className="success-banner">This procurement has already been marked as paid.</div>
            )}
            <div className="form-row">
              <div className="form-field">
                <label htmlFor="paymentReference">Payment reference number</label>
                <input
                  id="paymentReference"
                  value={form.paymentReference}
                  onChange={(e) => update('paymentReference', e.target.value)}
                  disabled={alreadyPaid}
                />
              </div>
              <div className="form-field">
                <label htmlFor="paymentDate">Payment date</label>
                <input
                  id="paymentDate"
                  type="date"
                  value={form.paymentDate}
                  onChange={(e) => update('paymentDate', e.target.value)}
                  disabled={alreadyPaid}
                />
              </div>
            </div>
            <div className="form-row">
              <div className="form-field">
                <label htmlFor="method">Payment method</label>
                <select id="method" value={form.method} onChange={(e) => update('method', e.target.value)} disabled={alreadyPaid}>
                  {METHODS.map((m) => (
                    <option key={m} value={m}>{METHOD_LABELS[m]}</option>
                  ))}
                </select>
              </div>
              <div className="form-field">
                <label htmlFor="amount">Payment amount</label>
                <input
                  id="amount"
                  type="number"
                  min={0.01}
                  step={0.01}
                  value={form.amount}
                  onChange={(e) => update('amount', e.target.value)}
                  disabled={alreadyPaid}
                />
              </div>
            </div>
            <div className="form-field">
              <label htmlFor="transactionReference">Transaction / reference ID</label>
              <input
                id="transactionReference"
                value={form.transactionReference}
                onChange={(e) => update('transactionReference', e.target.value)}
                disabled={alreadyPaid}
              />
            </div>
            <div className="form-field">
              <label htmlFor="notes">Payment notes</label>
              <textarea
                id="notes"
                rows={3}
                value={form.notes}
                onChange={(e) => update('notes', e.target.value)}
                disabled={alreadyPaid}
              />
            </div>

            {request.payment && (
              <p style={{ fontSize: 12 }}>
                Current status: <StatusBadge status={request.payment.status === 'Success' ? 'Completed' : request.payment.status} />
              </p>
            )}
          </div>

          <div className="pb-actions">
            <button type="button" className="btn btn--ghost" onClick={() => navigate(`/requests/${id}`)} disabled={busy}>
              Cancel
            </button>
            <div className="pb-actions__right">
              {!alreadyPaid && (
                <>
                  <button type="button" className="btn btn--ghost" onClick={handleSaveDraft} disabled={busy}>
                    {busy ? 'Saving…' : 'Save as Draft'}
                  </button>
                  <button type="button" className="btn" onClick={() => setConfirmOpen(true)} disabled={busy}>
                    Mark as Paid
                  </button>
                </>
              )}
              {alreadyPaid && (
                <Link to={`/invoice/${id}`} className="btn">
                  Go to Invoice
                </Link>
              )}
            </div>
          </div>
        </>
      )}

      {confirmOpen && (
        <div className="modal-backdrop" onClick={() => setConfirmOpen(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Confirm payment</h2>
            <p>
              Mark {request.requestNumber} as paid for {money(form.amount || request.totalAmount)} via{' '}
              {METHOD_LABELS[form.method] || form.method}? This cannot be undone from here.
            </p>
            <div className="btn-row" style={{ justifyContent: 'flex-end' }}>
              <button type="button" className="btn btn--ghost" onClick={() => setConfirmOpen(false)}>
                Cancel
              </button>
              <button type="button" className="btn" onClick={handleMarkAsPaid}>
                Yes, mark as paid
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
