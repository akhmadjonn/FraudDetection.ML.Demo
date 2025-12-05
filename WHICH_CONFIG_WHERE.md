# 📋 Which Configuration Goes Where - Simple Guide

## 🔐 IN SECRETS (NEVER commit to git)

### ✅ REQUIRED
```bash
# ClickHouse Database Connection
dotnet user-secrets set "ConnectionStrings:ClickHouse" "Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=YOUR_PASSWORD"
```
**Used by:**
- `Services/ClickHouseService.cs:16`
- `Services/AlertHistoryService.cs:21`

---

### ⚠️ OPTIONAL (but highly recommended for alerts)

```bash
# Microsoft Teams Webhook
dotnet user-secrets set "Notifications:TeamsWebhook" "https://outlook.office.com/webhook/YOUR_WEBHOOK_URL"

# Telegram Bot
dotnet user-secrets set "Notifications:TelegramBotToken" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Notifications:TelegramChatId" "YOUR_CHAT_ID"
```
**Used by:**
- `Services/NotificationService.cs:22-24`

---

## 📄 IN appsettings.json (Safe to commit)

### ML Settings
```json
{
  "ML": {
    "TrainingIntervalHours": 6,
    "MinSessionsForTraining": 1000,
    "ScoringIntervalSeconds": 10,
    "ScoringBatchSize": 100,
    "DailyReportHour": 8
  }
}
```
**Used by:**
- `BackgroundJobs/ModelTrainingJob.cs:21-22`
- `BackgroundJobs/RealTimeScoringJob.cs:22-23`
- `BackgroundJobs/DailyAnalysisJob.cs:19`

---

### Alert Settings
```json
{
  "Alerts": {
    "EnableDatabasePersistence": true,
    "DefaultThrottleWindowMinutes": 60,
    "ThrottleWindowHours": 24,
    "FraudTypeSettings": {
      "MultiAccounting": {
        "Enabled": true,
        "ThrottleWindowMinutes": 60,
        "Priority": "HIGH"
      },
      "MultiDevicing": {
        "Enabled": true,
        "ThrottleWindowMinutes": 60,
        "Priority": "HIGH"
      },
      "AccountTakeover": {
        "Enabled": true,
        "ThrottleWindowMinutes": 30,
        "Priority": "CRITICAL"
      },
      "ImpossibleTravel": {
        "Enabled": true,
        "ThrottleWindowMinutes": 30,
        "Priority": "CRITICAL"
      },
      "OtpBruteforce": {
        "Enabled": true,
        "ThrottleWindowMinutes": 45,
        "Priority": "MEDIUM"
      },
      "DeviceSpoofing": {
        "Enabled": true,
        "ThrottleWindowMinutes": 120,
        "Priority": "MEDIUM"
      },
      "VpnUsage": {
        "Enabled": true,
        "ThrottleWindowMinutes": 120,
        "Priority": "LOW"
      },
      "UnusualTiming": {
        "Enabled": true,
        "ThrottleWindowMinutes": 180,
        "Priority": "LOW"
      },
      "GeneralSuspicious": {
        "Enabled": true,
        "ThrottleWindowMinutes": 90,
        "Priority": "MEDIUM"
      }
    }
  }
}
```
**Used by:**
- `Services/HybridAlertService.cs:30-31`
- `Services/HybridAlertService.cs:299-310`
- `Services/AlertHistoryService.cs:26`

---

### Serilog (Logging)
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
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```
**Used by:**
- `Program.cs:8-12`
- Entire application (logging framework)

---

## 🗺️ Configuration Map by Service

| Service/File | Configurations Used | Storage |
|--------------|---------------------|---------|
| **ClickHouseService.cs** | ConnectionStrings:ClickHouse | 🔐 Secrets |
| **AlertHistoryService.cs** | ConnectionStrings:ClickHouse<br>Alerts:ThrottleWindowHours | 🔐 Secrets<br>📄 appsettings.json |
| **NotificationService.cs** | Notifications:TeamsWebhook<br>Notifications:TelegramBotToken<br>Notifications:TelegramChatId | 🔐 Secrets<br>🔐 Secrets<br>🔐 Secrets |
| **HybridAlertService.cs** | Alerts:EnableDatabasePersistence<br>Alerts:DefaultThrottleWindowMinutes<br>Alerts:FraudTypeSettings:* | 📄 appsettings.json<br>📄 appsettings.json<br>📄 appsettings.json |
| **ModelTrainingJob.cs** | ML:TrainingIntervalHours<br>ML:MinSessionsForTraining | 📄 appsettings.json<br>📄 appsettings.json |
| **RealTimeScoringJob.cs** | ML:ScoringIntervalSeconds<br>ML:ScoringBatchSize | 📄 appsettings.json<br>📄 appsettings.json |
| **DailyAnalysisJob.cs** | ML:DailyReportHour | 📄 appsettings.json |
| **Program.cs** | Serilog:*<br>Logging:LogLevel:* | 📄 appsettings.json<br>📄 appsettings.json |

---

## 📊 Total: 4 Secrets + 23 Non-Sensitive Settings

### 🔐 Secrets (4):
1. ConnectionStrings:ClickHouse
2. Notifications:TeamsWebhook
3. Notifications:TelegramBotToken
4. Notifications:TelegramChatId

### 📄 Non-Sensitive (23):
- ML settings (5)
- Alert settings (3 + 9 fraud types)
- Serilog configuration (complex object)
- Logging levels (complex object)

---

## 🎯 TLDR - Absolute Minimum to Run

```bash
# Only this is REQUIRED:
dotnet user-secrets set "ConnectionStrings:ClickHouse" "Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=yourpass"
```

Everything else has defaults! But you should also add at least one notification method (Teams or Telegram).
