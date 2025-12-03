# Fraud Detection ML Service - Web API Architecture

## 🚀 Overview

Real-time fraud detection service powered by Machine Learning, now with **dual-mode architecture**:

1. **Web API Mode**: RESTful API for real-time fraud analysis requests
2. **Background Mode**: ML model training, batch scoring, and reporting

## 📋 Features

### Web API
- ✅ Real-time fraud detection via REST API
- ✅ API Key authentication
- ✅ Swagger/OpenAPI documentation
- ✅ Health checks and monitoring
- ✅ Horizontal scaling support

### Fraud Detection
- ✅ Multi-accounting detection
- ✅ Multi-devicing detection
- ✅ Account takeover detection
- ✅ Impossible travel detection
- ✅ Device spoofing detection
- ✅ OTP brute force detection

### Background Processing
- ✅ Automated ML model training
- ✅ Batch session scoring
- ✅ Daily fraud reports
- ✅ Alert throttling and deduplication

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────┐
│  Single Docker Image - Multiple Run Modes           │
├─────────────────────────────────────────────────────┤
│                                                      │
│  ┌──────────────────┐       ┌──────────────────┐   │
│  │   Web API Mode   │       │  Background Mode │   │
│  │   (2 replicas)   │       │   (1 replica)    │   │
│  ├──────────────────┤       ├──────────────────┤   │
│  │ • REST API       │       │ • Model Training │   │
│  │ • Controllers    │       │ • Batch Scoring  │   │
│  │ • Auth Middleware│       │ • Daily Reports  │   │
│  │ • Swagger UI     │       │ • Alert Sending  │   │
│  └──────────────────┘       └──────────────────┘   │
│           │                           │             │
│           └───────────┬───────────────┘             │
│                       │                             │
│              ┌────────▼─────────┐                   │
│              │  Shared Services  │                   │
│              ├───────────────────┤                   │
│              │ • ClickHouseService                  │
│              │ • ML Services                        │
│              │ • Analysis Services                  │
│              │ • Notification Services              │
│              └───────────────────┘                   │
│                       │                             │
└───────────────────────┼─────────────────────────────┘
                        │
                ┌───────▼────────┐
                │  ClickHouse DB  │
                │  • Sessions     │
                │  • Events       │
                │  • Analysis     │
                └─────────────────┘
```

## 🚦 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Docker & Docker Compose
- ClickHouse database
- Access to model storage (NFS/local volume)

### Running Locally

#### Web API Mode (Default)
```bash
dotnet run
```
Access Swagger UI at: `http://localhost:5000`

#### Background Mode
```bash
dotnet run -- --hangfire
```

### Running with Docker

#### Build Image
```bash
docker build -t fraud-detection:latest -f docker/Dockerfile .
```

#### Run Web API
```bash
docker run -p 80:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v /path/to/models:/app/Models \
  fraud-detection:latest
```

#### Run Background Service
```bash
docker run \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v /path/to/models:/app/Models \
  fraud-detection:latest --hangfire
```

### Running with Docker Compose

```bash
docker-compose up -d
```

This starts:
- 2x Web API replicas (load balanced)
- 1x Background service
- Shared model volume

## 📡 API Usage

### Authentication

All API requests require an API key:

```bash
curl -H "X-API-Key: your-api-key" \
  https://api.example.com/api/frauddetection/analyze
```

### Analyze Session

```bash
curl -X POST https://api.example.com/api/frauddetection/analyze \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{"SessionId": "abc-123"}'
```

### Check Health

```bash
curl https://api.example.com/health
```

**Full API Documentation:** See [API_DOCUMENTATION.md](./API_DOCUMENTATION.md)

## ⚙️ Configuration

### appsettings.json

```json
{
  "ApiKeys": {
    "ValidKeys": [
      "production-key-1",
      "partner-key-2"
    ]
  },
  "ConnectionStrings": {
    "ClickHouse": "Host=localhost;Port=9000;Database=fraud_detection"
  },
  "ML": {
    "TrainingIntervalHours": 24,
    "ScoringIntervalSeconds": 10,
    "ScoringBatchSize": 100
  },
  "Alerts": {
    "EnableDatabasePersistence": true,
    "DefaultThrottleWindowMinutes": 60
  }
}
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `ASPNETCORE_URLS` | Listening URLs | `http://+:80` |
| `ConnectionStrings__ClickHouse` | ClickHouse connection | Required |

## 🔧 Development

### Project Structure

