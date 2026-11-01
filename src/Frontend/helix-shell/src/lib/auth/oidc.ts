import type { StoredSession } from './types';

export const OIDC_CALLBACK_ROUTE = '/#/oidc/callback';

export interface OidcCallbackResult {
  session: StoredSession | null;
  failed: boolean;
}

/**
 * Parses the URL hash fragment produced by the Shell's OIDC callback and turns
 * it into a persisted session. Returns null (with failed=false) when the current
 * URL is not an OIDC callback.
 */
export function readOidcCallbackHash(
  hash: string = window.location.hash ?? ''
): OidcCallbackResult {
  if (!hash.includes('oidc/callback')) {
    return { session: null, failed: false };
  }

  try {
    const params = new URLSearchParams(hash.split('?')[1] ?? '');

    if (params.get('auth') === 'failed') {
      return { session: null, failed: true };
    }

    const accessToken = params.get('access_token');
    const refreshToken = params.get('refresh_token');
    const accessTokenExpiresAtUtc = params.get('access_token_expires_at');
    const refreshTokenExpiresAtUtc = params.get('refresh_token_expires_at');

    if (
      !accessToken ||
      !refreshToken ||
      !accessTokenExpiresAtUtc ||
      !refreshTokenExpiresAtUtc
    ) {
      return { session: null, failed: true };
    }

    const session: StoredSession = {
      user: {
        id: '',
        login: '',
        displayName: '',
        isSuperUser: false,
        groups: []
      },
      tokens: {
        accessToken,
        accessTokenExpiresAtUtc,
        refreshToken,
        refreshTokenExpiresAtUtc
      },
      remember: false
    };

    return { session, failed: false };
  } catch {
    return { session: null, failed: true };
  }
}

export function clearOidcCallbackHash(): void {
  if (!window.location.hash) {
    return;
  }

  window.history.replaceState(null, '', window.location.pathname + window.location.search);
}
