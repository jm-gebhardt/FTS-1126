# API load testing

## Test Scenario

This load test simulates user behavior against the Transport Order API with the following workflow:

1. **Authentication**: Obtains access token from Keycloak using password grant flow
2. **Fetch AGVs**: Retrieves all available AGVs via `GET /agvs`
3. **Filter Action Groups**: Performs complex filtering on action groups using `POST /actionGroups/filtered`
   - Filters by AGV correlation IDs (from step 2)
   - Filters by action types (Charge, Pick)
   - Filters by estimated duration range
   - Filters by state (Completed, Canceled, Aborted)
   - Filters by log entries (Error, Warning, specific log codes)

### Endpoints Tested

- `POST {KEYCLOAK_URL}/realms/{realm}/protocol/openid-connect/token` - Authentication
- `GET {TRANSPORTORDER_API}/agvs` - Retrieve AGV list
- `POST {TRANSPORTORDER_API}/actionGroups/filtered?pageNumber=0&pageSize=15` - Filter action groups with complex criteria

### Load Profiles

The test runs three scenarios sequentially (**total duration: ~36 minutes**):

#### 1. Default Load Test (0:00 - 2:00)
- Ramp up: 30 seconds to 10 virtual users
- Sustained load: 1 minute ramping to 50 virtual users
- Ramp down: 30 seconds to 0 users
- **Purpose**: Baseline performance testing under normal load

#### 2. Spike Test (2:00 - 2:50)
- Sudden spike: 10 seconds to 100 concurrent users
- Hold spike: 30 seconds at 100 users
- Rapid drop: 10 seconds to 0 users
- **Purpose**: Identifies issues with connection pooling, and rate limiting under sudden load spikes

#### 3. Soak/Endurance Test (3:00 - 36:00)
- Ramp up: 2 minutes to 30 users
- Sustained load: 30 minutes at 30 concurrent users
- Ramp down: 1 minute to 0 users
- **Purpose**: Identifies memory leaks, connection pool exhaustion, disk space issues, and degradation over time

### Customizing Scenarios

To run only specific scenarios, comment out unwanted scenarios in `script.ts`:

```javascript
export const options = {
  scenarios: {
    default_load: { /* ... */ },
    // spike_test: { /* ... */ },     // Commented out
    // soak_test: { /* ... */ },      // Commented out
  },
};
```

## Configure and start the fleetmanager

In `tools` run script `01_create_load_testing_env.sh` (make executable if needed).


`sudo docker compose up -d`

Let it run for a while until some transport orders are completed.

## Run api load test

` sudo docker compose -f ACC/ApiLoadTesting/docker-compose.api-load-test.yml --env-file .env up -d k6`

Then open Grafana and check the dashboard: "k6 Load Testing Results (Prometheus)"

## Understanding the Dashboard

The Grafana dashboard provides comprehensive visibility into load test performance across multiple dimensions.

### Dashboard Variables

At the top of the dashboard, you can configure:

- **scenario**: Select which test scenario to analyze (`default_load`, `spike_test`, or `soak_test`)
- **domain**: Filter by domain/service (e.g., `transportorder`, `keycloak`)
- **HttpMetricName**: Choose which HTTP timing metric to analyze:
  - `duration` - Total request duration (most commonly used)
  - `waiting` - Time waiting for server response (TTFB)
  - `receiving` - Time receiving response data
  - `sending` - Time sending request data
  - `blocked` - Time blocked waiting for connection
  - `connecting` - Time establishing TCP connection
  - `tls_handshaking` - Time performing TLS handshake

### Key Metrics Overview (Top Row)

**Virtual Users**
- Shows current active virtual users and maximum configured users
- Use to verify the load profile is executing as expected
- Should match the configured ramp-up/ramp-down pattern

**Requests**
- Total number of HTTP requests completed for the selected scenario
- Indicates overall test activity volume

**Iterations per second**
- Rate at which complete test iterations execute
- Each iteration runs the full workflow (auth → get AGVs → filter action groups)
- Higher values indicate better throughput

**Data Received/Sent**
- Network bandwidth consumed in bytes per second
- Helps identify network bottlenecks or unexpectedly large payloads

### Performance Analysis

**Throughput (Time Series Chart)**
- Shows HTTP request volume over time, grouped by status code
- **What to look for**:
  - Steady green line (HTTP 200) indicates healthy performance
  - Red/yellow lines indicate errors (4xx/5xx status codes)
  - Throughput should scale proportionally with virtual users
  - Sudden drops may indicate server overload or crashes

**Status Code Table**
- Breakdown of all HTTP responses by status code and method
- **What to look for**:
  - HTTP 200: Successful requests (should be majority)
  - HTTP 401: Authentication failures (investigate credentials)
  - HTTP 4xx: Client errors (check request formatting)
  - HTTP 5xx: Server errors (indicates backend issues)

**Response Time Metrics (Stats Panel)**
- Four panels showing average, P95, max, and min response times
- Values are in milliseconds for the selected HttpMetricName
- **What to look for**:
  - **avg**: Baseline performance indicator
  - **P95**: 95% of requests complete within this time (critical SLA metric)
  - **max**: Worst-case latency (watch for outliers)
  - **min**: Best-case latency (baseline lower bound)

**K6 Latency (Time Series Chart)**
- Plots P90 and P95 response times over the test duration
- **What to look for**:
  - Stable horizontal lines indicate consistent performance
  - Rising trend suggests performance degradation (memory leaks, resource exhaustion)
  - Spikes during load increases are normal, but should stabilize
  - Compare across scenarios: spike test will show temporary increases, soak test reveals long-term trends

**Query Table (Bottom)**
- Detailed breakdown of requests by URL and status code
- Shows exact request counts for each endpoint
- **What to look for**:
  - Verify all expected endpoints are being hit
  - Check distribution of requests across endpoints
  - Identify which endpoints have errors (non-200 status)
  - Use to correlate errors with specific API operations

### Interpreting Results

**Successful Load Test**
- Status codes predominantly HTTP 200
- P95 latency remains stable throughout test duration
- Throughput scales linearly with virtual users
- No memory/resource degradation during soak test

**Performance Issues**
- Rising P95 latency over time → memory leak or resource exhaustion
- High error rates (4xx/5xx) → backend capacity exceeded
- Throughput plateaus despite increasing users → bottleneck identified
- Large gap between avg and P95 → inconsistent performance

**Spike Test Interpretation**
- Expect temporary latency spike when load jumps to 100 users
- System should recover when load drops
- Error rates indicate auto-scaling or rate limiting thresholds

**Soak Test Interpretation**
- Monitor for gradual performance degradation
- Watch memory/connection metrics in backend services
- Sustained stable performance indicates production readiness

Afterwards remove the orphan container of k6:

```
sudo docker container ls --all
sudo docker container rm <k6_container_id>
```
