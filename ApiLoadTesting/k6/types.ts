// k6 global type declarations
export declare const __ENV: Record<string, string | undefined>;

// k6 HTTP Response type
export interface K6Response {
  status: number;
  body: string | null;
  headers: Record<string, string>;
  [key: string]: unknown;
}

// Configuration interface
export interface Config {
  keycloakUrl: string;
  keycloakRealm: string;
  keycloakClient: string;
  keycloakClientSecret: string;
  keycloakRealmUser: string;
  keycloakRealmUserPwd: string;
  keycloakRealmSu: string;
  keycloakRealmSuPwd: string;
  transportOrderApi: string;
}

// User credentials interface
export interface User {
  username: string;
  password: string;
}

// Keycloak token response interface
export interface TokenResponse {
  access_token: string;
  expires_in: number;
  refresh_expires_in: number;
  token_type: string;
}

// AGV interface
export interface Agv {
  correlationId: string;
  [key: string]: unknown;
}

// Filter node interface for action groups filtering
export interface FilterNode {
  Name?: string;
  Content?: string;
  Variant?: string;
  Aggregation?: string;
  Children?: FilterNode[];
  Child?: FilterNode;
  Combination?: string;
}
