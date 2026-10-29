import { describe, expect, it, vi } from 'vitest';
import { ApiError, createAuthApi, type FetchLike } from './authApi';
import type { AuthResult } from './types';

function jsonResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 401 ? 'Unauthorized' : 'OK',
    text: async () => (body === undefined ? '' : JSON.stringify(body)),
    json: async () => body
  } as unknown as Response;
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
    accessTokenExpiresAtUtc: '2026-10-29T12:00:00Z',
    refreshToken: 'refresh-1',
    refreshTokenExpiresAtUtc: '2026-11-12T12:00:00Z'
  }
};

describe('authApi', () => {
  it('posts credentials to the login endpoint', async () => {
    const fetchImpl = vi.fn<FetchLike>(async () => jsonResponse(RESULT));
    const api = createAuthApi({ fetchImpl });

    const result = await api.login({ login: ' admin ', password: 'Helix@12345' });

    expect(result).toEqual(RESULT);
    expect(fetchImpl).toHaveBeenCalledTimes(1);

    const [url, init] = fetchImpl.mock.calls[0];
    expect(url).toBe('/api/auth/login');
    expect(init?.method).toBe('POST');
    expect(JSON.parse(String(init?.body))).toEqual({
      login: ' admin ',
      password: 'Helix@12345'
    });
    expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json');
  });

  it('turns a problem+json 401 into an ApiError', async () => {
    const fetchImpl = vi.fn<FetchLike>(async () =>
      jsonResponse(
        { title: 'Invalid credentials', detail: 'The login name or password is incorrect.' },
        401
      )
    );
    const api = createAuthApi({ fetchImpl });

    await expect(api.login({ login: 'admin', password: 'wrong' })).rejects.toMatchObject({
      name: 'ApiError',
      status: 401,
      title: 'Invalid credentials',
      detail: 'The login name or password is incorrect.'
    });
  });

  it('reports unreachable servers as status 0', async () => {
    const fetchImpl = vi.fn<FetchLike>(async () => {
      throw new TypeError('fetch failed');
    });
    const api = createAuthApi({ fetchImpl });

    const error = await api.refresh('refresh-1').catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(0);
  });

  it('sends the refresh token to the refresh endpoint', async () => {
    const fetchImpl = vi.fn<FetchLike>(async () => jsonResponse(RESULT));
    const api = createAuthApi({ fetchImpl });

    await api.refresh('refresh-1');

    const [url, init] = fetchImpl.mock.calls[0];
    expect(url).toBe('/api/auth/refresh');
    expect(JSON.parse(String(init?.body))).toEqual({ refreshToken: 'refresh-1' });
  });

  it('calls /me with a bearer token and normalises missing groups', async () => {
    const fetchImpl = vi.fn<FetchLike>(async () =>
      jsonResponse({ id: 'user-1', login: 'admin', displayName: 'Admin', isSuperUser: false })
    );
    const api = createAuthApi({ fetchImpl });

    const user = await api.me('access-1');

    const [url, init] = fetchImpl.mock.calls[0];
    expect(url).toBe('/api/auth/me');
    expect(init?.method).toBe('GET');
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer access-1');
    expect(user.groups).toEqual([]);
  });

  it('revokes the refresh token on logout', async () => {
    const fetchImpl = vi.fn<FetchLike>(async () => jsonResponse(undefined, 204));
    const api = createAuthApi({ fetchImpl });

    await expect(api.logout('access-1', 'refresh-1')).resolves.toBeUndefined();

    const [url, init] = fetchImpl.mock.calls[0];
    expect(url).toBe('/api/auth/logout');
    expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer access-1');
    expect(JSON.parse(String(init?.body))).toEqual({ refreshToken: 'refresh-1' });
  });
});