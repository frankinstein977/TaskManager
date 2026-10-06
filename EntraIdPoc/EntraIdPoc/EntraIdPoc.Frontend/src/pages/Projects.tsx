import { useState } from 'react';
import { useProjects } from '../hooks/useProjects';
import { useProfile } from '../hooks/useProfile';

export default function Projects() {
  const { projects, loading, error, createProject, deleteProject, refresh } = useProjects();
  const { profile } = useProfile();
  const [newTitle, setNewTitle] = useState('');
  const [newDesc, setNewDesc] = useState('');
  const [showForm, setShowForm] = useState(false);

  const isGuest = profile?.isGuest;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTitle.trim()) return;
    await createProject(newTitle, newDesc);
    setNewTitle('');
    setNewDesc('');
    setShowForm(false);
  };

  if (loading) return <div className="loading">Loading projects...</div>;
  if (error) return <div className="alert alert-error">{error}</div>;

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <h1>📁 Projects</h1>
        {!isGuest && (
          <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
            {showForm ? 'Cancel' : '+ New Project'}
          </button>
        )}
      </div>

      {isGuest && (
        <div className="alert alert-info" style={{ marginBottom: 16 }}>
          Guest users have read-only access to projects.
        </div>
      )}

      {showForm && (
        <div className="card" style={{ marginBottom: 16 }}>
          <h3 style={{ marginBottom: 16 }}>Create New Project</h3>
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Title *</label>
              <input 
                value={newTitle} 
                onChange={e => setNewTitle(e.target.value)} 
                placeholder="Project title"
                required
              />
            </div>
            <div className="form-group">
              <label>Description</label>
              <textarea 
                value={newDesc} 
                onChange={e => setNewDesc(e.target.value)} 
                placeholder="Optional description"
                rows={3}
              />
            </div>
            <button type="submit" className="btn btn-success">Create Project</button>
          </form>
        </div>
      )}

      {projects.length === 0 ? (
        <div className="empty-state">
          <div style={{ fontSize: 48, marginBottom: 12 }}>📂</div>
          <h3>No projects yet</h3>
          <p>Create your first project to get started</p>
        </div>
      ) : (
        <div className="grid-2">
          {projects.map(project => (
            <div className="card" key={project.id}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', marginBottom: 8 }}>
                <h3 style={{ margin: 0 }}>{project.title}</h3>
                <span className={`badge badge-${project.status === 'active' ? 'success' : 'guest'}`}>
                  {project.status}
                </span>
              </div>
              <p style={{ color: 'var(--text-secondary)', fontSize: 14, marginBottom: 12 }}>
                {project.description || 'No description'}
              </p>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: 12, color: 'var(--text-secondary)' }}>
                <span>Updated: {new Date(project.updatedAt).toLocaleDateString()}</span>
                {!isGuest && (
                  <button 
                    className="btn btn-outline" 
                    style={{ padding: '6px 12px', fontSize: 12 }}
                    onClick={() => deleteProject(project.id)}
                  >
                    🗑️ Delete
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
