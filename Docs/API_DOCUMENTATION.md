# Fraud Detection ML API Documentation

## Overview

The Fraud Detection ML API provides real-time fraud analysis for user sessions using Machine Learning models. The service can detect various types of fraud including multi-accounting, multi-devicing, account takeover, and impossible travel patterns.

## Architecture

The service runs in two modes:

### 1. **Web API Mode** (Default)
- Exposes REST API endpoints for real-time fraud detection
- Handles synchronous requests from other microservices
- Can be scaled horizontally for high availability
- Runs without command-line arguments

### 2. **Background Mode**
- Runs ML model training jobs
- Performs batch scoring of sessions
- Generates daily fraud reports
- Runs with `--hangfire` argument
- Should run as a single instance

## Authentication

All API endpoints (except `/health` and `/ping`) require API Key authentication.

### API Key Header
```
X-API-Key: your-api-key-here
```

### Generating API Keys

API keys are configured in `appsettings.json` or environment-specific configuration:

```json
{
  "ApiKeys": {
    "ValidKeys": [
      "your-production-key",
      "your-partner-key"
    ]
  }
}
```

## API Endpoints

### 1. Analyze Session (Real-time)

Analyzes a session for fraud in real-time using ML models.

**Endpoint:** `POST /api/frauddetection/analyze`

**Request Body:**
```json
{
  "SessionId": "string"
}
```

**Response (200 OK):**
```json
{
  "SessionId": "abc-123",
  "UserId": "user-456",
  "PhoneNumber": "+998901234567",
  "DeviceKey": "device-789",
  "AnalyzedAt": "2024-12-03T10:30:00Z",
  "RiskLevel": "HIGH",
  "AnomalyScore": 0.85,
  "IsAnomaly": true,
  "ClusterId": 2,
  "IsMultiAccounting": true,
  "IsMultiDevicing": false,
  "IsAccountTakeover": false,
  "IsImpossibleTravel": false,
  "SuspiciousReasons": [
    "⚠️ MULTI-ACCOUNTING: 5 different users on device in 24h",
    "Rooted/Jailbroken device detected"
  ],
  "AlertSent": true,
  "FraudTypesAlerted": [
    "MultiAccounting"
  ]
}
```

**Response Codes:**
- `200 OK` - Session analyzed successfully
- `400 Bad Request` - Invalid request (missing SessionId)
- `404 Not Found` - Session not found in database
- `503 Service Unavailable` - ML models not yet loaded
- `401 Unauthorized` - Missing or invalid API key
- `403 Forbidden` - Invalid API key

**Example Request (curl):**
```bash
curl -X POST https://api.example.com/api/frauddetection/analyze \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{"SessionId": "abc-123"}'
```

---

### 2. Get Session Analysis

Retrieves existing fraud analysis result for a session.

**Endpoint:** `GET /api/frauddetection/session/{sessionId}`

**Parameters:**
- `sessionId` (path) - The session identifier

**Response (200 OK):**
Same as Analyze Session response

**Response Codes:**
- `200 OK` - Analysis found
- `404 Not Found` - No analysis found for session
- `401 Unauthorized` - Missing or invalid API key
- `403 Forbidden` - Invalid API key

**Example Request (curl):**
```bash
curl https://api.example.com/api/frauddetection/session/abc-123 \
  -H "X-API-Key: your-api-key"
```

---

### 3. Get User Fraud History

Retrieves fraud history for a specific user.

**Endpoint:** `GET /api/frauddetection/user/{userId}/history`

**Parameters:**
- `userId` (path) - The user identifier
- `limit` (query, optional) - Number of results (default: 10)

**Response (200 OK):**
```json
[
  {
    "SessionId": "session-1",
    "UserId": "user-456",
    "RiskLevel": "HIGH",
    "AnomalyScore": 0.85,
    "AnalyzedAt": "2024-12-03T10:30:00Z",
    ...
  },
  {
    "SessionId": "session-2",
    "UserId": "user-456",
    "RiskLevel": "MEDIUM",
    "AnomalyScore": 0.65,
    "AnalyzedAt": "2024-12-02T14:20:00Z",
    ...
  }
]
```

**Example Request (curl):**
```bash
curl https://api.example.com/api/frauddetection/user/user-456/history?limit=20 \
  -H "X-API-Key: your-api-key"
```

---

### 4. Get Device Fraud History

Retrieves fraud history for a specific device.

**Endpoint:** `GET /api/frauddetection/device/{deviceKey}/history`

**Parameters:**
- `deviceKey` (path) - The device key identifier
- `limit` (query, optional) - Number of results (default: 10)

**Response (200 OK):**
Same as User History response

**Example Request (curl):**
```bash
curl https://api.example.com/api/frauddetection/device/device-789/history?limit=20 \
  -H "X-API-Key: your-api-key"
```

---

### 5. Health Check

Detailed health check with ML model status.

**Endpoint:** `GET /health`

**Authentication:** None required

**Response (200 OK - Healthy):**
```json
{
  "Status": "Healthy",
  "Timestamp": "2024-12-03T10:30:00Z",
  "Models": {
    "IsolationForest": {
      "Loaded": true,
      "Status": "Ready"
    },
    "Clustering": {
      "Loaded": true,
      "Status": "Ready"
    }
  },
  "Message": "All ML models loaded and ready"
}
```

**Response (503 Service Unavailable - Degraded):**
```json
{
  "Status": "Degraded",
  "Timestamp": "2024-12-03T10:30:00Z",
  "Models": {
    "IsolationForest": {
      "Loaded": false,
      "Status": "Not Loaded"
    },
    "Clustering": {
      "Loaded": false,
      "Status": "Not Loaded"
    }
  },
  "Message": "ML models not yet loaded. Please wait for model training to complete."
}
```

