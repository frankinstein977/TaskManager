import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';
import { useProfile } from '../hooks/useProfile';

interface LayoutProps { children: React.ReactNode; }

export default function Layout({ children }: LayoutProps) {
  const { logout } = useAuth();
  const { profile } = useProfile();
  const location = useLocation();

  const isActive = (path: string) => location.pathname === path ? 'active' : '';
  const isAdmin = profile?.appRole === 'admin';
  const isGuest = profile?.isGuest;

  return (
    <div>
      <nav className="navbar">
        <div className="navbar-brand">🔐 Entra ID POC</div>

        <div className="nav-links">
          <Link to="/" className={isActive('/')}>Home</Link>
          <Link to="/dashboard" className={isActive('/dashboard')}>Dashboard</Link>
          {!isGuest && <Link to="/projects" className={isActive('/projects')}>Projects</Link>}
          {isAdmin && <Link to="/admin" className={isActive('/admin')}>Admin Panel</Link>}
          <Link to="/profile" className={isActive('/profile')}>Profile</Link>

          <div className="user-chip">
            <span>{profile?.displayName || profile?.email || 'User'}</span>
            <span className={`badge badge-${profile?.appRole || 'user'}`}>
              {profile?.appRole}
            </span>
            {profile?.isGuest && <span className="badge badge-guest">Guest</span>}
          </div>

          <button className="btn btn-outline" onClick={logout}>
            Sign Out
          </button>
        </div>
      </nav>

      <main className="container">
        {children}
      </main>
    </div>
  );
}
