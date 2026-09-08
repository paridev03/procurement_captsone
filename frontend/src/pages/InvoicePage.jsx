import { useEffect, useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { requestService, invoiceService } from '../api/requestService';
import { ErrorBanner, Spinner } from '../components/Feedback';
import StatusBadge from '../components/StatusBadge';
import WorkflowStage from '../components/WorkflowStage';

const money = (n) => `$${Number(n || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
const when = (d) => (d ? new Date(d).toLocaleDateString() : '—');

const COMPANY = {
  name: 'Procurement Platform Inc.',
  address: '1 Market Street, Suite 400, San Francisco, CA 94105',
};

export default function InvoicePage() {
  const { id } = useParams();
  const location = useLocation();

  const [request, setRequest] = useState(null);
  const [invoice, setInvoice] = useState(null);
  const [notFound, setNotFound] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [discount, setDiscount] = useState('0');
  const [otherCharges, setOtherCharges] = useState('0');

  useEffect(() => {
    let cancelled = false;
    requestService
      .getById(id)
      .then((r) => {
        if (cancelled) return;
        setRequest(r);
      })
      .catch((err) => setError(err.message));

    invoiceService
      .get(id)
      .then((inv) => {
        if (!cancelled) setInvoice(inv);
      })
      .catch((err) => {
        if (cancelled) return;
        // 404 just means "not generated yet" — not a real error for this page.
        if (err.message?.toLowerCase().includes('no invoice')) setNotFound(true);
        else setError(err.message);
      });

    return () => {
      cancelled = true;
    };
  }, [id]);

  if (!request && !error) return <Spinner />;
  if (!request) return <ErrorBanner message={error} />;

  const canGenerate = request.payment?.status === 'Success';

  async function handleGenerate() {
    setError('');
    setBusy(true);
    try {
      const inv = await invoiceService.generate(id, {
        discountAmount: Number(discount) || 0,
        otherCharges: Number(otherCharges) || 0,
      });
      setInvoice(inv);
      setNotFound(false);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  function handlePrint() {
    window.print();
  }

  return (
    <div className="invoice-page">
      <div className="no-print">
        <Link to={`/requests/${id}`} className="back-link">
          ← Back to procurement
        </Link>

        <div className="page-header">
          <div>
            <h1>Invoice</h1>
            <p>{request.requestNumber} — {request.title}</p>
          </div>
        </div>

        <WorkflowStage
          procurementStatus={request.status}
          paymentStatus={request.payment?.status}
          hasInvoice={!!invoice}
        />

        <ErrorBanner message={error} />
        {location.state?.flash && !invoice && <div className="success-banner">{location.state.flash}</div>}
      </div>

      {!canGenerate && (
        <div className="state-block no-print">
          An invoice can only be generated once this procurement has been marked as paid.{' '}
          <Link to={`/payment/${id}`}>Go to Payment</Link>
        </div>
      )}

      {canGenerate && notFound && !invoice && (
        <div className="card no-print">
          <h2>Generate invoice</h2>
          <p style={{ fontSize: 13, marginBottom: 12 }}>
            The invoice will use this procurement's items and totals automatically — add any
            discount or other charges below if applicable.
          </p>
          <div className="form-row">
            <div className="form-field">
              <label htmlFor="discount">Discount amount</label>
              <input id="discount" type="number" min={0} step={0.01} value={discount} onChange={(e) => setDiscount(e.target.value)} />
            </div>
            <div className="form-field">
              <label htmlFor="otherCharges">Other charges</label>
              <input id="otherCharges" type="number" min={0} step={0.01} value={otherCharges} onChange={(e) => setOtherCharges(e.target.value)} />
            </div>
          </div>
          <div className="btn-row">
            <button type="button" className="btn" onClick={handleGenerate} disabled={busy}>
              {busy ? 'Generating…' : 'Generate Invoice'}
            </button>
          </div>
        </div>
      )}

      {invoice && (
        <div className="card invoice-doc">
          <div className="invoice-doc__header">
            <div>
              <h2 style={{ marginBottom: 2 }}>{COMPANY.name}</h2>
              <p style={{ fontSize: 12 }}>{COMPANY.address}</p>
            </div>
            <div className="invoice-doc__meta">
              <div><span className="label">Invoice #</span> <strong>{invoice.invoiceNumber}</strong></div>
              <div><span className="label">Invoice date</span> {when(invoice.invoiceDate)}</div>
              <div><span className="label">Procurement #</span> {invoice.procurementNumber}</div>
              <div><span className="label">Payment ref</span> {invoice.paymentReference || '—'}</div>
              <div><span className="label">Payment status</span> <StatusBadge status={invoice.paymentStatus === 'Success' ? 'Completed' : invoice.paymentStatus} /></div>
            </div>
          </div>

          <div className="invoice-doc__section">
            <h3>Supplier</h3>
            {invoice.vendor ? (
              <div className="detail-grid">
                <div className="detail-item">
                  <span className="label">Name</span>
                  <span className="value">{invoice.vendor.name}</span>
                </div>
                <div className="detail-item">
                  <span className="label">Contact</span>
                  <span className="value">{invoice.vendor.contactEmail} · {invoice.vendor.contactPhone}</span>
                </div>
                <div className="detail-item">
                  <span className="label">Tax / GST</span>
                  <span className="value">{invoice.vendor.taxId || '—'}</span>
                </div>
              </div>
            ) : (
              <p style={{ fontSize: 13 }}>No vendor on record.</p>
            )}
          </div>

          <div className="invoice-doc__section">
            <h3>Items</h3>
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Item</th>
                    <th>Description</th>
                    <th>Quantity</th>
                    <th>Unit Price</th>
                    <th>Total</th>
                  </tr>
                </thead>
                <tbody>
                  {invoice.items.map((i) => (
                    <tr key={i.id}>
                      <td>{i.itemCode}</td>
                      <td>{i.description}</td>
                      <td>{i.quantity}</td>
                      <td>{money(i.unitPrice)}</td>
                      <td>{money(i.totalPrice)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="invoice-doc__totals">
            <div><span>Subtotal</span><span>{money(invoice.subTotal)}</span></div>
            <div><span>Tax / GST</span><span>{money(invoice.taxAmount)}</span></div>
            {invoice.discountAmount > 0 && <div><span>Discount</span><span>-{money(invoice.discountAmount)}</span></div>}
            {invoice.otherCharges > 0 && <div><span>Other charges</span><span>{money(invoice.otherCharges)}</span></div>}
            <div className="invoice-doc__grand"><span>Grand Total</span><span>{money(invoice.grandTotal)}</span></div>
          </div>

          <div className="btn-row no-print" style={{ marginTop: 20 }}>
            <button type="button" className="btn" onClick={handlePrint}>
              Download / Print Invoice
            </button>
            <Link to={`/requests/${id}`} className="btn btn--ghost">
              Back to Procurement
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
