import { Navigate } from 'react-router-dom';
import { useProfile } from '../hooks/useProfile';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: string;
  allowGuest?: boolean;
}

export default function ProtectedRoute({ children, requiredRole, allowGuest = false }: ProtectedRouteProps) {
  const { profile, loading } = useProfile();

  if (loading) return <div className="loading">Loading...</div>;
  if (!profile) return <Navigate to="/" replace />;

  if (!profile.isActive) {
    return (
      <div className="alert alert-error">
        Your account has been deactivated. Contact an administrator.
      </div>
    );
  }

  if (!allowGuest && profile.isGuest) {
    return (
      <div className="alert alert-info">
        Guest users have read-only access. Contact an admin for full access.
      </div>
    );
  }

  if (requiredRole && profile.appRole !== requiredRole) {
    return (
      <div className="alert alert-error">
        Access denied. Required role: {requiredRole}
      </div>
    );
  }

  return <>{children}</>;
}
