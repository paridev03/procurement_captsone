import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import NotificationBell from './NotificationBell';

const QUEUE_LABEL = {
  Employee: 'My Requests',
  Manager: 'Approval Queue',
  Finance: 'Finance Queue',
  ProcurementAdmin: 'Procurement Queue',
};

export default function NavBar() {
  const { user, logout } = useAuth();
  if (!user) return null;

  return (
    <header className="navbar">
      <Link to="/" className="navbar__brand">
        Procurement Platform
      </Link>
      <nav className="navbar__links">
        <Link to="/">Dashboard</Link>
        <Link to="/requests">{QUEUE_LABEL[user.role] || 'Requests'}</Link>
      </nav>
      <div className="navbar__user">
        <NotificationBell />
        <span className="navbar__name">{user.fullName}</span>
        <span className="navbar__role">{user.role}</span>
        <button className="btn btn--ghost" onClick={logout}>
          Log out
        </button>
      </div>
    </header>
  );
}
