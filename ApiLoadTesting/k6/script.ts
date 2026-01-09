import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import type { Config, User } from './types.ts';
import { getAccessToken } from './keycloak.ts';
import { getAgvs, getFilteredActionGroups } from './transportorder.ts';

// k6 global type declaration
declare const __ENV: Record<string, string | undefined>;

const Config: Config = {
  keycloakUrl: __ENV.KEYCLOAK_URL || '',
  keycloakRealm: __ENV.KEYCLOAK_REALM || 'agv',
  keycloakClient: __ENV.KEYCLOAK_CLIENT || 'agv-kitt',
  keycloakClientSecret: __ENV.KEYCLOAK_CLIENT_SECRET || '',
  keycloakRealmUser: __ENV.KEYCLOAK_REALM_USER || 'agv',
  keycloakRealmUserPwd: __ENV.KEYCLOAK_REALM_USER_PWD || '',
  keycloakRealmSu: __ENV.KEYCLOAK_REALM_SU || 'agv',
  keycloakRealmSuPwd: __ENV.KEYCLOAK_REALM_SU_PWD || '',
  transportOrderApi: __ENV.TRANSPORTORDER_API || ''
};

const users = new SharedArray<User>('users', function (): User[] {
  return [
    { username: Config.keycloakRealmUser, password: Config.keycloakRealmUserPwd },
    { username: Config.keycloakRealmSu, password: Config.keycloakRealmSuPwd }
  ];
});

/**
 * Options used by k6 to define scenarios
 */
export const options: {
  scenarios: Record<string, unknown>;
} = {
  scenarios: {
    // Default load test
    default_load: {
      executor: 'ramping-vus',
      startTime: '0s',
      stages: [
        { duration: '30s', target: 10 },
        { duration: '1m', target: 50 },
        { duration: '30s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },

    // Spike test - runs after default load
    spike_test: {
      executor: 'ramping-vus',
      startTime: '2m',  // Starts after default_load finishes
      stages: [
        { duration: '10s', target: 100 },  // Sudden spike
        { duration: '30s', target: 100 },  // Hold
        { duration: '10s', target: 0 },    // Drop
      ],
      gracefulRampDown: '10s',
    },

    // Soak test - runs after spike test
    soak_test: {
      executor: 'ramping-vus',
      startTime: '3m',  // Starts after spike_test finishes
      stages: [
        { duration: '2m', target: 30 },     // Ramp up
        { duration: '30m', target: 30 },    // Sustained load
        { duration: '1m', target: 0 },      // Ramp down
      ],
      gracefulRampDown: '10s',
    },
  },
};

/**
 * Main function executed by k6
 */
export default function (): void {
  const user = users[Math.floor(Math.random() * users.length)];
  const token = getAccessToken(
    Config.keycloakUrl,
    Config.keycloakRealm,
    Config.keycloakClient,
    Config.keycloakClientSecret,
    user.username,
    user.password
  );
  if (!token) {
    return;
  }
  const params: { headers: Record<string, string> } = {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  };

  const agvCorrelationIds = getAgvs(Config.transportOrderApi, params);
  if (agvCorrelationIds.length > 0) {
    getFilteredActionGroups(Config.transportOrderApi, agvCorrelationIds, params);
  }

  sleep(1);
}