import { useState, useEffect } from 'react';
import { useAuth } from '../auth/useAuth';
import { api } from '../api/client';

interface UserProfile {
  dbId: string;
  entraOid: string;
  email: string;
  displayName: string;
  appRole: string;
  isGuest: boolean;
  isActive: boolean;
  department: string | null;
  preferences: string;
  lastLoginAt: string | null;
  memberSince: string;
}

export function useProfile() {
  const { isAuthenticated, getToken } = useAuth();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthenticated) {
      setLoading(false);
      return;
    }

    const fetchProfile = async () => {
      try {
        setLoading(true);

        // Get token and store for apiFetch
        const token = await getToken();
        if (token) localStorage.setItem('access_token', token);

        const data = await api.get('/auth/me');
        setProfile(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load profile');
      } finally {
        setLoading(false);
      }
    };

    fetchProfile();
  }, [isAuthenticated, getToken]);

  return { profile, loading, error, refresh: () => setLoading(true) };
}
