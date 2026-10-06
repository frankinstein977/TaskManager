import { PublicClientApplication, EventType, AccountInfo } from '@azure/msal-browser';

// MSAL configuration for Microsoft Entra ID
export const msalConfig = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID || 'YOUR_CLIENT_ID',
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_ENTRA_TENANT_ID || 'YOUR_TENANT_ID'}`,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: 'localStorage' as const,
    storeAuthStateInCookie: true,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level: any, message: string, containsPii: boolean) => {
        if (!containsPii) console.log(`[MSAL ${level}] ${message}`);
      },
      piiLoggingEnabled: false,
      logLevel: 3, // Verbose for debugging
    },
  },
};

// Login scopes
export const loginRequest = {
  scopes: ['openid', 'profile', 'User.Read'],
};

// API scope (for calling your backend)
export const apiRequest = {
  scopes: [`api://${import.meta.env.VITE_ENTRA_CLIENT_ID || 'YOUR_CLIENT_ID'}/access_as_user`],
};

// Initialize MSAL instance
export const msalInstance = new PublicClientApplication(msalConfig);

// Handle redirect promise on load
msalInstance.initialize().then(() => {
  msalInstance.handleRedirectPromise().catch((error) => {
    console.error('Redirect handling error:', error);
  });

  // Set active account if available
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length > 0) {
    msalInstance.setActiveAccount(accounts[0]);
  }
});

// Listen for login events
msalInstance.addEventCallback((event) => {
  if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
    const account = event.payload as AccountInfo;
    msalInstance.setActiveAccount(account);
  }
});
