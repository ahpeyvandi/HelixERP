import type { AuthResult, AuthUser, Credentials } from './types';

export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail?: string;

  constructor(status: number, title: string, detail?: string) {
    super(detail ?? title);
    this.name = 'ApiError';
    this.status = status;
    this.title = title;
    this.detail = detail;
  }
}

export type FetchLike = (input: string, init?: RequestInit) => Promise<Response>;

async function toApiError(response: Response): Promise<ApiError> {
  let title = response.statusText || `HTTP ${response.status}`;
  let detail: string | undefined;

  try {
    const text = await response.text();
    if (text) {
      const problem = JSON.parse(text) as { title?: string; detail?: string };
      if (typeof problem.title === 'string' && problem.title) {
        title = problem.title;
      }
      if (typeof problem.detail === 'string' && problem.detail) {
        detail = problem.detail;
      }
    }
  } catch {
    // Non JSON error bodies keep the HTTP status text.
  }

  return new ApiError(response.status, title, detail);
}

export interface AuthApiOptions {
  fetchImpl?: FetchLike;
  baseUrl?: string;
}

export function createAuthApi(options: AuthApiOptions = {}) {
  const doFetch: FetchLike = options.fetchImpl ?? ((input, init) => fetch(input, init));
  const baseUrl = options.baseUrl ?? '';

  async function send<T>(path: string, init: RequestInit, hasBody = true): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set('Accept', 'application/json');
    if (init.body !== undefined) {
      headers.set('Content-Type', 'application/json');
    }

    let response: Response;
    try {
      response = await doFetch(baseUrl + path, { ...init, headers });
    } catch {
      throw new ApiError(0, 'NetworkError', 'The HelixERP Shell API could not be reached.');
    }

    if (!response.ok) {
      throw await toApiError(response);
    }

    if (!hasBody || response.status === 204) {
      return undefined as T;
    }

    return (await response.json()) as T;
  }

  return {
    login(credentials: Credentials): Promise<AuthResult> {
      return send<AuthResult>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ login: credentials.login, password: credentials.password })
      });
    },

    refresh(refreshToken: string): Promise<AuthResult> {
      return send<AuthResult>('/api/auth/refresh', {
        method: 'POST',
        body: JSON.stringify({ refreshToken })
      });
    },

    async logout(accessToken: string, refreshToken: string): Promise<void> {
      await send<void>(
        '/api/auth/logout',
        {
          method: 'POST',
          headers: { Authorization: `Bearer ${accessToken}` },
          body: JSON.stringify({ refreshToken })
        },
        false
      );
    },

    async me(accessToken: string): Promise<AuthUser> {
      const user = await send<AuthUser>('/api/auth/me', {
        method: 'GET',
        headers: { Authorization: `Bearer ${accessToken}` }
      });

      return { ...user, groups: Array.isArray(user.groups) ? user.groups : [] };
    }
  };
}

export type AuthApi = ReturnType<typeof createAuthApi>;