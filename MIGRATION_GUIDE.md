# Migration Guide: Worker Service → Web API Architecture

## Overview

This guide explains the migration from a pure Worker Service to a dual-mode architecture supporting both Web API and Background modes.

## ✅ Completed Changes

### 1. Project Configuration

**File**: `Beepul.Afs.FraudDetection.ML.Host.csproj`

- ✅ Changed SDK from `Microsoft.NET.Sdk.Worker` to `Microsoft.NET.Sdk.Web`
- ✅ Added Web API packages:
  - `Microsoft.AspNetCore.OpenApi` (8.0.0)
  - `Swashbuckle.AspNetCore` (6.5.0)
  - `Microsoft.AspNetCore.Authentication` (2.2.0)
  - `Serilog.AspNetCore` (8.0.0)

### 2. New Folder Structure

```
✅ Controllers/                  # NEW
   ├── FraudDetectionController.cs
   └── HealthController.cs

✅ Extensions/                   # NEW
   ├── ServiceCollectionExtensions.cs
   └── FraudDetectionHealthCheck.cs

✅ Middleware/                   # NEW
   └── ApiKeyAuthMiddleware.cs

✅ Services/                     # UNCHANGED (existing)
✅ BackgroundJobs/               # UNCHANGED (existing)
✅ Models/                       # UNCHANGED (existing)
```

### 3. Program.cs Refactoring

**File**: `Program.cs`

- ✅ Added run mode detection (Web API vs Background)
- ✅ Created `RunWebApiAsync()` method for API mode
- ✅ Created `RunBackgroundAsync()` method for background mode
- ✅ Both modes share same service registrations
- ✅ Default mode: Web API (no args)
- ✅ Background mode: `--hangfire` argument

### 4. Service Registration

**File**: `Extensions/ServiceCollectionExtensions.cs`

Three extension methods created:
- ✅ `AddFraudDetectionServices()` - Core services (shared)
- ✅ `AddWebApiServices()` - API-specific (controllers, Swagger, health checks)
- ✅ `AddBackgroundJobs()` - Background jobs

### 5. API Controllers

**File**: `Controllers/FraudDetectionController.cs`

Endpoints created:
- ✅ `POST /api/frauddetection/analyze` - Real-time fraud analysis
- ✅ `GET /api/frauddetection/session/{id}` - Get existing analysis
- ✅ `GET /api/frauddetection/user/{id}/history` - User fraud history
- ✅ `GET /api/frauddetection/device/{id}/history` - Device fraud history

**File**: `Controllers/HealthController.cs`

- ✅ `GET /ping` - Simple ping for Docker health checks
- ✅ `GET /health` - Detailed health with model status
- ✅ `GET /info` - Service information

### 6. Authentication Middleware

**File**: `Middleware/ApiKeyAuthMiddleware.cs`

- ✅ API Key validation via `X-API-Key` header
- ✅ Exempts `/health`, `/ping`, `/swagger` from auth
- ✅ Returns proper 401/403 error responses

### 7. ClickHouse Service Extensions

**File**: `Services/ClickHouseService.cs`

Added new methods:
- ✅ `GetSessionsByIdAsync()` - Get session by ID
- ✅ `GetAnalysisResultBySessionIdAsync()` - Get analysis by session
- ✅ `GetUserFraudHistoryAsync()` - Get user history
- ✅ `GetDeviceFraudHistoryAsync()` - Get device history

### 8. Configuration

**File**: `appsettings.json`

- ✅ Added `ApiKeys` section with sample keys
- ✅ Added ASP.NET Core logging configuration

### 9. Docker Configuration

**File**: `docker/Dockerfile`

- ✅ Added `EXPOSE 80` for Web API
- ✅ Added `ASPNETCORE_URLS` environment variable
- ✅ Added comments explaining run modes

**File**: `docker-compose.yml` (NEW)

- ✅ Created `fraud-detection-api` service (2 replicas)
- ✅ Created `fraud-detection-background` service (1 replica)
- ✅ Shared model volume configuration
- ✅ Health checks for API service
- ✅ Network and secrets configuration

