# Fraud Detection ML - Deployment Guide

## 📁 Directory Structure

```
deploy/
├── test/
│   ├── settings.json          # Test environment Serilog config
│   ├── stack.template.yml     # Test Docker Swarm stack
│   └── deploy.sh              # Test deployment script
├── prod/
│   ├── settings.json          # Production Serilog config
│   ├── stack.template.yml     # Production Docker Swarm stack
│   ├── appsettings.Production.json
│   └── deploy.sh              # Production deployment script
└── DEPLOYMENT_GUIDE.md        # This file
```

---

## 🚀 Deployment Overview

### Architecture

**Two Service Model:**
1. **fraud-detection-api**: Web API (2-3 replicas, scaled for load)
2. **fraud-detection-background**: Background jobs (1 replica only)

**Configuration Sources:**
1. Baked into image: `appsettings.json`
2. Docker Config: `/settings.json` (Serilog, ML config)
3. Docker Secrets: `/run/secrets/secrets.json` (ClickHouse, API keys)
4. Environment Variables: Highest priority overrides

---

## 📋 Prerequisites

### 1. Docker Swarm Initialized
```bash
docker swarm init
```

### 2. Create Docker Networks
```bash
# Test
docker network create --driver overlay beepul-afs-test
docker network create --driver overlay publish-https

# Production
docker network create --driver overlay beepul-afs-prod
```

### 3. Create NFS Volume Mount Point
```bash
sudo mkdir -p /nfs/fraud-detection/models
sudo chown 1000:1000 /nfs/fraud-detection/models
```

### 4. Build and Push Docker Image
```bash
# Build
docker build -t nexus.beelab.uz:8082/beepul-afs/fraud-detection:v2.0.0 \
  -f docker/Dockerfile .

# Push
docker push nexus.beelab.uz:8082/beepul-afs/fraud-detection:v2.0.0
```

---

## 🔧 Configuration Setup

### Creating Docker Secrets

**secrets.json format:**
```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=clickhouse-prod;Port=9000;Database=fraud_detection;User=fraud_user;Password=CHANGE_ME"
  },
  "ApiKeys": {
    "ValidKeys": [
      "prod-api-key-partner-1",
      "prod-api-key-partner-2"
    ]
  },
  "Notification": {
    "WebhookUrl": "https://api.telegram.org/bot<TOKEN>/sendMessage",
    "ChatId": "-1234567890"
  }
}
```

**Create Docker Secret:**
```bash
# Test
docker secret create fraud-detection-secrets-test-v1 /path/to/secrets-test.json

# Production
docker secret create fraud-detection-secrets-prod-v1 /path/to/secrets-prod.json
```

**Verify:**
```bash
docker secret ls | grep fraud-detection
```

---

## 🧪 Test Environment Deployment

### Step 1: Navigate to Test Directory
```bash
cd deploy/test
```

### Step 2: Review Configuration
```bash
# Review settings
cat settings.json

# Review stack template
cat stack.template.yml
```

### Step 3: Set Environment Variables
```bash
export IMAGE_TAG=v2.0.0
export SETTINGS_VERSION=v1
export SECRETS_VERSION=v1

# Optional: AppDynamics (if using)
export APPDYNAMICS_CONTROLLER_HOST_NAME=appdynamics.example.com
export APPDYNAMICS_AGENT_APPLICATION_NAME=FraudDetection
```

### Step 4: Deploy
```bash
./deploy.sh
```

**Or manually:**
```bash
# Create config
docker config create fraud-detection-settings-test-v1 settings.json

# Deploy stack
docker stack deploy -c stack.template.yml fraud-detection-test
```

### Step 5: Verify Deployment
```bash
# Check services
docker service ls | grep fraud-detection

# Check health
curl http://testdetection-service-dev.service-dev.afs.beelab.uz/health

# View logs
docker service logs -f fraud-detection-test_fraud-detection-api
docker service logs -f fraud-detection-test_fraud-detection-background
```

---

## 🏭 Production Deployment

### Step 1: Navigate to Production Directory
```bash
cd deploy/prod
```

### Step 2: Review Configuration
```bash
# Review settings
cat settings.json

# Review stack template
cat stack.template.yml
```

### Step 3: Set Environment Variables
```bash
export IMAGE_TAG=v2.0.0
export SETTINGS_VERSION=v1
export SECRETS_VERSION=v1

# AppDynamics
export APPDYNAMICS_CONTROLLER_HOST_NAME=appdynamics.production.com
export APPDYNAMICS_AGENT_APPLICATION_NAME=FraudDetection-Prod
export APPDYNAMICS_AGENT_ACCOUNT_ACCESS_KEY=<your-key>
```

### Step 4: Deploy (with confirmation)
```bash
./deploy.sh
# Type 'yes' to confirm
```

**Or manually:**
```bash
# Create config
docker config create fraud-detection-settings-prod-v1 settings.json

# Deploy stack
docker stack deploy -c stack.template.yml fraud-detection-prod
```

### Step 5: Verify Production Deployment
```bash
# Check services
docker service ls | grep fraud-detection-prod

# Check health
curl https://detection-service.afs.beelab.uz/health

# View logs
docker service logs fraud-detection-prod_fraud-detection-api --tail 100
docker service logs fraud-detection-prod_fraud-detection-background --tail 100

# Monitor rollout
watch docker service ps fraud-detection-prod_fraud-detection-api
```

