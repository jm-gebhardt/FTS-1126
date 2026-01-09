import http from 'k6/http';
import { check } from 'k6';
import type { TokenResponse, K6Response } from './types.ts';

let cachedToken: string | null = null;
let tokenExpiry: number = 0;

/**
 * Get access token from Keycloak
 *
 * @param keycloakUrl - The Keycloak base URL
 * @param realm - The Keycloak realm
 * @param clientId - The client ID
 * @param clientSecret - The client secret
 * @param username - The user name
 * @param password - The user's password
 * @returns A token or null.
 */
export function getAccessToken(
  keycloakUrl: string,
  realm: string,
  clientId: string,
  clientSecret: string,
  username: string,
  password: string
): string | null {
  const now = Date.now() / 1000;

  if (cachedToken && tokenExpiry > now + 60) {
    return cachedToken;
  }

  const tokenUrl = `${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`;

  const payload = {
    grant_type: 'password',
    client_id: clientId,
    client_secret: clientSecret,
    username: username,
    password: password,
  };

  const response = http.post(tokenUrl, payload);

  check(response, {
    'token obtained': (r: K6Response) => r.status === 200,
  });

  if (response.status === 200) {
    const body = JSON.parse(response.body as string) as TokenResponse;
    cachedToken = body.access_token;
    tokenExpiry = now + body.expires_in;
    return cachedToken;
  }

  return null;
}
