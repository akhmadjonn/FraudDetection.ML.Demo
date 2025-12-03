# Configuration Values Reference Guide

## 📋 Overview

This document provides a comprehensive reference for all configuration values used in the Fraud Detection ML service, explaining:
- **What** each configuration value does
- **Where** it's used in the codebase
- **Why** it's needed
- **Where to place it** (appsettings.json, settings.json, or secrets.json)

---

## 🔐 Configuration File Types

### **1. appsettings.json**
- **Location**: Project root
- **Purpose**: Base configuration for all environments
- **Contains**: Default values, non-sensitive settings
- **Committed to Git**: ✅ Yes
- **Used in**: Local development and as base for all environments

### **2. appsettings.{Environment}.json**
- **Location**: Project root (not created yet, but supported)
- **Purpose**: Environment-specific overrides for local development
- **Contains**: Development-specific settings (Swagger, verbose logging)
- **Committed to Git**: ✅ Yes
- **Used in**: Local development only

### **3. /settings.json** (Docker Config)
- **Location**: `/settings.json` in container
- **Purpose**: Environment-specific settings for Test/Production
- **Contains**: Serilog configuration, ML parameters, operational settings
- **Committed to Git**: ❌ No (mounted at runtime)
- **Used in**: Docker deployments (Test, Production)

### **4. /run/secrets/secrets.json** (Docker Secret)
- **Location**: `/run/secrets/secrets.json` in container
- **Purpose**: Sensitive credentials and secrets
- **Contains**: Database passwords, API keys, webhook URLs
- **Committed to Git**: ❌ No (mounted securely at runtime)
- **Used in**: Docker deployments (Test, Production)

---

## 📊 Configuration Values by Category

---

## 1️⃣ Database Configuration

### **ConnectionStrings:ClickHouse**

```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=localhost;Port=9000;Database=beepul_afs;Username=default;Password=your_password"
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | ClickHouse database connection string |
| **Where Used** | `Services/ClickHouseService.cs:15` - Constructor reads this value to connect to database |
| **Why Needed** | Required to access session data, events, and store fraud analysis results |
| **Format** | `Host=hostname;Port=port;Database=dbname;Username=user;Password=pass` |
| **Environment Variables** | Can override with: `ConnectionStrings__ClickHouse` |

**Placement Guidelines:**

| Environment | File | Reason |
|-------------|------|--------|
| **Development** | `appsettings.json` | Local ClickHouse (localhost, no password) |
| **Test** | `/run/secrets/secrets.json` | Contains test database password |
| **Production** | `/run/secrets/secrets.json` | Contains production database password |

**Example Values:**

```json
// Development (appsettings.json)
{
  "ConnectionStrings": {
    "ClickHouse": "Host=localhost;Port=9000;Database=beepul_afs"
  }
}

