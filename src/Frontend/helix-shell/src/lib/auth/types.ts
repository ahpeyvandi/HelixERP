export interface AuthUser {
  id: string;
  login: string;
  displayName: string;
  isSuperUser: boolean;
  groups: string[];
}

export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

export interface AuthResult {
  user: AuthUser;
  tokens: AuthTokens;
}

export interface StoredSession extends AuthResult {
  remember: boolean;
}

export interface Credentials {
  login: string;
  password: string;
}