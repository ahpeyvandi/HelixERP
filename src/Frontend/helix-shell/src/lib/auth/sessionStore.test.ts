import { describe, expect, it } from 'vitest';
import {
  clearSession,
  isAccessTokenExpired,
  isRefreshTokenExpired,
  loadSession,
  saveSession,
  SESSION_STORAGE_KEY
} from './sessionStore';
import type { StoredSession } from './types';

function futureIso(offsetMs = 3_600_000): string {
  return new Date(Date.now() + offsetMs).toISOString();
}

function makeSession(overrides: Partial<StoredSession> = {}): StoredSession {
  return {
    user: {
      id: 'user-1',
      login: 'admin',
      displayName: 'Administrator',
      isSuperUser: true,
      groups: ['base.group_system']
    },
    tokens: {
      accessToken: 'access-1',
      accessTokenExpiresAtUtc: futureIso(),
      refreshToken: 'refresh-1',
      refreshTokenExpiresAtUtc: futureIso(86_400_000)
    },
    remember: true,
    ...overrides
  };
}

describe('sessionStore', () => {
  it('round-trips a remembered session through localStorage', () => {
    const session = makeSession();

    saveSession(session);

    expect(window.localStorage.getItem(SESSION_STORAGE_KEY)).not.toBeNull();
    expect(window.sessionStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
    expect(loadSession()).toEqual(session);
  });

  it('stores non-remembered sessions in sessionStorage only', () => {
    saveSession(makeSession({ remember: false }));

    expect(window.localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
    expect(window.sessionStorage.getItem(SESSION_STORAGE_KEY)).not.toBeNull();
    expect(loadSession()?.remember).toBe(false);
  });

  it('clears both storages and switches storage on a new save', () => {
    saveSession(makeSession({ remember: true }));
    saveSession(makeSession({ remember: false, user: { ...makeSession().user, displayName: 'Second' } }));

    expect(window.localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
    expect(loadSession()?.user.displayName).toBe('Second');

    clearSession();
    expect(loadSession()).toBeNull();
  });

  it('ignores corrupt payloads and removes them', () => {
    window.localStorage.setItem(SESSION_STORAGE_KEY, '{not-json');

    expect(loadSession()).toBeNull();
    expect(window.localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('rejects sessions without a refresh token', () => {
    const broken = makeSession();
    broken.tokens.refreshToken = '';
    window.localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(broken));

    expect(loadSession()).toBeNull();
  });

  it('detects access-token expiry with a clock skew', () => {
    const session = makeSession({
      tokens: { ...makeSession().tokens, accessTokenExpiresAtUtc: futureIso(20_000) }
    });

    expect(isAccessTokenExpired(session)).toBe(false);
    expect(isAccessTokenExpired(session, 30_000)).toBe(true);
  });

  it('detects expired refresh tokens', () => {
    const session = makeSession({
      tokens: { ...makeSession().tokens, refreshTokenExpiresAtUtc: futureIso(-1_000) }
    });

    expect(isRefreshTokenExpired(session)).toBe(true);
  });
});