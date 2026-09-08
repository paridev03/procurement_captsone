import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function NavBar() {
  const { user, logout } = useAuth();
  if (!user) return null;

  return (
    <header className="navbar">
      <Link to="/" className="navbar__brand">
        Procurement Platform
      </Link>
      <div className="navbar__user">
        <span className="navbar__name">{user.fullName}</span>
        <span className="navbar__role">{user.role}</span>
        <button className="btn btn--ghost" onClick={logout}>
          Log out
        </button>
      </div>
    </header>
  );
}
