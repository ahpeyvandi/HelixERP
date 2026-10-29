import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { AuthProvider, useAuth } from './AuthContext';
import { ApiError } from '../lib/auth/authApi';
import { saveSession, SESSION_STORAGE_KEY } from '../lib/auth/sessionStore';
import type { AuthApi } from '../lib/auth/authApi';
import type { AuthResult, StoredSession } from '../lib/auth/types';

function futureIso(offsetMs: number): string {
  return new Date(Date.now() + offsetMs).toISOString();
}

const RESULT: AuthResult = {
  user: {
    id: 'user-1',
    login: 'admin',
    displayName: 'Administrator',
    isSuperUser: true,
    groups: ['base.group_system']
  },
  tokens: {
    accessToken: 'access-1',
    accessTokenExpiresAtUtc: futureIso(3_600_000),
    refreshToken: 'refresh-1',
    refreshTokenExpiresAtUtc: futureIso(86_400_000)
  }
};

function makeSession(overrides: Partial<StoredSession> = {}): StoredSession {
  return { ...RESULT, remember: true, ...overrides };
}

function createFakeApi(overrides: Partial<AuthApi> = {}): AuthApi {
  return {
    login: vi.fn(async () => RESULT),
    refresh: vi.fn(async () => RESULT),
    logout: vi.fn(async () => undefined),
    me: vi.fn(async () => RESULT.user),
    ...overrides
  } as AuthApi;
}

function Probe() {
  const { status, user, login, logout } = useAuth();

  return (
    <div>
      <span data-testid="status">{status}</span>
      <span data-testid="user">{user?.displayName ?? 'none'}</span>
      <button
        type="button"
        onClick={() => {
          void login({ login: 'admin', password: 'Helix@12345', remember: true }).catch(
            () => undefined
          );
        }}
      >
        do-login
      </button>
      <button type="button" onClick={() => void logout()}>
        do-logout
      </button>
    </div>
  );
}

function renderProvider(api: AuthApi) {
  return render(
    <AuthProvider api={api}>
      <Probe />
    </AuthProvider>
  );
}

describe('AuthProvider', () => {
  it('starts anonymous without a stored session', async () => {
    renderProvider(createFakeApi());

    await waitFor(() => expect(screen.getByTestId('status').textContent).toBe('anonymous'));
  });

  it('restores a stored session and refreshes the user profile', async () => {
    saveSession(makeSession());
    const me = vi.fn(async () => ({ ...RESULT.user, displayName: 'مدیر بازیابی‌شده' }));
    const api = createFakeApi({ me });

    renderProvider(api);

    await waitFor(() => expect(screen.getByTestId('user').textContent).toBe('مدیر بازیابی‌شده'));
    expect(me).toHaveBeenCalledWith('access-1');
    expect(screen.getByTestId('status').textContent).toBe('authenticated');
  });

  it('refreshes an expired access token on startup without calling /me', async () => {
    saveSession(
      makeSession({
        tokens: { ...RESULT.tokens, accessTokenExpiresAtUtc: futureIso(-60_000) }
      })
    );
    const refresh = vi.fn(async () => ({
      ...RESULT,
      user: { ...RESULT.user, displayName: 'نشست تمدیدشده' }
    }));
    const me = vi.fn(async () => RESULT.user);
    const api = createFakeApi({ refresh, me });

    renderProvider(api);

    await waitFor(() =>
      expect(screen.getByTestId('user').textContent).toBe('نشست تمدیدشده')
    );
    expect(refresh).toHaveBeenCalledWith('refresh-1');
    expect(me).not.toHaveBeenCalled();
  });

  it('signs in and persists the session', async () => {
    const api = createFakeApi();
    renderProvider(api);

    await waitFor(() => expect(screen.getByTestId('status').textContent).toBe('anonymous'));
    await userEvent.click(screen.getByRole('button', { name: 'do-login' }));

    await waitFor(() => expect(screen.getByTestId('status').textContent).toBe('authenticated'));
    expect(api.login).toHaveBeenCalledWith({ login: 'admin', password: 'Helix@12345' });

    const stored = window.localStorage.getItem(SESSION_STORAGE_KEY);
    expect(stored).not.toBeNull();
    expect(JSON.parse(stored ?? '{}').tokens.accessToken).toBe('access-1');
  });

  it('revokes the refresh token and clears the session on logout', async () => {
    saveSession(makeSession());
    const api = createFakeApi();
    renderProvider(api);

    await waitFor(() => expect(screen.getByTestId('status').textContent).toBe('authenticated'));
    await userEvent.click(screen.getByRole('button', { name: 'do-logout' }));

    await waitFor(() => expect(screen.getByTestId('status').textContent).toBe('anonymous'));
    expect(api.logout).toHaveBeenCalledWith('access-1', 'refresh-1');
    expect(window.localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('drops the session when the refresh token is rejected', async () => {
    saveSession(
      makeSession({
        tokens: { ...RESULT.tokens, accessTokenExpiresAtUtc: futureIso(-60_000) }
      })
    );
    const api = createFakeApi({
      refresh: vi.fn(async () => {
        throw new ApiError(401, 'Invalid refresh token');
      })
    });

    renderProvider(api);

    await waitFor(() => expect(screen.getByTestId('status').textContent).toBe('anonymous'));
    expect(window.localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('keeps the session when refresh fails because of the network', async () => {
    saveSession(
      makeSession({
        tokens: { ...RESULT.tokens, accessTokenExpiresAtUtc: futureIso(-60_000) }
      })
    );
    const api = createFakeApi({
      refresh: vi.fn(async () => {
        throw new ApiError(0, 'NetworkError');
      })
    });

    renderProvider(api);

    await waitFor(() => expect(screen.getByTestId('status').textContent).toBe('authenticated'));
    expect(window.localStorage.getItem(SESSION_STORAGE_KEY)).not.toBeNull();
  });
});