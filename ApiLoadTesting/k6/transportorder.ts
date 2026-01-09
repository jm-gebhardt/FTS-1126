import http from 'k6/http';
import { check } from 'k6';
import type { Agv, FilterNode, K6Response } from './types.ts';

/**
 * Get all agvs from API GET: /agvs.
 *
 * @param apiBaseUrl - The transport order API base URL
 * @param params - The header params (authentication).
 * @returns List of agv correlation ids.
 */
export function getAgvs(apiBaseUrl: string, params: { headers: Record<string, string> }): string[] {
  const apiUrl = `${apiBaseUrl}/agvs`;
  const response = http.get(apiUrl, params);
  check(response, {
    'status is 200': (r: K6Response) => r.status === 200,
    'authorized': (r: K6Response) => r.status !== 401,
  });

  if (response.status !== 200) {
    console.log(`Failed to get AGVs: ${response.status}`);
    return [];
  }

  const agvs = JSON.parse(response.body as string) as Agv[];

  return agvs.map((agv) => agv.correlationId);
}

/**
 * Build the body for filtering on GET /actionGroups/filtered
 *
 * @param agvCorrelationIds - The agv correlation ids to be filtered.
 * @returns A filter object.
 */
export function buildFilterBody(agvCorrelationIds: string[]): FilterNode {
  const agvFilters = agvCorrelationIds.map((id) => ({
    Name: 'AssignedAgv.CorrelationId',
    Content: id,
    Variant: 'Equals',
  }));

  return {
    Children: [
      {
        Children: agvFilters,
        Combination: 'Or',
      },
      {
        Name: 'Actions',
        Aggregation: 'Any',
        Child: {
          Children: [
            {
              Name: 'Actions.ActionType',
              Content: 'Charge',
              Variant: 'Equals',
            },
            {
              Name: 'Actions.ActionType',
              Content: 'Pick',
              Variant: 'Equals',
            },
          ],
          Combination: 'Or',
        },
      },
      {
        Children: [
          {
            Name: 'EstimatedDuration',
            Content: '0',
            Variant: 'Gt',
          },
          {
            Name: 'EstimatedDuration',
            Content: '10675199.02:48:05.4775807',
            Variant: 'Lt',
          },
        ],
        Combination: 'Or',
      },
      {
        Children: [
          {
            Name: 'State',
            Content: 'Completed',
            Variant: 'Equals',
          },
          {
            Name: 'State',
            Content: 'Canceled',
            Variant: 'Equals',
          },
          {
            Name: 'State',
            Content: 'Aborted',
            Variant: 'Equals',
          },
        ],
        Combination: 'Or',
      },
      {
        Name: 'LogEntries',
        Aggregation: 'Where',
        Child: {
          Children: [
            {
              Name: 'LogEntries.LogLevel',
              Content: 'Error',
              Variant: 'Equals',
            },
            {
              Name: 'LogEntries.LogLevel',
              Content: 'Warning',
              Variant: 'Equals',
            },
            {
              Name: 'LogEntries.LogCode',
              Content: '117',
              Variant: 'Equals',
            },
          ],
          Combination: 'Or',
        },
      },
    ],
    Combination: 'And',
  };
}

/**
 * Get filtered action groups from API POST: /actionGroups/filtered
 *
 * @param apiBaseUrl - The transport order API base URL
 * @param agvCorrelationIds - The agv correlation ids to filter by
 * @param params - The header params (authentication).
 * @param pageNumber - The page number (default: 0)
 * @param pageSize - The page size (default: 15)
 */
export function getFilteredActionGroups(
  apiBaseUrl: string,
  agvCorrelationIds: string[],
  params: { headers: Record<string, string> },
  pageNumber: number = 0,
  pageSize: number = 15
): void {
  const filterBody = buildFilterBody(agvCorrelationIds);
  const filterUrl = `${apiBaseUrl}/actionGroups/filtered?pageNumber=${pageNumber}&pageSize=${pageSize}`;
  const filterResponse = http.post(filterUrl, JSON.stringify(filterBody), params);

  check(filterResponse, {
    'actionGroups/filtered status is 200': (r: K6Response) => r.status === 200,
    'actionGroups/filtered authorized': (r: K6Response) => r.status !== 401,
  });
}