---

## 🔄 Updating Configuration

### Updating Settings (Serilog, ML config)

**Create new config version:**
```bash
# Edit settings.json
vim settings.json

# Create new config version
docker config create fraud-detection-settings-prod-v2 settings.json

# Update environment variable
export SETTINGS_VERSION=v2

# Redeploy
docker stack deploy -c stack.template.yml fraud-detection-prod
```

### Updating Secrets (ClickHouse, API keys)

**Create new secret version:**
```bash
# Edit secrets
vim secrets-prod.json

# Create new secret version
docker secret create fraud-detection-secrets-prod-v2 secrets-prod.json

# Update environment variable
export SECRETS_VERSION=v2

# Redeploy
docker stack deploy -c stack.template.yml fraud-detection-prod
```

**Note:** Old secret versions remain until no services use them.

---

## 📊 Scaling

### Scale API Service
```bash
# Test (2 → 4 replicas)
docker service scale fraud-detection-test_fraud-detection-api=4

# Production (3 → 6 replicas)
docker service scale fraud-detection-prod_fraud-detection-api=6
```

### Background Service
**⚠️ Always keep at 1 replica** to avoid duplicate model training and processing.

---

## 🐛 Troubleshooting

### Service Not Starting

**Check logs:**
```bash
docker service logs fraud-detection-test_fraud-detection-api
```

**Common issues:**
- Missing secrets: `secret not found`
- Config not found: `config not found`
- Network not found: `network not found`

**Check service details:**
```bash
docker service ps fraud-detection-test_fraud-detection-api --no-trunc
```

### Configuration Not Loading

**Verify config mounted:**
```bash
# Get container ID
docker ps | grep fraud-detection-api

# Check file
docker exec <container-id> cat /settings.json
docker exec <container-id> cat /run/secrets/secrets.json
```

### Models Not Loading

**Check volume:**
```bash
# Check mount point
ls -la /nfs/fraud-detection/models

# Inside container
docker exec <container-id> ls -la /app/Models
```

### Health Check Failing

**Test manually:**
```bash
# Get container ID
docker ps | grep fraud-detection-api

# Test from inside
docker exec <container-id> curl http://localhost/ping
docker exec <container-id> curl http://localhost/health
```

---

## 🔙 Rollback

### Rollback to Previous Version

```bash
# Set previous image tag
export IMAGE_TAG=v1.9.0
export SETTINGS_VERSION=v1
export SECRETS_VERSION=v1

# Redeploy
docker stack deploy -c stack.template.yml fraud-detection-prod
```

### Force Rollback with Docker
```bash
# Rollback API service
docker service rollback fraud-detection-prod_fraud-detection-api

# Rollback Background service
docker service rollback fraud-detection-prod_fraud-detection-background
```

---

## 📈 Monitoring

### Service Status
```bash
# List services
docker service ls | grep fraud-detection

# Service details
docker service ps fraud-detection-prod_fraud-detection-api
```

### Logs
```bash
# Follow logs
docker service logs -f fraud-detection-prod_fraud-detection-api

# Last 100 lines
docker service logs --tail 100 fraud-detection-prod_fraud-detection-api

# Filter by keyword
docker service logs fraud-detection-prod_fraud-detection-api 2>&1 | grep "ERROR"
```

### Resource Usage
```bash
# Container stats
docker stats

# Service stats
docker service ps fraud-detection-prod_fraud-detection-api
```

### Health Checks
```bash
# API health
curl https://detection-service.afs.beelab.uz/health

# Ping
curl https://detection-service.afs.beelab.uz/ping

# Service info
curl https://detection-service.afs.beelab.uz/info
```

---

## 🔒 Security Best Practices

1. **Secrets Management**
   - Never commit secrets to git
   - Rotate secrets regularly
   - Use different secrets per environment

2. **API Keys**
   - Generate strong, random keys
   - One key per partner/service
   - Rotate keys periodically

3. **Network Security**
   - Use overlay networks
   - API behind reverse proxy (Caddy)
   - Background service internal-only

4. **Container Security**
   - Runs as non-root user (UID 1000)
   - Resource limits enforced
   - Read-only secrets mount

---

## 📝 Deployment Checklist

### Pre-Deployment
- [ ] Docker image built and pushed
- [ ] Docker secrets created
- [ ] NFS volume mounted and accessible
- [ ] Networks created
- [ ] Configuration reviewed
- [ ] Tested in Test environment first

### During Deployment
- [ ] Environment variables set
- [ ] Config version updated
- [ ] Stack deployed successfully
- [ ] Services started
- [ ] Health checks passing

### Post-Deployment
- [ ] Health endpoint responding
- [ ] Logs show no errors
- [ ] ML models loading correctly
- [ ] API accepting requests
- [ ] Background jobs running
- [ ] AppDynamics reporting
- [ ] Monitor for 30 minutes

---

## 🆘 Support

For issues:
1. Check logs
2. Verify configuration
3. Review this guide
4. Contact DevOps team

---

**Last Updated:** December 2024
