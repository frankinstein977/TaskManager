import { useState, useEffect } from 'react';
import { useProfile } from '../hooks/useProfile';
import { api } from '../api/client';

interface Stats {
  totalUsers: number;
  activeUsers: number;
  guestUsers: number;
  totalProjects: number;
  myProjects: number;
}

export default function Dashboard() {
  const { profile } = useProfile();
  const [stats, setStats] = useState<Stats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/admin/dashboard')
      .then(data => { setStats(data); setLoading(false); })
      .catch(() => setLoading(false));
  }, []);

  if (loading) return <div className="loading">Loading dashboard...</div>;

  const statCards = [
    { label: 'Total Users', value: stats?.totalUsers || 0, icon: '👥' },
    { label: 'Active Users', value: stats?.activeUsers || 0, icon: '✅' },
    { label: 'Guest Users', value: stats?.guestUsers || 0, icon: '🌐' },
    { label: 'Total Projects', value: stats?.totalProjects || 0, icon: '📁' },
    { label: 'My Projects', value: stats?.myProjects || 0, icon: '📝' },
  ];

  return (
    <div>
      <h1 style={{ marginBottom: 20 }}>📊 Dashboard</h1>

      <div className="grid-4">
        {statCards.map(card => (
          <div className="card" key={card.label} style={{ textAlign: 'center' }}>
            <div style={{ fontSize: 32, marginBottom: 8 }}>{card.icon}</div>
            <div style={{ fontSize: 28, fontWeight: 700, color: 'var(--primary)' }}>{card.value}</div>
            <div style={{ fontSize: 13, color: 'var(--text-secondary)', marginTop: 4 }}>{card.label}</div>
          </div>
        ))}
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        <h3 style={{ marginBottom: 12 }}>Your Profile Summary</h3>
        <div className="grid-2">
          <div>
            <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Email</div>
            <div style={{ fontWeight: 500 }}>{profile?.email}</div>
          </div>
          <div>
            <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Entra OID</div>
            <div style={{ fontFamily: 'monospace', fontSize: 13 }}>{profile?.entraOid}</div>
          </div>
          <div>
            <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Database ID</div>
            <div style={{ fontFamily: 'monospace', fontSize: 13 }}>{profile?.dbId}</div>
          </div>
          <div>
            <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Member Since</div>
            <div>{profile?.memberSince ? new Date(profile.memberSince).toLocaleDateString() : 'N/A'}</div>
          </div>
        </div>
      </div>
    </div>
  );
}
