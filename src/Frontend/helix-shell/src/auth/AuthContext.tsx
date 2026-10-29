import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode
} from 'react';
import { ApiError, createAuthApi, type AuthApi } from '../lib/auth/authApi';
import {
  clearSession,
  isAccessTokenExpired,
  isRefreshTokenExpired,
  loadSession,
  saveSession
} from '../lib/auth/sessionStore';
import type { AuthUser, Credentials, StoredSession } from '../lib/auth/types';

const REFRESH_SKEW_MS = 60_000;
const ACCESS_TOKEN_SKEW_MS = 10_000;
const NETWORK_RETRY_MS = 30_000;

export type AuthStatus = 'loading' | 'authenticated' | 'anonymous';

export interface LoginOptions extends Credentials {
  remember: boolean;
}

interface AuthContextValue {
  status: AuthStatus;
  user: AuthUser | null;
  login: (options: LoginOptions) => Promise<void>;
  logout: () => Promise<void>;
  refresh: () => Promise<boolean>;
  getAccessToken: () => Promise<string | null>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

interface AuthProviderProps {
  children: ReactNode;
  api?: AuthApi;
}

export function AuthProvider({ children, api }: AuthProviderProps) {
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [session, setSession] = useState<StoredSession | null>(null);

  const sessionRef = useRef<StoredSession | null>(null);
  const refreshTimer = useRef<number | null>(null);
  const refreshPromise = useRef<Promise<StoredSession | null> | null>(null);
  const bootstrapped = useRef(false);
  const apiRef = useRef<AuthApi>(api ?? createAuthApi());
  const refreshRef = useRef<() => Promise<StoredSession | null>>(async () => null);

  useEffect(() => {
    if (api) {
      apiRef.current = api;
    }
  }, [api]);

  const clearRefreshTimer = useCallback(() => {
    if (refreshTimer.current !== null) {
      window.clearTimeout(refreshTimer.current);
      refreshTimer.current = null;
    }
  }, []);

  const schedule = useCallback(
    (current: StoredSession | null) => {
      clearRefreshTimer();
      if (!current) {
        return;
      }

      const expiresAt = Date.parse(current.tokens.accessTokenExpiresAtUtc);
      if (!Number.isFinite(expiresAt)) {
        return;
      }

      const delay = Math.max(5_000, expiresAt - REFRESH_SKEW_MS - Date.now());
      refreshTimer.current = window.setTimeout(() => {
        void refreshRef.current();
      }, delay);
    },
    [clearRefreshTimer]
  );

  const applySession = useCallback(
    (next: StoredSession | null) => {
      sessionRef.current = next;
      setSession(next);
      setStatus(next ? 'authenticated' : 'anonymous');

      if (next) {
        saveSession(next);
      } else {
        clearSession();
      }

      schedule(next);
    },
    [schedule]
  );

  const runRefresh = useCallback(async (): Promise<StoredSession | null> => {
    if (refreshPromise.current) {
      return refreshPromise.current;
    }

    const current = sessionRef.current;
    if (!current) {
      return null;
    }

    if (isRefreshTokenExpired(current)) {
      applySession(null);
      return null;
    }

    const task = (async () => {
      try {
        const result = await apiRef.current.refresh(current.tokens.refreshToken);
        const next: StoredSession = { ...result, remember: current.remember };
        applySession(next);
        return next;
      } catch (error) {
        if (error instanceof ApiError && (error.status === 401 || error.status === 400)) {
          applySession(null);
          return null;
        }

        clearRefreshTimer();
        refreshTimer.current = window.setTimeout(() => {
          void refreshRef.current();
        }, NETWORK_RETRY_MS);

        return current;
      } finally {
        refreshPromise.current = null;
      }
    })();

    refreshPromise.current = task;
    return task;
  }, [applySession, clearRefreshTimer]);

  useEffect(() => {
    refreshRef.current = runRefresh;
  }, [runRefresh]);

  useEffect(() => {
    if (bootstrapped.current) {
      if (sessionRef.current) {
        schedule(sessionRef.current);
      }
      return;
    }

    bootstrapped.current = true;
    const stored = loadSession();

    if (!stored) {
      setStatus('anonymous');
      return;
    }

    sessionRef.current = stored;
    setSession(stored);

    if (isRefreshTokenExpired(stored)) {
      applySession(null);
      return;
    }

    applySession(stored);

    void (async () => {
      if (!isAccessTokenExpired(stored, ACCESS_TOKEN_SKEW_MS)) {
        try {
          const user = await apiRef.current.me(stored.tokens.accessToken);
          applySession({ ...stored, user: { ...user, groups: user.groups ?? [] } });
          return;
        } catch (error) {
          if (!(error instanceof ApiError) || error.status !== 401) {
            return;
          }
        }
      }

      await runRefresh();
    })();
  }, [applySession, runRefresh, schedule]);

  useEffect(() => clearRefreshTimer, [clearRefreshTimer]);

  const login = useCallback(
    async ({ login: loginName, password, remember }: LoginOptions) => {
      const result = await apiRef.current.login({ login: loginName, password });
      applySession({ ...result, remember });
    },
    [applySession]
  );

  const logout = useCallback(async () => {
    const current = sessionRef.current;
    if (!current) {
      applySession(null);
      return;
    }

    try {
      let accessToken = current.tokens.accessToken;
      let refreshToken = current.tokens.refreshToken;

      if (isAccessTokenExpired(current, 0) && !isRefreshTokenExpired(current)) {
        const renewed = await apiRef.current.refresh(refreshToken);
        accessToken = renewed.tokens.accessToken;
        refreshToken = renewed.tokens.refreshToken;
      }

      await apiRef.current.logout(accessToken, refreshToken);
    } catch {
      // Revocation is best effort; the local session is always cleared.
    } finally {
      applySession(null);
    }
  }, [applySession]);

  const refresh = useCallback(async () => (await refreshRef.current()) !== null, []);

  const getAccessToken = useCallback(async () => {
    const current = sessionRef.current;
    if (!current) {
      return null;
    }

    if (!isAccessTokenExpired(current, ACCESS_TOKEN_SKEW_MS)) {
      return current.tokens.accessToken;
    }

    const next = await refreshRef.current();
    return next?.tokens.accessToken ?? null;
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user: session?.user ?? null,
      login,
      logout,
      refresh,
      getAccessToken
    }),
    [status, session, login, logout, refresh, getAccessToken]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside an AuthProvider.');
  }

  return context;
}