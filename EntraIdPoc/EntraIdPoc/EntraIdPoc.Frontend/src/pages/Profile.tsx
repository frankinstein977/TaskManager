import { useState } from 'react';
import { useProfile } from '../hooks/useProfile';
import { api } from '../api/client';

export default function Profile() {
  const { profile, loading, refresh } = useProfile();
  const [dept, setDept] = useState(profile?.department || '');
  const [prefs, setPrefs] = useState(profile?.preferences || '{}');
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  if (loading) return <div className="loading">Loading profile...</div>;
  if (!profile) return <div className="alert alert-error">Profile not found</div>;

  const handleSave = async () => {
    setSaving(true);
    try {
      await api.put('/auth/me', { department: dept, preferences: prefs });
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
      refresh();
    } catch (err) {
      console.error('Save failed:', err);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div>
      <h1 style={{ marginBottom: 20 }}>👤 Profile</h1>

      <div className="grid-2">
        <div className="card">
          <h3 style={{ marginBottom: 16 }}>Identity (Read-only from Entra)</h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div>
              <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Display Name</div>
              <div style={{ fontWeight: 500 }}>{profile.displayName}</div>
            </div>
            <div>
              <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Email</div>
              <div style={{ fontWeight: 500 }}>{profile.email}</div>
            </div>
            <div>
              <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Entra OID</div>
              <div style={{ fontFamily: 'monospace', fontSize: 13, wordBreak: 'break-all' }}>{profile.entraOid}</div>
            </div>
            <div>
              <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>App Role</div>
              <span className={`badge badge-${profile.appRole}`}>{profile.appRole}</span>
            </div>
            <div>
              <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Account Type</div>
              <span className={`badge badge-${profile.isGuest ? 'guest' : 'user'}`}>
                {profile.isGuest ? 'Guest (External)' : 'Internal Employee'}
              </span>
            </div>
          </div>
        </div>

        <div className="card">
          <h3 style={{ marginBottom: 16 }}>Application Settings (Editable)</h3>

          {saved && <div className="alert alert-success" style={{ marginBottom: 12 }}>Profile updated!</div>}

          <div className="form-group">
            <label>Department</label>
            <input 
              value={dept} 
              onChange={e => setDept(e.target.value)} 
              placeholder="e.g., Engineering, Marketing"
            />
          </div>

          <div className="form-group">
            <label>Preferences (JSON)</label>
            <textarea 
              value={prefs} 
              onChange={e => setPrefs(e.target.value)} 
              rows={5}
              placeholder='{"theme": "dark", "notifications": true}'
            />
          </div>

          <button 
            className="btn btn-primary" 
            onClick={handleSave}
            disabled={saving}
          >
            {saving ? 'Saving...' : '💾 Save Changes'}
          </button>
        </div>
      </div>

      <div className="card" style={{ marginTop: 16 }}>
        <h3 style={{ marginBottom: 12 }}>Account Metadata</h3>
        <div className="grid-3">
          <div>
            <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Database ID</div>
            <div style={{ fontFamily: 'monospace', fontSize: 13 }}>{profile.dbId}</div>
          </div>
          <div>
            <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Member Since</div>
            <div>{new Date(profile.memberSince).toLocaleDateString()}</div>
          </div>
          <div>
            <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginBottom: 4 }}>Last Login</div>
            <div>{profile.lastLoginAt ? new Date(profile.lastLoginAt).toLocaleString() : 'Never'}</div>
          </div>
        </div>
      </div>
    </div>
  );
}
