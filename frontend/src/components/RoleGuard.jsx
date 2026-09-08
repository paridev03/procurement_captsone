import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

/// Route-level authorization. Backend re-checks everything, but the UI shouldn't even
/// render a Manager's approval buttons to an Employee — see Section 7, docs/ANALYSIS.md.
export default function RoleGuard({ roles, children }) {
  const { user, isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (roles && !roles.includes(user.role)) {
    return <Navigate to="/" replace />;
  }

  return children;
}