---

### 6. Ping

Simple ping endpoint for Docker health checks.

**Endpoint:** `GET /ping`

**Authentication:** None required

**Response (200 OK):**
```json
{
  "Status": "OK",
  "Timestamp": "2024-12-03T10:30:00Z"
}
```

---

### 7. Service Info

Get service information and version.

**Endpoint:** `GET /info`

**Authentication:** None required

**Response (200 OK):**
```json
{
  "Service": "Fraud Detection ML API",
  "Version": "1.0.0",
  "Environment": "Production",
  "Timestamp": "2024-12-03T10:30:00Z"
}
```

---

## Risk Levels

The API returns one of four risk levels:

| Risk Level | Score Range | Description |
|-----------|-------------|-------------|
| **CRITICAL** | 0.85+ | Severe fraud indicators - immediate action required |
| **HIGH** | 0.70-0.84 | Multiple red flags detected |
| **MEDIUM** | 0.50-0.69 | Some concerning behavior |
| **LOW** | < 0.50 | Normal or minor concerns |

## Fraud Types Detected

- **Multi-Accounting**: Multiple user accounts on the same device
- **Multi-Devicing**: Single user accessing from multiple devices
- **Account Takeover**: User account appearing on many new devices
- **Impossible Travel**: User appearing in different locations impossibly fast
- **OTP Brute Force**: Multiple OTP failures
- **Device Spoofing**: Rooted, jailbroken, cloned, or emulated devices
- **VPN Usage**: VPN connection detected
- **Unusual Timing**: Activity during suspicious hours (2-5 AM)

## Response Times

- **Analyze Session**: 100-500ms (depending on session complexity)
- **Get History**: 50-200ms
- **Health Check**: < 10ms
- **Ping**: < 5ms

## Rate Limiting

Currently no rate limiting is enforced at the API level. Implement rate limiting at the reverse proxy (Caddy/Nginx) if needed.

## Error Handling

All errors return a consistent format:

```json
{
  "Error": "Error Type",
  "Message": "Human-readable error message"
}
```

Common HTTP status codes:
- `400` - Bad Request (invalid input)
- `401` - Unauthorized (missing API key)
- `403` - Forbidden (invalid API key)
- `404` - Not Found (resource doesn't exist)
- `500` - Internal Server Error
- `503` - Service Unavailable (models not loaded)

## Swagger/OpenAPI

In development mode, Swagger UI is available at the root URL:

```
http://localhost:5000/
```

The OpenAPI specification is available at:

```
http://localhost:5000/swagger/v1/swagger.json
```

**Note:** Swagger is disabled in production for security.

## Integration Example (C#)

```csharp
public class FraudDetectionClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public FraudDetectionClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    }

    public async Task<FraudAnalysisResponse> AnalyzeSessionAsync(string sessionId)
    {
        var request = new { SessionId = sessionId };
        var response = await _httpClient.PostAsJsonAsync(
            "/api/frauddetection/analyze",
            request);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FraudAnalysisResponse>();
    }

    public async Task<List<FraudAnalysisResponse>> GetUserHistoryAsync(
        string userId,
        int limit = 10)
    {
        var response = await _httpClient.GetAsync(
            $"/api/frauddetection/user/{userId}/history?limit={limit}");

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<FraudAnalysisResponse>>();
    }
}
```

## Monitoring

### AppDynamics

The service is instrumented with AppDynamics for monitoring:
- API endpoint performance
- ML model prediction times
- Database query performance
- Error rates and exceptions

### Serilog Logging

All requests are logged with:
- Request path and method
- Response status code
- Processing time
- User/session identifiers
- Fraud detection results

Log levels:
- `Information`: Normal operations
- `Warning`: High/Critical risk sessions, throttled alerts
- `Error`: Processing errors, exceptions
- `Critical`: Service failures, model loading issues

## Deployment

### Running Locally (Web API Mode)

```bash
dotnet run
```

Access Swagger UI at `http://localhost:5000`

### Running Locally (Background Mode)

```bash
dotnet run -- --hangfire
```

### Docker Deployment

```bash
# Build image
docker build -t fraud-detection:latest -f docker/Dockerfile .

# Run Web API
docker run -p 80:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v /path/to/models:/app/Models \
  fraud-detection:latest

# Run Background
docker run \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v /path/to/models:/app/Models \
  fraud-detection:latest --hangfire
```

### Docker Swarm Deployment

```bash
docker stack deploy -c docker-compose.yml fraud-detection
```

## Troubleshooting

### ML Models Not Loading

**Symptom:** API returns 503 "ML models not yet loaded"

**Solutions:**
1. Wait 5-10 minutes for background service to train models
2. Check background service logs for training errors
3. Verify ClickHouse connection and data availability
4. Ensure shared volume has trained model files

### API Key Authentication Failing

**Symptom:** 401/403 errors

**Solutions:**
1. Verify `X-API-Key` header is present
2. Check API key is configured in `appsettings.json`
3. Verify case-sensitive key match
4. Check logs for authentication attempts

### Session Not Found

**Symptom:** 404 "Session not found"

**Solutions:**
1. Verify session exists in ClickHouse Sessions table
2. Check SessionId format and spelling
3. Verify ClickHouse connection string
4. Check session hasn't been archived/deleted

## Support

For issues and questions:
- Check logs: `/var/log/frauddetection/`
- Review AppDynamics dashboards
- Contact DevOps team
- GitHub Issues: [repository URL]
