import { useState, useEffect } from 'react';
import { api } from '../api/client';

interface AdminUser {
  id: string;
  entraOid: string;
  email: string;
  displayName: string;
  appRole: string;
  isGuest: boolean;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

export default function Admin() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('');

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    try {
      setLoading(true);
      const data = await api.get('/admin/users');
      setUsers(data);
    } catch (err) {
      console.error('Failed to load users:', err);
    } finally {
      setLoading(false);
    }
  };

  const updateRole = async (id: string, role: string) => {
    await api.put(`/admin/users/${id}/role`, { appRole: role });
    fetchUsers();
  };

  const toggleActive = async (id: string, current: boolean) => {
    await api.put(`/admin/users/${id}/active?active=${!current}`);
    fetchUsers();
  };

  const filteredUsers = users.filter(u => 
    u.email?.toLowerCase().includes(filter.toLowerCase()) ||
    u.displayName?.toLowerCase().includes(filter.toLowerCase())
  );

  if (loading) return <div className="loading">Loading users...</div>;

  return (
    <div>
      <h1 style={{ marginBottom: 20 }}>⚙️ Admin Panel</h1>

      <div className="card" style={{ marginBottom: 16 }}>
        <div className="form-group" style={{ marginBottom: 0 }}>
          <input 
            placeholder="🔍 Search users by email or name..."
            value={filter}
            onChange={e => setFilter(e.target.value)}
          />
        </div>
      </div>

      <div className="card">
        <table className="table">
          <thead>
            <tr>
              <th>User</th>
              <th>Role</th>
              <th>Type</th>
              <th>Status</th>
              <th>Last Login</th>
              <th>Joined</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredUsers.map(user => (
              <tr key={user.id}>
                <td>
                  <div style={{ fontWeight: 500 }}>{user.displayName || 'Unknown'}</div>
                  <div style={{ fontSize: 12, color: 'var(--text-secondary)' }}>{user.email}</div>
                </td>
                <td>
                  <select 
                    value={user.appRole}
                    onChange={e => updateRole(user.id, e.target.value)}
                    style={{ padding: '4px 8px', borderRadius: 4, border: '1px solid var(--border)' }}
                  >
                    <option value="admin">Admin</option>
                    <option value="editor">Editor</option>
                    <option value="user">User</option>
                    <option value="guest">Guest</option>
                  </select>
                </td>
                <td>
                  {user.isGuest ? (
                    <span className="badge badge-guest">Guest</span>
                  ) : (
                    <span className="badge badge-user">Internal</span>
                  )}
                </td>
                <td>
                  {user.isActive ? (
                    <span className="badge badge-success">Active</span>
                  ) : (
                    <span className="badge badge-admin">Inactive</span>
                  )}
                </td>
                <td style={{ fontSize: 13 }}>
                  {user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleDateString() : 'Never'}
                </td>
                <td style={{ fontSize: 13 }}>
                  {new Date(user.createdAt).toLocaleDateString()}
                </td>
                <td>
                  <button 
                    className={`btn ${user.isActive ? 'btn-danger' : 'btn-success'}`}
                    style={{ padding: '6px 12px', fontSize: 12 }}
                    onClick={() => toggleActive(user.id, user.isActive)}
                  >
                    {user.isActive ? 'Deactivate' : 'Activate'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {filteredUsers.length === 0 && (
          <div className="empty-state">
            <p>No users found matching "{filter}"</p>
          </div>
        )}
      </div>
    </div>
  );
}
