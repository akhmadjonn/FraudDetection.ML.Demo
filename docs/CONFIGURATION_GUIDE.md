# 🔧 Complete Configuration Guide for Fraud Detection ML

## 📋 Table of Contents
1. [Configuration Overview](#configuration-overview)
2. [Quick Start](#quick-start)
3. [Configuration Files](#configuration-files)
4. [All Configuration Keys](#all-configuration-keys)
5. [Deployment Scenarios](#deployment-scenarios)
6. [Security Best Practices](#security-best-practices)

---

## Configuration Overview

### Configuration Hierarchy (Priority Order)
1. **Environment Variables** (Highest priority)
2. **User Secrets** (Development only)
3. **appsettings.{Environment}.json**
4. **appsettings.json** (Lowest priority)

### Configuration Types

| Type | Location | Committed? | Purpose |
|------|----------|------------|---------|
| **Base Config** | `appsettings.json` | ✅ Yes | Non-sensitive defaults for all environments |
| **Dev Overrides** | `appsettings.Development.json` | ❌ No | Development-specific settings |
| **Prod Overrides** | `appsettings.Production.json` | ⚠️ Yes (no secrets) | Production-specific settings |
| **Dev Secrets** | User Secrets | ❌ No | Sensitive data for local development |
| **Prod Secrets** | Environment Variables | ❌ No | Sensitive data for production |

---

## Quick Start

### 1️⃣ For Local Development

```bash
# Clone the repository
cd /home/user/FraudDetection.ML.Demo

# Copy example files
cp appsettings.example.json appsettings.json
cp appsettings.Development.example.json appsettings.Development.json

# Set up user secrets (SENSITIVE DATA)
dotnet user-secrets set "ConnectionStrings:ClickHouse" "Host=localhost;Port=8123;Database=fraud_dev;Username=default;Password=dev123"
dotnet user-secrets set "Notifications:TeamsWebhook" "YOUR-TEAMS-WEBHOOK"
dotnet user-secrets set "Notifications:TelegramBotToken" "YOUR-BOT-TOKEN"
dotnet user-secrets set "Notifications:TelegramChatId" "YOUR-CHAT-ID"

# Run the application
dotnet run
```

### 2️⃣ For Production (Linux/systemd)

```bash
# Copy production config
sudo cp appsettings.Production.example.json /opt/frauddetection/appsettings.Production.json

# Create secrets file
sudo nano /etc/frauddetection/secrets.env
# Add all ConnectionStrings, Notifications, etc.

# Set proper permissions
sudo chmod 600 /etc/frauddetection/secrets.env
sudo chown frauddetection:frauddetection /etc/frauddetection/secrets.env

# Copy and enable service
sudo cp deploy/systemd/frauddetection.service.example /etc/systemd/system/frauddetection.service
sudo systemctl daemon-reload
sudo systemctl enable frauddetection
sudo systemctl start frauddetection
```

### 3️⃣ For Production (Docker)

```bash
# Copy docker-compose file
cp docker-compose.secrets.example.yml docker-compose.yml

# Edit and add your secrets
nano docker-compose.yml

# Start services
docker-compose up -d
```

---

## Configuration Files

### 📄 appsettings.json
**Location:** `/home/user/FraudDetection.ML.Demo/appsettings.json`
**Purpose:** Base configuration, safe to commit
**Contains:**
- Logging levels
- ML training/scoring intervals
- Alert settings (fraud type configs)
- Serilog configuration

### 📄 appsettings.Development.json
**Location:** `/home/user/FraudDetection.ML.Demo/appsettings.Development.json`
**Purpose:** Development overrides
**Contains:**
- Debug logging
- Faster training intervals
- Lower data thresholds

### 📄 appsettings.Production.json
**Location:** `/home/user/FraudDetection.ML.Demo/appsettings.Production.json`
**Purpose:** Production overrides (NO SECRETS!)
**Contains:**
- Production logging paths
- Production intervals
- File retention policies

### 🔐 User Secrets
**Location:** `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`
**Purpose:** Development sensitive data
**Contains:**
- ClickHouse connection strings
- Notification webhooks/tokens

### 🌍 Environment Variables
**Location:** Deployment environment
**Purpose:** Production sensitive data
**Contains:**
- Production database credentials
- Production notification endpoints

---

## All Configuration Keys

### 🔗 ConnectionStrings

| Key | Type | Required | Example | Where to Set |
|-----|------|----------|---------|--------------|
| `ConnectionStrings:ClickHouse` | string | ✅ Yes | `Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=secret` | User Secrets (dev) / Env Vars (prod) |

**Usage in Code:** `Services/ClickHouseService.cs:16`, `Services/AlertHistoryService.cs:21`

---

### 🤖 ML Settings

| Key | Type | Default | Example | Description |
|-----|------|---------|---------|-------------|
| `ML:TrainingIntervalHours` | int | 6 | `6` | Hours between model retraining |
| `ML:MinSessionsForTraining` | int | 1000 | `1000` | Minimum sessions required to train |
| `ML:ScoringIntervalSeconds` | int | 10 | `10` | Seconds between scoring batches |
| `ML:ScoringBatchSize` | int | 100 | `100` | Number of sessions per scoring batch |
| `ML:DailyReportHour` | int | 8 | `8` | Hour of day (0-23) to send daily report |

**Usage in Code:**
- `BackgroundJobs/ModelTrainingJob.cs:21-22`
- `BackgroundJobs/RealTimeScoringJob.cs:22-23`
- `BackgroundJobs/DailyAnalysisJob.cs:19`

**Where to Set:** `appsettings.json` (can override in Production)

---

### 🚨 Alerts Settings

| Key | Type | Default | Example | Description |
|-----|------|---------|---------|-------------|
| `Alerts:EnableDatabasePersistence` | bool | true | `true` | Save alerts to database |
| `Alerts:DefaultThrottleWindowMinutes` | int | 60 | `60` | Default throttle window |
| `Alerts:ThrottleWindowHours` | int | 24 | `24` | Cache retention hours |

**Usage in Code:** `Services/HybridAlertService.cs:30-31`, `Services/AlertHistoryService.cs:26`

**Where to Set:** `appsettings.json`

---

### 🎯 Fraud Type Settings

Each fraud type has these settings:

| Setting | Type | Values | Description |
|---------|------|--------|-------------|
| `Enabled` | bool | `true`/`false` | Enable detection for this type |
| `ThrottleWindowMinutes` | int | 30-180 | Minutes before re-alerting |
| `Priority` | string | `CRITICAL`, `HIGH`, `MEDIUM`, `LOW` | Alert severity level |

**Fraud Types:**
- `Alerts:FraudTypeSettings:MultiAccounting`
- `Alerts:FraudTypeSettings:MultiDevicing`
- `Alerts:FraudTypeSettings:AccountTakeover`
- `Alerts:FraudTypeSettings:ImpossibleTravel`
- `Alerts:FraudTypeSettings:OtpBruteforce`
- `Alerts:FraudTypeSettings:DeviceSpoofing`
- `Alerts:FraudTypeSettings:VpnUsage`
- `Alerts:FraudTypeSettings:UnusualTiming`
- `Alerts:FraudTypeSettings:GeneralSuspicious`

**Example:**
```json
{
  "Alerts": {
    "FraudTypeSettings": {
      "MultiAccounting": {
        "Enabled": true,
        "ThrottleWindowMinutes": 60,
        "Priority": "HIGH"
      }
    }
  }
}
```

**Usage in Code:** `Services/HybridAlertService.cs:299-310`

**Where to Set:** `appsettings.json`

---

### 📧 Notification Settings

| Key | Type | Required | Example | Where to Set |
|-----|------|----------|---------|--------------|
| `Notifications:TeamsWebhook` | string | ⚠️ Optional | `https://outlook.office.com/webhook/...` | User Secrets / Env Vars |
| `Notifications:TelegramBotToken` | string | ⚠️ Optional | `1234567890:ABCdef...` | User Secrets / Env Vars |
| `Notifications:TelegramChatId` | string | ⚠️ Optional | `-1001234567890` | User Secrets / Env Vars |

**Usage in Code:** `Services/NotificationService.cs:22-24`

**Notification Behavior:**
- **Teams:** Receives ALL alert levels (CRITICAL, HIGH, MEDIUM, LOW) + Daily reports
- **Telegram:** Receives CRITICAL only + Daily reports

**Setup Guides:**
- [Teams Setup](./TEAMS_SETUP.md)
- [Telegram Setup](./TELEGRAM_SETUP.md)

---

### 📝 Serilog Settings

**Full configuration in `appsettings.json`:**

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/frauddetection-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

**Where to Set:** `appsettings.json` (override paths in Production)

---

## Deployment Scenarios

### 🏠 Local Development

```bash
# appsettings.json (base config) ✅
# appsettings.Development.json (dev overrides) ✅
# User Secrets (sensitive data) ✅

dotnet user-secrets set "ConnectionStrings:ClickHouse" "Host=localhost;Port=8123;..."
dotnet user-secrets set "Notifications:TelegramBotToken" "YOUR_TOKEN"
```

### 🐳 Docker Compose

```yaml
# docker-compose.yml
services:
  frauddetection-ml:
    environment:
      - ConnectionStrings__ClickHouse=Host=clickhouse;...
      - Notifications__TelegramBotToken=YOUR_TOKEN
```

### ⚙️ Linux systemd

```bash
# /etc/frauddetection/secrets.env
ConnectionStrings__ClickHouse=Host=...
Notifications__TelegramBotToken=...

# /etc/systemd/system/frauddetection.service
EnvironmentFile=/etc/frauddetection/secrets.env
```

### ☸️ Kubernetes

```yaml
# secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: frauddetection-secrets
stringData:
  ConnectionStrings__ClickHouse: "Host=..."
  Notifications__TelegramBotToken: "..."
```

---

## Security Best Practices

### ✅ DO:
- ✅ Use User Secrets for local development
- ✅ Use Environment Variables for production
- ✅ Store secrets in Azure Key Vault / AWS Secrets Manager
- ✅ Use `.gitignore` to exclude sensitive files
- ✅ Rotate credentials regularly
- ✅ Set file permissions on secret files (chmod 600)
- ✅ Use separate configs for dev/staging/prod

### ❌ DON'T:
- ❌ Commit secrets to git
- ❌ Put production credentials in appsettings.json
- ❌ Share webhook URLs publicly
- ❌ Use the same credentials across environments
- ❌ Store secrets in plain text configuration files
- ❌ Log sensitive data

---

## Environment Variable Format

**Note:** Environment variables use `__` (double underscore) instead of `:` (colon)

| JSON Format | Environment Variable Format |
|-------------|----------------------------|
| `ConnectionStrings:ClickHouse` | `ConnectionStrings__ClickHouse` |
| `Notifications:TelegramBotToken` | `Notifications__TelegramBotToken` |
| `ML:TrainingIntervalHours` | `ML__TrainingIntervalHours` |
| `Alerts:FraudTypeSettings:MultiAccounting:Enabled` | `Alerts__FraudTypeSettings__MultiAccounting__Enabled` |

---

## Verification Checklist

After configuring, verify:

- [ ] Application starts without errors
- [ ] ClickHouse connection successful
- [ ] ML models training successfully
- [ ] Real-time scoring running
- [ ] Test alert appears in Teams
- [ ] Test CRITICAL alert appears in Telegram
- [ ] Logs are being written
- [ ] No secrets in git repository

---

## Troubleshooting

### Connection String Issues:
```
❌ Error: "ClickHouse connection string not found"
✅ Solution: Set ConnectionStrings:ClickHouse in User Secrets or Environment Variables
```

### Notification Not Sent:
```
❌ Error: Alerts not appearing in Teams/Telegram
✅ Solution: Check Notifications config, verify webhook URLs, check logs
```

### Models Not Training:
```
❌ Error: "Not enough data for training"
✅ Solution: Lower ML:MinSessionsForTraining in development
```

---

## Need Help?

- 📖 [Telegram Setup Guide](./TELEGRAM_SETUP.md)
- 📖 [Teams Setup Guide](./TEAMS_SETUP.md)
- 📖 [Deployment Guide](../deploy/DEPLOYMENT.md)
