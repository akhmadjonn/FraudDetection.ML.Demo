# Docker Deployment Guide

## Quick Start

### Production Deployment

```bash
# Navigate to docker directory
cd docker

# Build and start services
docker-compose up -d

# View logs
docker-compose logs -f fraud-detection-ml

# Stop services
docker-compose down
```

### Development/Test Deployment

```bash
# Use test configuration
docker-compose -f docker-compose.yml up -d

# Override environment
docker-compose up -d \
  -e ASPNETCORE_ENVIRONMENT=Test \
  -v ../deploy/test/appsettings.Test.json:/app/appsettings.Test.json:ro
```

## Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Set to `Production`, `Test`, or `Development`
- `DOTNET_ENVIRONMENT`: Matches `ASPNETCORE_ENVIRONMENT`

### Volume Mounts

- `ml-models`: Persists trained ML models
- `logs`: Application logs
- `clickhouse-data`: ClickHouse database data
- `clickhouse-logs`: ClickHouse logs

### Before Deployment

1. **Update ClickHouse credentials** in `docker-compose.yml`:
   ```yaml
   CLICKHOUSE_USER=your_username
   CLICKHOUSE_PASSWORD=your_secure_password
   ```

2. **Update connection string** in `deploy/prod/appsettings.Production.json`:
   ```json
   "ConnectionStrings": {
     "ClickHouse": "Host=clickhouse;Port=8123;Database=fraud_detection;Username=your_username;Password=your_password"
   }
   ```

3. **Configure notification webhook** in settings files

## Building Individual Images

```bash
# Build only the fraud detection service
docker build -f docker/Dockerfile -t fraud-detection-ml:latest ..

# Run standalone
docker run -d \
  --name fraud-detection-ml \
  -v $(pwd)/deploy/prod/appsettings.Production.json:/app/appsettings.Production.json:ro \
  fraud-detection-ml:latest
```

## Monitoring

```bash
# Check service status
docker-compose ps

# View real-time logs
docker-compose logs -f

# Check resource usage
docker stats fraud-detection-ml-host

# Access ClickHouse CLI
docker exec -it clickhouse-server clickhouse-client
```

## Troubleshooting

### Models not persisting
- Check volume permissions: `docker volume inspect ml-models`
- Verify `/app/Models` directory exists in container

### ClickHouse connection issues
- Ensure `clickhouse` service is running: `docker-compose ps clickhouse`
- Check network connectivity: `docker exec fraud-detection-ml-host ping clickhouse`
- Verify credentials match between compose and settings files

### High memory usage
- Adjust `mem_limit` in docker-compose.yml
- Monitor with: `docker stats`
