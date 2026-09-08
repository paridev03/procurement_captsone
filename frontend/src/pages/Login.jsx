import { useState } from 'react';
import { useNavigate, useLocation, Navigate, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ErrorBanner } from '../components/Feedback';

const DEMO_ACCOUNTS = [
  { role: 'Employee', icon: '🧑‍💻', email: 'employee@procurement.local' },
  { role: 'Manager', icon: '🧭', email: 'manager@procurement.local' },
  { role: 'Finance', icon: '💰', email: 'finance@procurement.local' },
  { role: 'Procurement', icon: '📦', email: 'procurement@procurement.local' },
];
const DEMO_PASSWORD = 'Passw0rd!';

export default function Login() {
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (isAuthenticated) {
    return <Navigate to={location.state?.from || '/'} replace />;
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');
    setSubmitting(true);
    try {
      await login(email, password);
      navigate(location.state?.from || '/', { replace: true });
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  }

  function fillDemo(account) {
    setEmail(account.email);
    setPassword(DEMO_PASSWORD);
    setError('');
  }

  return (
    <div className="auth-shell">
      <aside className="auth-brand">
        <div className="auth-brand__mark">
          <span className="glyph">◆</span>
          Procurement Platform
        </div>

        <div className="auth-brand__body">
          <h1>Procure with confidence, end to end.</h1>
          <p>
            One connected workflow for requests, approvals, budgets, vendors, and payments —
            built for teams that need visibility and control at every stage.
          </p>

          <ul className="auth-brand__features">
            <li>
              <span className="icon">✓</span>
              Role-based approval chain with full audit history
            </li>
            <li>
              <span className="icon">✓</span>
              Live department budget checks before every sign-off
            </li>
            <li>
              <span className="icon">✓</span>
              Vendor selection, payment, and invoicing in one place
            </li>
          </ul>
        </div>

        <div className="auth-brand__foot">© {new Date().getFullYear()} Procurement Platform</div>
      </aside>

      <div className="auth-panel">
        <div className="auth-card">
          <h1>Welcome back</h1>
          <p>Sign in to your workspace to continue.</p>

          <ErrorBanner message={error} />

          <form onSubmit={handleSubmit}>
            <div className="form-field">
              <label htmlFor="email">Email</label>
              <input
                id="email"
                type="email"
                autoComplete="username"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div className="form-field">
              <label htmlFor="password">Password</label>
              <input
                id="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            <button type="submit" className="btn" disabled={submitting} style={{ width: '100%' }}>
              {submitting ? 'Signing in…' : 'Sign in'}
            </button>
          </form>

          <div className="auth-demo">
            <span className="auth-demo__label">Quick sign-in · demo accounts</span>
            <div className="auth-demo-grid">
              {DEMO_ACCOUNTS.map((a) => (
                <button
                  type="button"
                  key={a.email}
                  className="auth-demo-card"
                  onClick={() => fillDemo(a)}
                  title={`Fill credentials for ${a.email}`}
                >
                  <span className="auth-demo-card__icon" aria-hidden="true">{a.icon}</span>
                  <span className="auth-demo-card__text">
                    <span className="auth-demo-card__role">{a.role}</span>
                    <span className="auth-demo-card__email">{a.email}</span>
                  </span>
                </button>
              ))}
            </div>
          </div>

          <div className="auth-switch">
            New here? <Link to="/register">Create an account</Link>
          </div>
        </div>
      </div>
    </div>
  );
}
