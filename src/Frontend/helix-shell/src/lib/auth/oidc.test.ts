import { describe, expect, it, vi, afterEach } from 'vitest';
import { clearOidcCallbackHash, readOidcCallbackHash } from './oidc';

describe('OIDC callback', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('parses tokens from a callback fragment', () => {
    const hash =
      '#/oidc/callback?access_token=at-1&refresh_token=rt-1&' +
      'access_token_expires_at=2026-11-01T10%3A00%3A00Z&refresh_token_expires_at=2026-11-15T10%3A00%3A00Z';

    const result = readOidcCallbackHash(hash);

    expect(result.failed).toBe(false);
    expect(result.session).not.toBeNull();
    expect(result.session!.tokens.accessToken).toBe('at-1');
    expect(result.session!.tokens.refreshToken).toBe('rt-1');
  });

  it('returns failed when the auth marker is present', () => {
    const result = readOidcCallbackHash('#/oidc/callback?auth=failed');

    expect(result.failed).toBe(true);
    expect(result.session).toBeNull();
  });

  it('ignores hashes that are not OIDC callbacks', () => {
    const result = readOidcCallbackHash('#/dashboard');

    expect(result.failed).toBe(false);
    expect(result.session).toBeNull();
  });

  it('clears the callback hash from the address bar', () => {
    const replaceState = vi.spyOn(window.history, 'replaceState').mockImplementation(() => undefined);
    window.location.hash = '#/oidc/callback?access_token=x';

    clearOidcCallbackHash();

    expect(replaceState).toHaveBeenCalled();
  });
});