// Production (secrets.json)
{
  "ConnectionStrings": {
    "ClickHouse": "Host=clickhouse-prod.internal;Port=9000;Database=beepul_afs;Username=fraud_service;Password=StrongP@ssw0rd123"
  }
}
```

---

## 2️⃣ Logging Configuration (Serilog)

### **Serilog:MinimumLevel**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Sets the minimum log level for different namespaces |
| **Where Used** | `Program.cs:23-24` - Serilog reads this during initialization |
| **Why Needed** | Controls logging verbosity; reduces noise from Microsoft/System logs |

**Placement Guidelines:**

| Environment | File | Reason |
|-------------|------|--------|
| **Development** | `appsettings.json` | Can use "Debug" or "Information" |
| **Test** | `/settings.json` | Use "Information" |
| **Production** | `/settings.json` | Use "Warning" or "Information" |

---

### **Serilog:WriteTo** (Console and File)

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "Console",
              "Args": {
                "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
                "outputTemplate": "{Timestamp:yyyy.MM.dd HH:mm:ss} [{Level:u3}] [{SourceContext}]: {Message:lj}{NewLine}{Exception}{NewLine}"
              }
            }
          ]
        }
      },
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "File",
              "Args": {
                "path": "/root/logs/fraud-detection/.log",
                "outputTemplate": "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}]: {Message:lj}{NewLine}{Exception}{NewLine}",
                "rollingInterval": "Day",
                "retainedFileCountLimit": 60,
                "shared": true,
                "rollOnFileSizeLimit": true
              }
            }
          ]
        }
      }
    ]
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Configures async logging to console and file |
| **Where Used** | `Program.cs:23-24` - Serilog configuration builder |
| **Why Needed** | Non-blocking logging improves performance; file logs for debugging |

**Key Settings:**

| Setting | Purpose |
|---------|---------|
| `Async` | Wraps Console/File sinks for non-blocking writes |
| `outputTemplate` | Formats log entries (timestamp, level, message) |
| `rollingInterval: "Day"` | Creates new log file daily |
| `retainedFileCountLimit: 60` | Keeps 60 days of logs |
| `shared: true` | Multiple processes can write to same file |

**Placement Guidelines:**

| Environment | File | Reason |
|-------------|------|--------|
| **Development** | `appsettings.json` | Simple console logging |
| **Test** | `/settings.json` | Async logging to `/root/logs/` |
| **Production** | `/settings.json` | Async logging to `/root/logs/` |

---

### **Serilog:Properties**

```json
{
  "Serilog": {
    "Properties": {
      "ServiceName": "Beepul.fraud-detection"
    }
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Adds custom properties to all log entries |
| **Where Used** | Automatically enriched into every log entry |
| **Why Needed** | Helps identify logs from this service in centralized logging (ELK, Splunk) |

**Placement:** `/settings.json` (both Test and Production)

---

## 3️⃣ API Authentication

### **ApiKeys:ValidKeys**

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

| Property | Description |
|----------|-------------|
| **What** | List of valid API keys for authentication |
| **Where Used** | `Middleware/ApiKeyAuthMiddleware.cs:46` - Validates `X-API-Key` header |
| **Why Needed** | Protects API endpoints from unauthorized access |

**How It Works:**
1. Client sends request with header: `X-API-Key: prod-key-abc123`
2. Middleware reads `ApiKeys:ValidKeys` array from configuration
3. If provided key is in the array → request allowed
4. If not in array → 403 Forbidden

**Placement Guidelines:**

| Environment | File | Reason |
|-------------|------|--------|
| **Development** | `appsettings.json` | Simple keys like `"dev-key-12345"` |
| **Test** | `/run/secrets/secrets.json` | Test environment keys |
| **Production** | `/run/secrets/secrets.json` | Strong, rotated production keys |

**Security Best Practices:**

```bash
# Generate secure API keys
openssl rand -hex 32
# Example output: a7f8e3b2c1d4567890abcdef1234567890abcdef1234567890abcdef12345678

# Use different keys per partner/service
{
  "ApiKeys": {
    "ValidKeys": [
      "partner_abc_f8e3b2c1d4567890",  # Partner ABC
      "partner_xyz_1234567890abcdef",  # Partner XYZ
      "internal_api_abcdef12345678"    # Internal services
    ]
  }
}
```

---

## 4️⃣ Machine Learning Configuration

### **ML:TrainingIntervalHours**

```json
{
  "ML": {
    "TrainingIntervalHours": 24
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Hours between ML model training cycles |
| **Where Used** | `BackgroundJobs/ModelTrainingJob.cs:21` - Sets training interval |
| **Why Needed** | Controls how frequently models are retrained with new data |
| **Default** | 6 hours |

**Recommended Values:**

| Environment | Value | Reason |
|-------------|-------|--------|
| **Development** | `1` hour | Test training pipeline frequently |
| **Test** | `24` hours | Once daily, sufficient for testing |
| **Production** | `24` hours | Daily training with yesterday's data |

**Placement:** `/settings.json` (Test and Production)

---

### **ML:MinSessionsForTraining**

```json
{
  "ML": {
    "MinSessionsForTraining": 1000
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Minimum sessions required before training models |
| **Where Used** | `BackgroundJobs/ModelTrainingJob.cs:22` - Validates data availability |
| **Why Needed** | Prevents training with insufficient data (poor model quality) |
| **Default** | 1000 sessions |

**Recommended Values:**

| Environment | Value | Reason |
|-------------|-------|--------|
| **Development** | `100` | Lower threshold for testing |
| **Test** | `1000` | Standard minimum |
| **Production** | `5000` | Higher quality threshold |

**Placement:** `/settings.json` (Test and Production)

---

### **ML:ScoringIntervalSeconds**

```json
{
  "ML": {
    "ScoringIntervalSeconds": 10
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Seconds between real-time scoring job executions |
| **Where Used** | `BackgroundJobs/RealTimeScoringJob.cs:21` - Sets scoring frequency |
| **Why Needed** | Controls how frequently new sessions are analyzed |
| **Default** | 30 seconds |

**Impact on Performance:**

| Value | Sessions/Hour | Load | Use Case |
|-------|--------------|------|----------|
| `5` seconds | ~720 batches | High | High-traffic production |
| `10` seconds | ~360 batches | Medium | Standard production |
| `30` seconds | ~120 batches | Low | Low-traffic or testing |

**Placement:** `/settings.json` (Test and Production)

---

### **ML:ScoringBatchSize**

```json
{
  "ML": {
    "ScoringBatchSize": 100
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Number of sessions to analyze per scoring batch |
| **Where Used** | `BackgroundJobs/RealTimeScoringJob.cs:22` - Limits batch processing |
| **Why Needed** | Controls memory usage and prevents overload |
| **Default** | 100 sessions |

**Tuning Guide:**

| Batch Size | Memory Usage | Processing Time | Recommendation |
|-----------|--------------|----------------|----------------|
| `50` | Low (~200MB) | ~2-3 seconds | Low-memory environments |
| `100` | Medium (~400MB) | ~5-7 seconds | **Recommended** |
| `500` | High (~2GB) | ~30-40 seconds | High-performance servers |

**Placement:** `/settings.json` (Test and Production)

Can override with environment variable: `ML__ScoringBatchSize=500`

---

### **ML:AnalysisIntervalHours**

```json
{
  "ML": {
    "AnalysisIntervalHours": 24
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Hours between daily analysis report generation |
| **Where Used** | `BackgroundJobs/DailyAnalysisJob.cs:18` - Sets report frequency |
| **Why Needed** | Generates comprehensive fraud pattern reports |
| **Default** | 24 hours (daily) |

**Placement:** `/settings.json` (Test and Production)

---

## 5️⃣ Alert Configuration

### **Alerts:EnableDatabasePersistence**

```json
{
  "Alerts": {
    "EnableDatabasePersistence": true
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Enable/disable storing alert history in database |
| **Where Used** | `Services/HybridAlertService.cs:25` - Controls database writes |
| **Why Needed** | Allows disabling persistence for testing or troubleshooting |
| **Default** | `true` |

**Placement:** `appsettings.json` (all environments)

---

### **Alerts:DefaultThrottleWindowMinutes**

```json
{
  "Alerts": {
    "DefaultThrottleWindowMinutes": 60
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Default time window (minutes) for alert throttling |
| **Where Used** | `Services/HybridAlertService.cs:26` - Fallback throttle window |
| **Why Needed** | Prevents alert spam; same alert won't send twice within window |
| **Default** | 60 minutes |

**How Throttling Works:**

```
User A triggers "MultiAccounting" alert at 10:00 AM
→ Alert sent ✅

User A triggers "MultiAccounting" alert again at 10:30 AM
→ Alert NOT sent ❌ (within 60-minute window)

User A triggers "MultiAccounting" alert again at 11:15 AM
→ Alert sent ✅ (outside 60-minute window)
```

**Placement:** `appsettings.json` (all environments)

---

### **Alerts:FraudTypeSettings**

```json
{
  "Alerts": {
    "FraudTypeSettings": {
      "MultiAccounting": {
        "Enabled": true,
        "ThrottleWindowMinutes": 60,
        "Priority": "HIGH"
      },
      "AccountTakeover": {
        "Enabled": true,
        "ThrottleWindowMinutes": 30,
        "Priority": "CRITICAL"
      }
    }
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Per-fraud-type alert configuration |
| **Where Used** | `Services/HybridAlertService.cs:110-135` - Checks settings per fraud type |
| **Why Needed** | Allows fine-grained control over alert behavior |

**Supported Fraud Types:**

| Fraud Type | Description | Typical Throttle | Priority |
|-----------|-------------|------------------|----------|
| `MultiAccounting` | Multiple users on one device | 60 min | HIGH |
| `MultiDevicing` | One user on multiple devices | 60 min | HIGH |
| `AccountTakeover` | Account hijacking detected | 30 min | CRITICAL |
| `ImpossibleTravel` | Logins from distant locations | 30 min | CRITICAL |
| `OtpBruteforce` | OTP code guessing | 45 min | MEDIUM |
| `DeviceSpoofing` | Fake device fingerprints | 120 min | MEDIUM |
| `VpnUsage` | VPN/proxy usage detected | 120 min | LOW |
| `UnusualTiming` | Unusual access times | 180 min | LOW |
| `GeneralSuspicious` | Other suspicious patterns | 90 min | MEDIUM |

**Configuration Options:**

```json
{
  "Enabled": true,              // Enable/disable this fraud type alert
  "ThrottleWindowMinutes": 60,  // Minutes before same alert can resend
  "Priority": "HIGH"            // CRITICAL, HIGH, MEDIUM, LOW
}
```

**Placement:** `appsettings.json` (all environments)

Can be overridden in `/settings.json` for production tuning.

---

## 6️⃣ Notification Configuration

### **Notifications:TeamsWebhook**

```json
{
  "Notifications": {
    "TeamsWebhook": "https://outlook.office.com/webhook/..."
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Microsoft Teams incoming webhook URL |
| **Where Used** | `Services/NotificationService.cs:22` - Sends alerts to Teams channel |
| **Why Needed** | Real-time fraud alerts visible to team |

**Placement Guidelines:**

| Environment | File | Reason |
|-------------|------|--------|
| **Development** | `appsettings.json` (optional) | Test Teams channel webhook |
| **Test** | `/run/secrets/secrets.json` | Test Teams channel webhook |
| **Production** | `/run/secrets/secrets.json` | Production Teams channel webhook |

**Setup Instructions:**

1. Open Microsoft Teams
2. Go to channel → Connectors → Incoming Webhook
3. Name: "Fraud Detection Alerts"
4. Copy webhook URL
5. Add to secrets.json

---

### **Notifications:TelegramBotToken**

```json
{
  "Notifications": {
    "TelegramBotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Telegram bot authentication token |
| **Where Used** | `Services/NotificationService.cs:23` - Sends CRITICAL alerts to Telegram |
| **Why Needed** | Critical alerts sent to mobile for immediate action |

**Placement:** `/run/secrets/secrets.json` (Test and Production)

**How to Get Token:**

1. Message @BotFather on Telegram
2. Send: `/newbot`
3. Choose bot name: "Fraud Detection Bot"
4. Copy the token provided

---

### **Notifications:TelegramChatId**

```json
{
  "Notifications": {
    "TelegramChatId": "-1001234567890"
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | Telegram chat/channel ID where alerts are sent |
| **Where Used** | `Services/NotificationService.cs:24` - Target for Telegram messages |
| **Why Needed** | Specifies which chat receives alerts |

**Placement:** `/run/secrets/secrets.json` (Test and Production)

**How to Get Chat ID:**

1. Add bot to your Telegram channel
2. Send a message to the channel
3. Visit: `https://api.telegram.org/bot<YourBotToken>/getUpdates`
4. Find `"chat":{"id":-1001234567890}` in response
5. Use that ID

---

## 7️⃣ ASP.NET Core Configuration

### **Logging:LogLevel**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

| Property | Description |
|----------|-------------|
| **What** | ASP.NET Core logging levels (separate from Serilog) |
| **Where Used** | Built-in ASP.NET Core logger (before Serilog takes over) |
| **Why Needed** | Controls early startup logging |

**Placement:** `appsettings.json` and `deploy/prod/appsettings.Production.json`

---

## 📦 Complete Configuration Examples

### **Development (appsettings.json)**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "ClickHouse": "Host=localhost;Port=9000;Database=beepul_afs"
  },
  "ApiKeys": {
    "ValidKeys": [
      "dev-key-12345",
      "test-key-67890"
    ]
  },
  "Alerts": {
    "EnableDatabasePersistence": true,
    "DefaultThrottleWindowMinutes": 60,
    "FraudTypeSettings": {
      "MultiAccounting": {
        "Enabled": true,
        "ThrottleWindowMinutes": 60,
        "Priority": "HIGH"
      },
      "AccountTakeover": {
        "Enabled": true,
        "ThrottleWindowMinutes": 30,
        "Priority": "CRITICAL"
      }
    }
  }
}
```

---

### **Test Environment (/settings.json)**

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Async", "Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "Properties": {
      "ServiceName": "Beepul.fraud-detection"
    },
    "WriteTo": [
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "Console",
              "Args": {
                "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
                "outputTemplate": "{Timestamp:yyyy.MM.dd HH:mm:ss} [{Level:u3}] [{SourceContext}]: {Message:lj}{NewLine}{Exception}{NewLine}"
              }
            }
          ]
        }
      },
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "File",
              "Args": {
                "path": "/root/logs/fraud-detection/.log",
                "outputTemplate": "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}]: {Message:lj}{NewLine}{Exception}{NewLine}",
                "rollingInterval": "Day",
                "retainedFileCountLimit": 60,
                "shared": true,
                "rollOnFileSizeLimit": true
              }
            }
          ]
        }
      }
    ]
  },
  "ML": {
    "TrainingIntervalHours": 24,
    "MinSessionsForTraining": 1000,
    "ScoringIntervalSeconds": 10,
    "ScoringBatchSize": 100,
    "AnalysisIntervalHours": 24
  }
}
```

---

### **Test Environment (/run/secrets/secrets.json)**

```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=clickhouse-test.internal;Port=9000;Database=beepul_afs_test;Username=fraud_service;Password=Test_P@ssw0rd_123"
  },
  "ApiKeys": {
    "ValidKeys": [
      "test-partner-abc-a7f8e3b2c1d45678",
      "test-internal-api-1234567890abcdef"
    ]
  },
  "Notifications": {
    "TeamsWebhook": "https://outlook.office.com/webhook/test-channel-id/IncomingWebhook/...",
    "TelegramBotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz_TEST",
    "TelegramChatId": "-1001234567890"
  }
}
```

---

### **Production Environment (/settings.json)**

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Async", "Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "Properties": {
      "ServiceName": "Beepul.fraud-detection"
    },
    "WriteTo": [
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "Console",
              "Args": {
                "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
                "outputTemplate": "{Timestamp:yyyy.MM.dd HH:mm:ss} [{Level:u3}] [{SourceContext}]: {Message:lj}{NewLine}{Exception}{NewLine}"
              }
            }
          ]
        }
      },
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "File",
              "Args": {
                "path": "/root/logs/fraud-detection/.log",
                "outputTemplate": "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}]: {Message:lj}{NewLine}{Exception}{NewLine}",
                "rollingInterval": "Day",
                "retainedFileCountLimit": 60,
                "shared": true,
                "rollOnFileSizeLimit": true
              }
            }
          ]
        }
      }
    ]
  },
  "ML": {
    "TrainingIntervalHours": 24,
    "MinSessionsForTraining": 5000,
    "ScoringIntervalSeconds": 10,
    "ScoringBatchSize": 100,
    "AnalysisIntervalHours": 24
  },
  "Alerts": {
    "EnableDatabasePersistence": true,
    "DefaultThrottleWindowMinutes": 60,
    "FraudTypeSettings": {
      "AccountTakeover": {
        "ThrottleWindowMinutes": 15
      }
    }
  }
}
```

---

### **Production Environment (/run/secrets/secrets.json)**

```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=clickhouse-prod.internal;Port=9000;Database=beepul_afs;Username=fraud_service;Password=Pr0d_S3cur3_P@ssw0rd_!@#"
  },
  "ApiKeys": {
    "ValidKeys": [
      "prod-partner-abc-f8e3b2c1d4567890abcdef1234567890",
      "prod-partner-xyz-1234567890abcdef1234567890abcdef",
      "prod-internal-api-abcdef1234567890abcdef12345678"
    ]
  },
  "Notifications": {
    "TeamsWebhook": "https://outlook.office.com/webhook/prod-channel-id/IncomingWebhook/...",
    "TelegramBotToken": "9876543210:XYZabcDEFghiJKLmnoPQRstuvWXYZ_PROD",
    "TelegramChatId": "-1009876543210"
  }
}
```

---

## 🔄 Configuration Override Priority

Configuration values are merged with **later sources overriding earlier sources**:

```
1. appsettings.json                    (Lowest priority - base defaults)
2. appsettings.{Environment}.json      (Environment overrides)
3. /settings.json                      (Docker Config)
4. /run/secrets/secrets.json           (Docker Secrets)
5. Environment Variables               (Highest priority - runtime overrides)
```

### **Example: Override with Environment Variables**

```bash
# Override ClickHouse connection string
export ConnectionStrings__ClickHouse="Host=different-host;Port=9000"