### 10. Documentation

Created files:
- ✅ `API_DOCUMENTATION.md` - Complete API reference
- ✅ `README_WEBAPI.md` - Architecture overview and usage
- ✅ `MIGRATION_GUIDE.md` - This file

## 🔧 What Stayed the Same

### Unchanged Components

1. ✅ **All Business Logic**
   - `ClickHouseService`
   - `FeatureEngineeringService`
   - `IsolationForestService`
   - `ClusteringService`
   - `AnomalyAnalysisService`
   - `NotificationService`
   - `AlertHistoryService`
   - `HybridAlertService`

2. ✅ **Background Jobs**
   - `ModelTrainingJob`
   - `RealTimeScoringJob`
   - `DailyAnalysisJob`

3. ✅ **ML Algorithms**
   - Isolation Forest implementation
   - Clustering implementation
   - Feature engineering logic

4. ✅ **Data Models**
   - All models in `Models/` folder
   - ClickHouse schema
   - Analysis result structure

5. ✅ **Alert System**
   - Fraud type detection
   - Alert throttling
   - Notification sending

## 📋 Deployment Checklist

### Before Deployment

- [ ] Review and update API keys in production config
- [ ] Configure ClickHouse connection string
- [ ] Set up shared model storage (NFS volume)
- [ ] Configure AppDynamics variables
- [ ] Review resource limits in docker-compose.yml
- [ ] Update Caddy reverse proxy configuration

### Deployment Steps

1. **Build Docker Image**
   ```bash
   docker build -t nexus.beelab.uz:8082/beepul-afs/fraud-detection:v2.0.0 -f docker/Dockerfile .
   docker push nexus.beelab.uz:8082/beepul-afs/fraud-detection:v2.0.0
   ```

2. **Update Environment Variables**
   ```bash
   export IMAGE_TAG=v2.0.0
   export SETTINGS_NAME=fraud-detection-settings
   export SECRETS_NAME=fraud-detection-secrets
   ```

3. **Deploy Stack**
   ```bash
   docker stack deploy -c docker-compose.yml fraud-detection
   ```

4. **Verify Deployment**
   ```bash
   # Check services are running
   docker service ls | grep fraud-detection

   # Check API health
   curl http://api-endpoint/health

   # Check logs
   docker service logs fraud-detection_fraud-detection-api
   docker service logs fraud-detection_fraud-detection-background
   ```

### After Deployment

- [ ] Verify API responds to requests
- [ ] Test API key authentication
- [ ] Verify background jobs are running
- [ ] Check ML models are loading
- [ ] Monitor AppDynamics dashboards
- [ ] Test fraud detection functionality
- [ ] Verify alert sending works

## 🧪 Testing

### Local Testing

**Test Web API Mode:**
```bash
# Terminal 1: Start API
dotnet run

# Terminal 2: Test endpoints
curl http://localhost:5000/health
curl -H "X-API-Key: dev-key-12345" http://localhost:5000/info

# Access Swagger
open http://localhost:5000
```

**Test Background Mode:**
```bash
dotnet run -- --hangfire
# Watch logs for model training and scoring
```

### Docker Testing

```bash
# Build
docker build -t fraud-detection:test -f docker/Dockerfile .

# Test Web API
docker run -p 5000:80 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -v $(pwd)/Models:/app/Models \
  fraud-detection:test

# Test Background
docker run \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -v $(pwd)/Models:/app/Models \
  fraud-detection:test --hangfire
```

### Integration Testing

```bash
# Test fraud detection flow
curl -X POST http://localhost:5000/api/frauddetection/analyze \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-key-12345" \
  -d '{"SessionId": "test-session-123"}'

# Test user history
curl -H "X-API-Key: dev-key-12345" \
  "http://localhost:5000/api/frauddetection/user/test-user/history?limit=5"
```

## 🔐 Security Considerations

### API Keys

**Development Keys** (in appsettings.json):
```json
{
  "ApiKeys": {
    "ValidKeys": [
      "dev-key-12345",
      "test-key-67890"
    ]
  }
}
```

