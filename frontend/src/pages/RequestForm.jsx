import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { requestService } from '../api/requestService';
import { ErrorBanner, Spinner } from '../components/Feedback';

const emptyForm = {
  title: '',
  businessJustification: '',
  department: '',
  estimatedQuantity: 1,
  estimatedUnitCost: '',
};

export default function RequestForm({ mode }) {
  const isEdit = mode === 'edit';
  const { id } = useParams();
  const navigate = useNavigate();

  const [form, setForm] = useState(emptyForm);
  const [loading, setLoading] = useState(isEdit);
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!isEdit) return;
    let cancelled = false;
    requestService
      .getById(id)
      .then((r) => {
        if (cancelled) return;
        if (r.status !== 'Draft') {
          setError('Only draft requests can be edited.');
          return;
        }
        setForm({
          title: r.title,
          businessJustification: r.businessJustification,
          department: r.department,
          estimatedQuantity: r.estimatedQuantity,
          estimatedUnitCost: r.estimatedUnitCost,
        });
      })
      .catch((err) => !cancelled && setError(err.message))
      .finally(() => !cancelled && setLoading(false));
    return () => {
      cancelled = true;
    };
  }, [isEdit, id]);

  function update(field, value) {
    setForm((f) => ({ ...f, [field]: value }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');
    setSubmitting(true);
    const dto = {
      title: form.title,
      businessJustification: form.businessJustification,
      department: form.department,
      estimatedQuantity: Number(form.estimatedQuantity),
      estimatedUnitCost: Number(form.estimatedUnitCost),
    };
    try {
      const result = isEdit ? await requestService.update(id, dto) : await requestService.create(dto);
      navigate(`/requests/${result.id}`);
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) return <Spinner />;

  const total =
    Number(form.estimatedQuantity || 0) * Number(form.estimatedUnitCost || 0);

  return (
    <div>
      <Link to="/" className="back-link">
        ← Back to worklist
      </Link>
      <h1>{isEdit ? 'Edit Request' : 'New Purchase Request'}</h1>

      <ErrorBanner message={error} />

      <div className="card" style={{ maxWidth: 560 }}>
        <form onSubmit={handleSubmit}>
          <div className="form-field">
            <label htmlFor="title">Title</label>
            <input
              id="title"
              maxLength={200}
              value={form.title}
              onChange={(e) => update('title', e.target.value)}
              required
            />
          </div>

          <div className="form-field">
            <label htmlFor="justification">Business justification</label>
            <textarea
              id="justification"
              rows={4}
              maxLength={2000}
              value={form.businessJustification}
              onChange={(e) => update('businessJustification', e.target.value)}
              required
            />
          </div>

          <div className="form-field">
            <label htmlFor="department">Department</label>
            <input
              id="department"
              maxLength={100}
              value={form.department}
              onChange={(e) => update('department', e.target.value)}
              required
            />
          </div>

          <div className="form-row">
            <div className="form-field">
              <label htmlFor="quantity">Estimated quantity</label>
              <input
                id="quantity"
                type="number"
                min={1}
                step={1}
                value={form.estimatedQuantity}
                onChange={(e) => update('estimatedQuantity', e.target.value)}
                required
              />
            </div>
            <div className="form-field">
              <label htmlFor="unitCost">Estimated unit cost</label>
              <input
                id="unitCost"
                type="number"
                min={0.01}
                step={0.01}
                value={form.estimatedUnitCost}
                onChange={(e) => update('estimatedUnitCost', e.target.value)}
                required
              />
            </div>
          </div>

          <p style={{ fontSize: 13, marginBottom: 16 }}>
            Estimated total:{' '}
            <strong>
              ${total.toLocaleString(undefined, { minimumFractionDigits: 2 })}
            </strong>
          </p>

          <div className="btn-row">
            <button type="submit" className="btn" disabled={submitting}>
              {submitting ? 'Saving…' : isEdit ? 'Save changes' : 'Create draft'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
