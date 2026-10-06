import { useAuth } from '../auth/useAuth';

export default function LoginPage() {
  const { login } = useAuth();

  return (
    <div style={{ 
      minHeight: '100vh', 
      display: 'flex', 
      alignItems: 'center', 
      justifyContent: 'center',
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)'
    }}>
      <div className="card" style={{ maxWidth: 400, width: '100%', textAlign: 'center' }}>
        <div style={{ fontSize: 64, marginBottom: 16 }}>🔐</div>
        <h1 style={{ marginBottom: 8 }}>Entra ID POC</h1>
        <p style={{ color: 'var(--text-secondary)', marginBottom: 24 }}>
          Secure authentication with Microsoft Entra ID
        </p>

        <div style={{ marginBottom: 24, textAlign: 'left', fontSize: 14, color: 'var(--text-secondary)' }}>
          <div style={{ marginBottom: 8 }}>✅ JWT token validation</div>
          <div style={{ marginBottom: 8 }}>✅ Auto-provisioning to PostgreSQL</div>
          <div style={{ marginBottom: 8 }}>✅ Role-based access control</div>
          <div>✅ Admin panel with user management</div>
        </div>

        <button className="btn btn-primary" style={{ width: '100%', justifyContent: 'center' }} onClick={login}>
          <span>🚀</span> Sign in with Microsoft
        </button>

        <p style={{ marginTop: 16, fontSize: 12, color: 'var(--text-secondary)' }}>
          Uses Microsoft Entra ID (Azure AD) for authentication
        </p>
      </div>
    </div>
  );
}
