import { useMsal } from '@azure/msal-react';
import { loginRequest } from './msalConfig';

export function useAuth() {
  const { instance, accounts } = useMsal();
  const isAuthenticated = accounts.length > 0;
  const account = accounts[0] || null;

  const login = async () => {
    try {
      await instance.loginRedirect(loginRequest);
    } catch (error) {
      console.error('Login failed:', error);
    }
  };

  const logout = () => {
    instance.logoutRedirect({
      postLogoutRedirectUri: window.location.origin,
    });
  };

  const getToken = async () => {
    if (!account) return null;
    try {
      const response = await instance.acquireTokenSilent({
        ...loginRequest,
        account,
      });
      return response.accessToken;
    } catch (error) {
      // Token expired or not available, try interactive
      await instance.acquireTokenRedirect(loginRequest);
      return null;
    }
  };

  return { isAuthenticated, account, login, logout, getToken };
}
