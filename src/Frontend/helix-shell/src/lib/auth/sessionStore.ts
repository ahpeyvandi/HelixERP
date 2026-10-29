import type { StoredSession } from './types';

export const SESSION_STORAGE_KEY = 'helix.auth.session';

function readStorage(kind: 'local' | 'session'): Storage | null {
  try {
    return kind === 'local' ? window.localStorage : window.sessionStorage;
  } catch {
    return null;
  }
}

function parseSession(raw: string): StoredSession | null {
  try {
    const value = JSON.parse(raw) as Partial<StoredSession> | null;
    if (!value || typeof value !== 'object') {
      return null;
    }

    const { user, tokens } = value;
    if (!user || !tokens) {
      return null;
    }

    if (
      typeof user.id !== 'string' ||
      typeof user.login !== 'string' ||
      typeof user.displayName !== 'string' ||
      typeof tokens.accessToken !== 'string' ||
      !tokens.accessToken ||
      typeof tokens.refreshToken !== 'string' ||
      !tokens.refreshToken
    ) {
      return null;
    }

    if (
      !Number.isFinite(Date.parse(tokens.accessTokenExpiresAtUtc)) ||
      !Number.isFinite(Date.parse(tokens.refreshTokenExpiresAtUtc))
    ) {
      return null;
    }

    return {
      user: {
        id: user.id,
        login: user.login,
        displayName: user.displayName,
        isSuperUser: Boolean(user.isSuperUser),
        groups: Array.isArray(user.groups)
          ? user.groups.filter((group): group is string => typeof group === 'string')
          : []
      },
      tokens: {
        accessToken: tokens.accessToken,
        accessTokenExpiresAtUtc: tokens.accessTokenExpiresAtUtc,
        refreshToken: tokens.refreshToken,
        refreshTokenExpiresAtUtc: tokens.refreshTokenExpiresAtUtc
      },
      remember: value.remember !== false
    };
  } catch {
    return null;
  }
}

export function loadSession(): StoredSession | null {
  for (const kind of ['local', 'session'] as const) {
    const storage = readStorage(kind);
    if (!storage) {
      continue;
    }

    let raw: string | null = null;
    try {
      raw = storage.getItem(SESSION_STORAGE_KEY);
    } catch {
      raw = null;
    }

    if (!raw) {
      continue;
    }

    const session = parseSession(raw);
    if (session) {
      return session;
    }

    try {
      storage.removeItem(SESSION_STORAGE_KEY);
    } catch {
      // Ignore storage failures; the corrupt payload is simply skipped.
    }
  }

  return null;
}

export function clearSession(): void {
  for (const kind of ['local', 'session'] as const) {
    try {
      readStorage(kind)?.removeItem(SESSION_STORAGE_KEY);
    } catch {
      // Ignore storage failures.
    }
  }
}

export function saveSession(session: StoredSession): void {
  clearSession();
  const storage = readStorage(session.remember ? 'local' : 'session');
  if (!storage) {
    return;
  }

  try {
    storage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
  } catch {
    // A full or unavailable storage must not break the login flow.
  }
}

export function isAccessTokenExpired(session: StoredSession, skewMs = 0): boolean {
  return Date.parse(session.tokens.accessTokenExpiresAtUtc) - skewMs <= Date.now();
}

export function isRefreshTokenExpired(session: StoredSession): boolean {
  return Date.parse(session.tokens.refreshTokenExpiresAtUtc) <= Date.now();
}