# Override ML training interval
export ML__TrainingIntervalHours=12

# Override scoring batch size
export ML__ScoringBatchSize=500

# Override API keys (JSON array)
export ApiKeys__ValidKeys__0="override-key-1"
export ApiKeys__ValidKeys__1="override-key-2"
```

**Note:** Use double underscore `__` to navigate nested JSON structure.

---

## 🛡️ Security Best Practices

### **✅ DO:**

1. **Store passwords in secrets.json**
   ```json
   {
     "ConnectionStrings": {
       "ClickHouse": "...Password=StrongP@ssw0rd..."
     }
   }
   ```

2. **Rotate API keys regularly**
   ```bash
   # Generate new keys monthly
   openssl rand -hex 32
   ```

3. **Use strong passwords**
   - Minimum 16 characters
   - Mix of uppercase, lowercase, numbers, symbols
   - Avoid dictionary words

4. **Limit API key access**
   - One key per partner/service
   - Rotate keys when partner access is revoked

5. **Use Docker Secrets for production**
   ```yaml
   services:
     fraud-detection-api:
       secrets:
         - source: secrets_source
           target: /run/secrets/secrets.json
   ```

### **❌ DON'T:**

1. **Don't commit secrets to Git**
   ```bash
   # Add to .gitignore
   **/secrets.json
   **/secrets*.json
   appsettings.Production.json  # If it contains secrets
   ```

2. **Don't use environment variables for secrets in production**
   - Visible in `docker inspect`
   - Can leak in logs
   - Use Docker Secrets instead

3. **Don't hardcode passwords**
   ```json
   // ❌ BAD
   {
     "ConnectionStrings": {
       "ClickHouse": "Host=prod;Password=admin123"
     }
   }
   ```

4. **Don't use weak API keys in production**
   ```json
   // ❌ BAD
   {
     "ApiKeys": {
       "ValidKeys": ["key1", "test", "admin"]
     }
   }
   ```

---

## 🔍 Troubleshooting Configuration Issues

### **Problem: Configuration value not loading**

**Check order:**
```bash
# Inside container
docker exec -it <container> cat /settings.json
docker exec -it <container> cat /run/secrets/secrets.json
docker exec -it <container> env | grep -i <config_key>
```

**Verify format:**
```bash
# Test JSON validity
cat /settings.json | jq .
cat /run/secrets/secrets.json | jq .
```

---

### **Problem: API returns "Models not loaded"**

**Check:**
- `ML:MinSessionsForTraining` - Is threshold too high?
- Background service logs - Is training job running?
- Volume mount - Are models persisted?

```bash
docker service logs fraud-detection_fraud-detection-background
docker exec -it <container> ls -la /app/Models/
```

---

### **Problem: Alerts not sending**

**Check:**
1. `Alerts:FraudTypeSettings:Enabled` - Is fraud type enabled?
2. `Notifications:TeamsWebhook` - Is webhook URL valid?
3. Service logs - Look for notification errors

```bash
# Test Teams webhook
curl -X POST <webhook-url> \
  -H "Content-Type: application/json" \
  -d '{"text": "Test message"}'
```

---

## 📚 Related Documentation

- [CONFIGURATION_EXPLAINED.md](./CONFIGURATION_EXPLAINED.md) - Detailed explanation of configuration system
- [DEPLOYMENT_GUIDE.md](./deploy/DEPLOYMENT_GUIDE.md) - Deployment procedures
- [API_DOCUMENTATION.md](./API_DOCUMENTATION.md) - API endpoint reference

---

**Last Updated:** December 2024
**Version:** 2.0.0
