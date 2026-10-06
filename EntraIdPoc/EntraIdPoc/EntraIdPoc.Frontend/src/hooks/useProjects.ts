import { useState, useEffect, useCallback } from 'react';
import { api } from '../api/client';

interface Project {
  id: string;
  title: string;
  description: string | null;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export function useProjects() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchProjects = useCallback(async () => {
    try {
      setLoading(true);
      const data = await api.get('/projects');
      setProjects(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load projects');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchProjects(); }, [fetchProjects]);

  const createProject = async (title: string, description?: string) => {
    const data = await api.post('/projects', { title, description });
    setProjects(prev => [data, ...prev]);
    return data;
  };

  const updateProject = async (id: string, title: string, description: string | null, status: string) => {
    const data = await api.put(`/projects/${id}`, { title, description, status });
    setProjects(prev => prev.map(p => p.id === id ? data : p));
    return data;
  };

  const deleteProject = async (id: string) => {
    await api.delete(`/projects/${id}`);
    setProjects(prev => prev.filter(p => p.id !== id));
  };

  return { projects, loading, error, createProject, updateProject, deleteProject, refresh: fetchProjects };
}