```
Beepul.Afs.FraudDetection.ML.Host/
├── Program.cs                    # Entry point with mode detection
├── Controllers/                  # API controllers
│   ├── FraudDetectionController.cs
│   └── HealthController.cs
├── Services/                     # Business logic (shared)
│   ├── ClickHouseService.cs
│   ├── FeatureEngineeringService.cs
│   ├── IsolationForestService.cs
│   ├── ClusteringService.cs
│   ├── AnomalyAnalysisService.cs
│   ├── NotificationService.cs
│   ├── AlertHistoryService.cs
│   └── HybridAlertService.cs
├── BackgroundJobs/               # Background workers
│   ├── ModelTrainingJob.cs
│   ├── RealTimeScoringJob.cs
│   └── DailyAnalysisJob.cs
├── Extensions/                   # Service registration
│   ├── ServiceCollectionExtensions.cs
│   └── FraudDetectionHealthCheck.cs
├── Middleware/                   # API middleware
│   └── ApiKeyAuthMiddleware.cs
├── Models/                       # Data models (shared)
└── docker/
    └── Dockerfile
```

### Adding New API Endpoints

1. Create controller in `Controllers/`
2. Inject required services via constructor
3. Add authentication attribute if needed
4. Document in API_DOCUMENTATION.md

### Running Tests

```bash
dotnet test
```

## 📊 Monitoring

### Health Checks

- **Endpoint**: `GET /health`
- **Docker**: Automated via healthcheck
- **Returns**: ML model loading status

### Logging

Structured logging via Serilog:
- Console output (development)
- File output: `/var/log/frauddetection/`
- AppDynamics integration

### Metrics

AppDynamics monitors:
- API response times
- ML prediction latency
- Database query performance
- Error rates

## 🐳 Docker Deployment

### Environment Variables

Required:
- `IMAGE_TAG` - Docker image tag
- `SETTINGS_NAME` - Config name
- `SECRETS_NAME` - Secrets name

Optional (AppDynamics):
- `APPDYNAMICS_CONTROLLER_HOST_NAME`
- `APPDYNAMICS_AGENT_APPLICATION_NAME`
- etc.

### Deploy to Swarm

```bash
# Set environment variables
export IMAGE_TAG=v1.0.0
export SETTINGS_NAME=fraud-detection-settings
export SECRETS_NAME=fraud-detection-secrets

# Deploy stack
docker stack deploy -c docker-compose.yml fraud-detection

# Check status
docker service ls | grep fraud-detection

# View logs
docker service logs fraud-detection_fraud-detection-api
docker service logs fraud-detection_fraud-detection-background
```

### Scaling

```bash
# Scale API replicas
docker service scale fraud-detection_fraud-detection-api=4

# Background service should stay at 1 replica
```

## 🔒 Security

### API Keys
- Configured in `appsettings.json` or secrets
- Transmitted via `X-API-Key` header
- Validated by middleware before processing

### Network Security
- API exposed via Caddy reverse proxy
- TLS termination at proxy layer
- Background service internal-only

### Container Security
- Runs as non-root user (UID 1000)
- Read-only root filesystem (where possible)
- Limited resource allocation

## 🐛 Troubleshooting

### Models Not Loading

**Problem**: API returns 503 "ML models not yet loaded"

**Solution**:
1. Check background service is running
2. Wait 5-10 minutes for initial training
3. Verify ClickHouse has session data
4. Check shared volume permissions

```bash
docker service logs fraud-detection_fraud-detection-background
```

### API Authentication Failing

**Problem**: 401/403 errors

**Solution**:
1. Verify API key in request header
2. Check configuration has valid keys
3. Review middleware logs

```bash
docker service logs fraud-detection_fraud-detection-api
```

### Database Connection Issues

**Problem**: Cannot connect to ClickHouse

**Solution**:
1. Verify connection string in secrets/config
2. Check ClickHouse is accessible from service
3. Test connection manually

```bash
docker exec -it <container> bash
curl http://clickhouse-host:8123/ping
```

## 📝 Migration from Worker Service

### What Changed

1. ✅ **SDK**: `Microsoft.NET.Sdk.Worker` → `Microsoft.NET.Sdk.Web`
2. ✅ **Entry Point**: `Program.cs` now supports multiple modes
3. ✅ **New**: Controllers, middleware, API authentication
4. ✅ **Deployment**: Single image, multiple run modes

### What Stayed the Same

1. ✅ All business logic and services
2. ✅ ML models and algorithms
3. ✅ ClickHouse schema
4. ✅ Background jobs
5. ✅ Alert throttling logic

### Backward Compatibility

The background mode (`--hangfire`) runs exactly like the old worker service. No changes to:
- Model training
- Batch scoring
- Daily reports
- Alert sending

## 📚 Additional Documentation

- [API Documentation](./API_DOCUMENTATION.md) - Complete API reference
- [Alert Throttling Guide](./HYBRID_ALERT_THROTTLING_GUIDE.md) - Alert configuration
- [Docker Deployment](./docker-compose.yml) - Deployment configuration

## 🤝 Contributing

1. Create feature branch
2. Make changes
3. Test both API and Background modes
4. Submit pull request

## 📄 License

[Your License]

## 👥 Team

[Your Team Info]

---

**Version**: 1.0.0
**Last Updated**: December 2024
