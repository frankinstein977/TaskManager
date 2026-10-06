import { Link } from 'react-router-dom';
import { useProfile } from '../hooks/useProfile';

export default function Home() {
  const { profile } = useProfile();

  return (
    <div>
      <div className="card">
        <h1 style={{ marginBottom: 8 }}>Welcome, {profile?.displayName || 'User'}! 👋</h1>
        <p style={{ color: 'var(--text-secondary)' }}>
          You are signed in via Microsoft Entra ID with role: 
          <span className={`badge badge-${profile?.appRole}`} style={{ marginLeft: 8 }}>
            {profile?.appRole}
          </span>
          {profile?.isGuest && (
            <span className="badge badge-guest" style={{ marginLeft: 8 }}>Guest</span>
          )}
        </p>
      </div>

      <div className="grid-3">
        <Link to="/dashboard" style={{ textDecoration: 'none' }}>
          <div className="card" style={{ cursor: 'pointer' }}>
            <div style={{ fontSize: 32, marginBottom: 8 }}>📊</div>
            <h3>Dashboard</h3>
            <p style={{ color: 'var(--text-secondary)', fontSize: 14 }}>
              View system statistics and your activity
            </p>
          </div>
        </Link>

        {!profile?.isGuest && (
          <Link to="/projects" style={{ textDecoration: 'none' }}>
            <div className="card" style={{ cursor: 'pointer' }}>
              <div style={{ fontSize: 32, marginBottom: 8 }}>📁</div>
              <h3>Projects</h3>
              <p style={{ color: 'var(--text-secondary)', fontSize: 14 }}>
                Manage your projects and tasks
              </p>
            </div>
          </Link>
        )}

        {profile?.appRole === 'admin' && (
          <Link to="/admin" style={{ textDecoration: 'none' }}>
            <div className="card" style={{ cursor: 'pointer' }}>
              <div style={{ fontSize: 32, marginBottom: 8 }}>⚙️</div>
              <h3>Admin Panel</h3>
              <p style={{ color: 'var(--text-secondary)', fontSize: 14 }}>
                Manage users, roles, and system settings
              </p>
            </div>
          </Link>
        )}
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        <h3 style={{ marginBottom: 12 }}>How it works</h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {[
            { step: '1', text: 'You authenticate with Microsoft Entra ID (corporate credentials + MFA)' },
            { step: '2', text: 'Backend receives JWT token and validates signature against Microsoft JWKS' },
            { step: '3', text: 'User is auto-provisioned in PostgreSQL on first login (linked by Entra OID)' },
            { step: '4', text: 'App role is derived from Entra claims (admin, editor, user, guest)' },
            { step: '5', text: 'All subsequent requests use your DB user ID for authorization and data queries' },
          ].map(item => (
            <div key={item.step} style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <div style={{ 
                width: 28, height: 28, borderRadius: '50%', background: 'var(--primary)', 
                color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center',
                fontSize: 12, fontWeight: 700, flexShrink: 0
              }}>
                {item.step}
              </div>
              <span style={{ fontSize: 14 }}>{item.text}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
