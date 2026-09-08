import { useState } from 'react';
import { useNavigate, Link, Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ErrorBanner } from '../components/Feedback';
import { ROLES } from '../utils/constants';

export default function Register() {
  const { register, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [department, setDepartment] = useState('');
  const [role, setRole] = useState('Employee');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');
    setSubmitting(true);
    try {
      await register(fullName, email, password, role, department);
      navigate('/', { replace: true });
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="auth-shell">
      <aside className="auth-brand">
        <div className="auth-brand__mark">
          <span className="glyph">◆</span>
          Procurement Platform
        </div>

        <div className="auth-brand__body">
          <h1>Join your team's workspace.</h1>
          <p>
            Create an account to raise requests, review approvals, manage budgets, or handle
            procurement — whichever role fits what you do.
          </p>

          <ul className="auth-brand__features">
            <li>
              <span className="icon">✓</span>
              Set up in under a minute, no admin approval needed for this demo
            </li>
            <li>
              <span className="icon">✓</span>
              Every action tracked in a complete audit timeline
            </li>
            <li>
              <span className="icon">✓</span>
              Switch roles anytime by creating another account
            </li>
          </ul>
        </div>

        <div className="auth-brand__foot">© {new Date().getFullYear()} Procurement Platform</div>
      </aside>

      <div className="auth-panel">
        <div className="auth-card">
          <h1>Create your account</h1>
          <p>Register for the Procurement Platform.</p>

          <ErrorBanner message={error} />

          <form onSubmit={handleSubmit}>
            <div className="form-field">
              <label htmlFor="fullName">Full name</label>
              <input id="fullName" value={fullName} onChange={(e) => setFullName(e.target.value)} required />
            </div>
            <div className="form-field">
              <label htmlFor="email">Email</label>
              <input id="email" type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} required />
            </div>
            <div className="form-field">
              <label htmlFor="password">Password</label>
              <input
                id="password"
                type="password"
                autoComplete="new-password"
                minLength={8}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            <div className="form-row">
              <div className="form-field">
                <label htmlFor="department">Department</label>
                <input id="department" value={department} onChange={(e) => setDepartment(e.target.value)} required />
              </div>
              <div className="form-field">
                <label htmlFor="role">Role</label>
                <select id="role" value={role} onChange={(e) => setRole(e.target.value)}>
                  {ROLES.map((r) => (
                    <option key={r.value} value={r.value}>{r.label}</option>
                  ))}
                </select>
              </div>
            </div>
            <button type="submit" className="btn" disabled={submitting} style={{ width: '100%' }}>
              {submitting ? 'Creating account…' : 'Create account'}
            </button>
          </form>

          <div className="auth-switch">
            Already have an account? <Link to="/login">Sign in</Link>
          </div>
        </div>
      </div>
    </div>
  );
}
