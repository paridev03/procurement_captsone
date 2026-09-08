import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { requestService } from '../api/requestService';
import { useAuth } from '../context/AuthContext';
import { Spinner, ErrorBanner } from '../components/Feedback';

const QUEUE_LABEL = {
  Employee: 'My Requests',
  Manager: 'Approval Queue',
  Finance: 'Finance Queue',
  ProcurementAdmin: 'Procurement Queue',
};

const TILES = [
  { key: 'total', label: 'Total Requests', icon: '📋' },
  { key: 'draft', label: 'Draft', icon: '📝' },
  { key: 'pending', label: 'Pending', icon: '⏳' },
  { key: 'approved', label: 'Approved / In Progress', icon: '✅' },
  { key: 'rejected', label: 'Rejected', icon: '⛔' },
  { key: 'completed', label: 'Completed', icon: '🏁' },
  { key: 'paymentFailed', label: 'Payment Failed', icon: '⚠️' },
];

export default function Dashboard() {
  const { user } = useAuth();
  const [summary, setSummary] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    requestService
      .getDashboard()
      .then((data) => {
        if (!cancelled) setSummary(data);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Dashboard</h1>
          <p>Welcome back, {user.fullName}.</p>
        </div>
        <div className="btn-row">
          {user.role === 'Employee' && (
            <Link to="/requests/new" className="btn">+ New Request</Link>
          )}
          {user.role === 'ProcurementAdmin' && (
            <Link to="/procurement/new" className="btn">+ New Procurement</Link>
          )}
          <Link to="/requests" className="btn btn--ghost">{QUEUE_LABEL[user.role] || 'Requests'}</Link>
        </div>
      </div>

      <ErrorBanner message={error} />

      {summary === null && !error ? (
        <Spinner />
      ) : summary ? (
        <div className="summary-cards">
          {TILES.map((t) => (
            <div className="summary-card" key={t.key}>
              <span className="summary-card__icon" aria-hidden="true">{t.icon}</span>
              <div>
                <span className="summary-card__label">{t.label}</span>
                <span className="summary-card__value">{summary[t.key] ?? 0}</span>
              </div>
            </div>
          ))}
        </div>
      ) : null}
    </div>
  );
}
