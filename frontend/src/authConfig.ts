import { BrowserCacheLocation, PublicClientApplication, type Configuration } from '@azure/msal-browser';

const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID?.trim();
const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID?.trim();
export const apiScope = import.meta.env.VITE_ENTRA_API_SCOPE?.trim() ?? '';

export const authConfigured = Boolean(clientId && tenantId && apiScope);

const configuration: Configuration | undefined = authConfigured
  ? {
      auth: {
        clientId: clientId!,
        authority: `https://login.microsoftonline.com/${tenantId}`,
        redirectUri: window.location.origin,
      },
      cache: { cacheLocation: BrowserCacheLocation.SessionStorage },
    }
  : undefined;

export const msalInstance = configuration ? new PublicClientApplication(configuration) : undefined;
