import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { requestService, itemService } from '../api/requestService';
import { ErrorBanner, Spinner } from '../components/Feedback';
import StatusBadge from '../components/StatusBadge';
import WorkflowStage from '../components/WorkflowStage';
import { CATEGORIES } from '../utils/constants';

const money = (n) =>
  `$${Number(n || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

let tempLineId = 0;
const nextTempId = () => `tmp-${++tempLineId}`;

/// Procurement Admin's itemized purchase-request builder — create (mode="create") and
/// edit (mode="edit") share this one screen. Follows the same Draft/Submit lifecycle as
/// the Employee flow (see RequestForm.jsx) but with a catalog-backed line-item grid
/// instead of one inline quantity/unit cost.
export default function ProcurementBuilder({ mode }) {
  const isEdit = mode === 'edit';
  const { id } = useParams();
  const navigate = useNavigate();

  const [requestId, setRequestId] = useState(isEdit ? id : null);
  const [requestNumber, setRequestNumber] = useState(null);
  const [status, setStatus] = useState('Draft');

  const [title, setTitle] = useState('');
  const [department, setDepartment] = useState('');
  const [category, setCategory] = useState(CATEGORIES[0].value);
  const [justification, setJustification] = useState('');
  const [lines, setLines] = useState([]); // { lineId, itemId, code, name, description, quantity, unitPrice }
  const [taxRate, setTaxRate] = useState('0');

  const [catalog, setCatalog] = useState(null);
  const [query, setQuery] = useState('');
  const [pickerOpen, setPickerOpen] = useState(false);
  const pickerRef = useRef(null);

  const [loading, setLoading] = useState(isEdit);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [busy, setBusy] = useState(false);
  const [confirmSubmitOpen, setConfirmSubmitOpen] = useState(false);

  // ---- load ------------------------------------------------------------

  useEffect(() => {
    itemService.getActive().then(setCatalog).catch((err) => setError(err.message));
  }, []);

  useEffect(() => {
    if (!isEdit) return;
    let cancelled = false;
    requestService
      .getById(id)
      .then((r) => {
        if (cancelled) return;
        if (r.status !== 'Draft') {
          // Read-only once submitted — send the user to the detail view instead.
          navigate(`/requests/${id}`, { replace: true });
          return;
        }
        setRequestId(r.id);
        setRequestNumber(r.requestNumber);
        setStatus(r.status);
        setTitle(r.title);
        setDepartment(r.department);
        setCategory(r.category);
        setJustification(r.businessJustification);
        // TaxRate itself isn't persisted (only the resulting TaxAmount is) — this derives
        // an equivalent rate to show back for editing; the backend recomputes TaxAmount
        // from whatever rate is submitted, so this is a display convenience only.
        setTaxRate(r.estimatedTotalCost > 0 ? String(Math.round((r.taxAmount / r.estimatedTotalCost) * 10000) / 100) : '0');
        setLines(
          r.items.map((i) => ({
            lineId: i.id,
            itemId: i.itemId,
            code: i.itemCode,
            description: i.description,
            quantity: i.quantity,
            unitPrice: i.unitPrice,
          }))
        );
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
    return () => {
      cancelled = true;
    };
  }, [isEdit, id, navigate]);

  // Close the item picker on an outside click.
  useEffect(() => {
    function onDocClick(e) {
      if (pickerRef.current && !pickerRef.current.contains(e.target)) setPickerOpen(false);
    }
    document.addEventListener('mousedown', onDocClick);
    return () => document.removeEventListener('mousedown', onDocClick);
  }, []);

  // ---- derived totals (must match the backend's formula exactly: Σ quantity × unitPrice) --

  const totals = useMemo(() => {
    const totalItems = lines.length;
    const totalQuantity = lines.reduce((sum, l) => sum + (Number(l.quantity) || 0), 0);
    const subTotal = lines.reduce(
      (sum, l) => sum + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0),
      0
    );
    const rate = Number(taxRate) || 0;
    const taxAmount = Math.round(subTotal * rate) / 100;
    const totalAmount = subTotal + taxAmount;
    return { totalItems, totalQuantity, subTotal, taxAmount, totalAmount };
  }, [lines, taxRate]);

  const filteredCatalog = useMemo(() => {
    if (!catalog) return [];
    const q = query.trim().toLowerCase();
    if (!q) return catalog;
    return catalog.filter(
      (i) => i.name.toLowerCase().includes(q) || i.code.toLowerCase().includes(q)
    );
  }, [catalog, query]);

  // ---- editing -----------------------------------------------------------

  function addItem(catalogItem) {
    setLines((prev) => [
      ...prev,
      {
        lineId: nextTempId(),
        itemId: catalogItem.id,
        code: catalogItem.code,
        description: catalogItem.description,
        quantity: 1,
        unitPrice: catalogItem.unitPrice,
      },
    ]);
    setQuery('');
    setPickerOpen(false);
  }

  function updateLine(lineId, patch) {
    setLines((prev) => prev.map((l) => (l.lineId === lineId ? { ...l, ...patch } : l)));
  }

  function removeLine(lineId) {
    setLines((prev) => prev.filter((l) => l.lineId !== lineId));
  }

  // ---- validation (mirrors the backend's own checks — server is still authoritative) ----

  function validate() {
    if (!title.trim()) return 'Title is required.';
    if (!department.trim()) return 'Department is required.';
    if (!justification.trim()) return 'Business justification is required.';
    if (lines.length === 0) return 'Add at least one item.';
    for (const l of lines) {
      if (!l.quantity || l.quantity < 1) return 'Every line needs a quantity of at least 1.';
      if (!l.unitPrice || l.unitPrice <= 0) return 'Every line needs a unit price greater than 0.';
    }
    return '';
  }

  function buildDto() {
    return {
      title,
      businessJustification: justification,
      department,
      category,
      taxRate: Number(taxRate) || 0,
      items: lines.map((l) => ({
        id: typeof l.lineId === 'string' && l.lineId.startsWith('tmp-') ? null : l.lineId,
        itemId: l.itemId,
        description: l.description,
        quantity: Number(l.quantity),
        unitPrice: Number(l.unitPrice),
      })),
    };
  }

  function applyServerResult(r) {
    setRequestId(r.id);
    setRequestNumber(r.requestNumber);
    setStatus(r.status);
    setLines(
      r.items.map((i) => ({
        lineId: i.id,
        itemId: i.itemId,
        code: i.itemCode,
        description: i.description,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
      }))
    );
  }

  async function handleSaveDraft() {
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }
    setError('');
    setSuccess('');
    setBusy(true);
    try {
      const dto = buildDto();
      const result = requestId
        ? await requestService.updateItemized(requestId, dto)
        : await requestService.createItemized(dto);
      applyServerResult(result);
      setSuccess('Draft saved.');
      if (!isEdit) {
        // Prevents a duplicate record on a second "Save as Draft" click — from here on,
        // saves go through the update-in-place path.
        navigate(`/procurement/${result.id}/edit`, { replace: true });
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function handleSubmit() {
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }
    setConfirmSubmitOpen(true);
  }

  async function confirmSubmit() {
    setConfirmSubmitOpen(false);
    setError('');
    setSuccess('');
    setBusy(true);
    try {
      const dto = buildDto();
      const saved = requestId
        ? await requestService.updateItemized(requestId, dto)
        : await requestService.createItemized(dto);
      await requestService.submit(saved.id);
      navigate(`/requests/${saved.id}`, {
        state: { flash: `${saved.requestNumber} submitted for manager approval.` },
      });
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  if (loading) return <Spinner />;

  return (
    <div className="procurement-builder">
      <Link to="/requests" className="back-link">
        ← Back to worklist
      </Link>

      <div className="pb-header">
        <div>
          <h1>{isEdit ? 'Edit Procurement Request' : 'New Procurement Request'}</h1>
          <p className="pb-subhead">
            {requestNumber ? <span className="pb-reqnum">{requestNumber}</span> : 'Not yet saved'}
            {' · '}
            <StatusBadge status={status} />
          </p>
        </div>
      </div>

      <WorkflowStage procurementStatus={status} paymentStatus={null} hasInvoice={false} />

      <ErrorBanner message={error} />
      {success && <div className="success-banner">{success}</div>}

      {/* ---- summary cards ---- */}
      <div className="summary-cards">
        <div className="summary-card">
          <span className="summary-card__icon" aria-hidden="true">📦</span>
          <div>
            <span className="summary-card__label">Total Items</span>
            <span className="summary-card__value">{totals.totalItems}</span>
          </div>
        </div>
        <div className="summary-card">
          <span className="summary-card__icon" aria-hidden="true">🔢</span>
          <div>
            <span className="summary-card__label">Total Quantity</span>
            <span className="summary-card__value">{totals.totalQuantity}</span>
          </div>
        </div>
        <div className="summary-card summary-card--accent">
          <span className="summary-card__icon" aria-hidden="true">💰</span>
          <div>
            <span className="summary-card__label">Total Amount</span>
            <span className="summary-card__value">{money(totals.totalAmount)}</span>
          </div>
        </div>
      </div>

      {/* ---- header fields ---- */}
      <div className="card">
        <h2>Request details</h2>
        <div className="form-field">
          <label htmlFor="title">Title</label>
          <input id="title" value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} />
        </div>
        <div className="form-row">
          <div className="form-field">
            <label htmlFor="department">Department</label>
            <input id="department" value={department} onChange={(e) => setDepartment(e.target.value)} maxLength={100} />
          </div>
          <div className="form-field">
            <label htmlFor="category">Category</label>
            <select id="category" value={category} onChange={(e) => setCategory(e.target.value)}>
              {CATEGORIES.map((c) => (
                <option key={c.value} value={c.value}>{c.label}</option>
              ))}
            </select>
          </div>
        </div>
        <div className="form-field">
          <label htmlFor="justification">Business justification</label>
          <textarea
            id="justification"
            rows={3}
            maxLength={2000}
            value={justification}
            onChange={(e) => setJustification(e.target.value)}
          />
        </div>
      </div>

      {/* ---- item selector + grid ---- */}
      <div className="card">
        <h2>Items</h2>

        <div className="item-picker" ref={pickerRef}>
          <input
            type="text"
            placeholder="Search item or material by name / code…"
            value={query}
            onFocus={() => setPickerOpen(true)}
            onChange={(e) => {
              setQuery(e.target.value);
              setPickerOpen(true);
            }}
          />
          {pickerOpen && catalog && (
            <div className="item-picker__menu">
              {filteredCatalog.length === 0 ? (
                <div className="item-picker__empty">No matching items.</div>
              ) : (
                filteredCatalog.map((i) => (
                  <button
                    type="button"
                    key={i.id}
                    className="item-picker__option"
                    onClick={() => addItem(i)}
                  >
                    <span className="item-picker__code">{i.code}</span>
                    <span className="item-picker__name">{i.name}</span>
                    <span className="item-picker__price">{money(i.unitPrice)}</span>
                  </button>
                ))
              )}
            </div>
          )}
        </div>

        {lines.length === 0 ? (
          <div className="state-block" style={{ marginTop: 16 }}>
            No items yet — search above to add one.
          </div>
        ) : (
          <div className="table-wrap" style={{ marginTop: 16 }}>
            <table className="table item-grid">
              <thead>
                <tr>
                  <th>Item Code</th>
                  <th>Description</th>
                  <th>Quantity</th>
                  <th>Unit Price</th>
                  <th>Total Price</th>
                  <th aria-label="Remove" />
                </tr>
              </thead>
              <tbody>
                {lines.map((l) => (
                  <tr key={l.lineId}>
                    <td>
                      <span className="item-grid__code">{l.code}</span>
                    </td>
                    <td>
                      <input
                        className="item-grid__input"
                        value={l.description}
                        onChange={(e) => updateLine(l.lineId, { description: e.target.value })}
                      />
                    </td>
                    <td>
                      <input
                        className="item-grid__input item-grid__input--num"
                        type="number"
                        min={1}
                        step={1}
                        value={l.quantity}
                        onChange={(e) => updateLine(l.lineId, { quantity: e.target.value })}
                      />
                    </td>
                    <td>
                      <input
                        className="item-grid__input item-grid__input--num"
                        type="number"
                        min={0.01}
                        step={0.01}
                        value={l.unitPrice}
                        onChange={(e) => updateLine(l.lineId, { unitPrice: e.target.value })}
                      />
                    </td>
                    <td className="item-grid__total">
                      {money((Number(l.quantity) || 0) * (Number(l.unitPrice) || 0))}
                    </td>
                    <td>
                      <button
                        type="button"
                        className="item-grid__remove"
                        title="Remove item"
                        onClick={() => removeLine(l.lineId)}
                      >
                        ✕
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="tax-row">
          <div className="form-field" style={{ maxWidth: 160 }}>
            <label htmlFor="taxRate">Tax rate (%)</label>
            <input
              id="taxRate"
              type="number"
              min={0}
              max={100}
              step={0.01}
              value={taxRate}
              onChange={(e) => setTaxRate(e.target.value)}
            />
          </div>
          <div className="tax-row__breakdown">
            <div><span>Subtotal</span><span>{money(totals.subTotal)}</span></div>
            <div><span>Tax</span><span>{money(totals.taxAmount)}</span></div>
            <div className="tax-row__total"><span>Total Amount</span><span>{money(totals.totalAmount)}</span></div>
          </div>
        </div>
      </div>

      {/* ---- bottom actions ---- */}
      <div className="pb-actions">
        <button type="button" className="btn btn--ghost" onClick={() => navigate('/requests')} disabled={busy}>
          Cancel
        </button>
        <div className="pb-actions__right">
          <button type="button" className="btn btn--ghost" onClick={handleSaveDraft} disabled={busy}>
            {busy ? 'Saving…' : 'Save as Draft'}
          </button>
          <button type="button" className="btn" onClick={handleSubmit} disabled={busy}>
            {busy ? 'Submitting…' : 'Submit'}
          </button>
        </div>
      </div>

      {confirmSubmitOpen && (
        <div className="modal-backdrop" onClick={() => setConfirmSubmitOpen(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h2>Submit procurement?</h2>
            <p>
              Submit {title || 'this request'} for {money(totals.totalAmount)} ({totals.totalItems} items)? It will move to
              Manager approval and can no longer be edited.
            </p>
            <div className="btn-row" style={{ justifyContent: 'flex-end' }}>
              <button type="button" className="btn btn--ghost" onClick={() => setConfirmSubmitOpen(false)}>
                Cancel
              </button>
              <button type="button" className="btn" onClick={confirmSubmit}>
                Yes, submit
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