**Production Keys** (in secrets):
- Generate strong, random keys
- Rotate regularly
- Use separate keys per partner/service
- Never commit to source control

**Key Generation Example:**
```bash
# Generate a secure API key
openssl rand -hex 32
```

### Network Security

- API exposed via reverse proxy only
- Background service internal-only
- Use TLS for all external communication
- Implement rate limiting at proxy level

## 📊 Monitoring Setup

### Health Checks

**Docker Swarm:**
- Automatic health checks configured
- 10s interval, 5s timeout, 5 retries
- 10s start period for initialization

**Manual Check:**
```bash
curl http://api-endpoint/health
```

### Logging

**Log Locations:**
- Container: `/var/log/frauddetection/`
- Docker: `docker service logs <service-name>`

**Log Levels:**
- Development: `Information`
- Production: `Warning` (for ASP.NET), `Information` (for app)

### AppDynamics

Configure environment variables:
- `APPDYNAMICS_CONTROLLER_HOST_NAME`
- `APPDYNAMICS_AGENT_APPLICATION_NAME`
- `APPDYNAMICS_AGENT_TIER_NAME` (suffix `-api` or `-background`)

## 🐛 Troubleshooting

### Common Issues

**Issue 1: "dotnet: command not found" during build**
- Solution: Ensure .NET 8.0 SDK is installed

**Issue 2: API returns 503 "Models not loaded"**
- Solution: Wait for background service to train models (5-10 min)
- Check: `docker service logs fraud-detection_fraud-detection-background`

**Issue 3: API Key authentication fails**
- Solution: Verify `X-API-Key` header is present and matches config
- Check: API key is in correct configuration file

**Issue 4: Cannot connect to ClickHouse**
- Solution: Verify connection string in secrets/config
- Check: Network connectivity from container

**Issue 5: Models not shared between services**
- Solution: Verify volume mount is correct
- Check: Both services mount same volume path

## 🔄 Rollback Plan

If issues occur, rollback to previous version:

```bash
# Set to previous version
export IMAGE_TAG=v1.0.0

# Redeploy
docker stack deploy -c docker-compose-old.yml fraud-detection

# Or scale down new, scale up old
docker service scale fraud-detection_fraud-detection-api=0
docker service scale fraud-detection-old_api=2
```

## 📝 Configuration Management

### Environment-Specific Settings

**Development** (`appsettings.Development.json`):
- Swagger enabled
- Verbose logging
- Test API keys

**Testing** (`appsettings.Testing.json`):
- Swagger enabled
- Moderate logging
- Test API keys

**Production** (`appsettings.Production.json`):
- Swagger disabled
- Minimal logging
- Production API keys (from secrets)

### Secrets Management

Store in Docker Secrets or environment variables:
- ClickHouse connection strings
- API keys
- Notification service credentials
- AppDynamics keys

## 🎯 Performance Tuning

### API Service

**Resource Limits:**
- Memory: 512Mi per replica
- CPU: 0.5 per replica
- Replicas: 2 (can scale to 4-8)

**Tuning:**
- Increase replicas for higher throughput
- Monitor response times in AppDynamics
- Consider caching frequently accessed data

### Background Service

**Resource Limits:**
- Memory: 768Mi (higher for ML training)
- CPU: 1.0 (higher for ML training)
- Replicas: 1 (do not scale)

**Tuning:**
- Adjust training interval based on data volume
- Tune scoring batch size and interval
- Monitor model training times

## ✅ Success Criteria

Deployment is successful when:

- [ ] Both API and Background services are running
- [ ] Health check returns "Healthy" with models loaded
- [ ] API responds to authenticated requests
- [ ] Background jobs are processing sessions
- [ ] Models are training successfully
- [ ] Alerts are being sent
- [ ] No errors in logs
- [ ] AppDynamics shows normal metrics

## 📞 Support

For issues:
1. Check service logs
2. Review health checks
3. Check AppDynamics dashboards
4. Contact DevOps team
5. Escalate to development team

---

**Migration Date**: December 2024
**Version**: 2.0.0
**Status**: ✅ Complete
