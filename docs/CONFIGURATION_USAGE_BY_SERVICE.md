# 🔍 Actually Used Configurations - Service Mapping

## Quick Reference Table

| Configuration Key | Type | Used In Service/File | Where to Store | Example Value | Required? |
|-------------------|------|---------------------|----------------|---------------|-----------|
| **ConnectionStrings:ClickHouse** | String | ClickHouseService.cs:16<br>AlertHistoryService.cs:21 | 🔐 **SECRETS** | `Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=secret` | ✅ **YES** |
| **Notifications:TeamsWebhook** | String | NotificationService.cs:22 | 🔐 **SECRETS** | `https://outlook.office.com/webhook/abc123...` | ⚠️ Optional |
| **Notifications:TelegramBotToken** | String | NotificationService.cs:23 | 🔐 **SECRETS** | `1234567890:ABCdefGHI...` | ⚠️ Optional |
| **Notifications:TelegramChatId** | String | NotificationService.cs:24 | 🔐 **SECRETS** | `-1001234567890` | ⚠️ Optional |
| **ML:TrainingIntervalHours** | Integer | ModelTrainingJob.cs:21 | 📄 appsettings.json | `6` | No (has default) |
| **ML:MinSessionsForTraining** | Integer | ModelTrainingJob.cs:22 | 📄 appsettings.json | `1000` | No (has default) |
| **ML:ScoringIntervalSeconds** | Integer | RealTimeScoringJob.cs:22 | 📄 appsettings.json | `10` | No (has default) |
| **ML:ScoringBatchSize** | Integer | RealTimeScoringJob.cs:23 | 📄 appsettings.json | `100` | No (has default) |
| **ML:DailyReportHour** | Integer | DailyAnalysisJob.cs:19 | 📄 appsettings.json | `8` | No (has default) |
| **Alerts:EnableDatabasePersistence** | Boolean | HybridAlertService.cs:30 | 📄 appsettings.json | `true` | No (has default) |
| **Alerts:DefaultThrottleWindowMinutes** | Integer | HybridAlertService.cs:31 | 📄 appsettings.json | `60` | No (has default) |
| **Alerts:ThrottleWindowHours** | Integer | AlertHistoryService.cs:26 | 📄 appsettings.json | `24` | No (has default) |
| **Alerts:FraudTypeSettings:MultiAccounting** | Object | HybridAlertService.cs:299-310 | 📄 appsettings.json | See below | No (has default) |
| **Alerts:FraudTypeSettings:MultiDevicing** | Object | HybridAlertService.cs:299-310 | 📄 appsettings.json | See below | No (has default) |
| **Alerts:FraudTypeSettings:AccountTakeover** | Object | HybridAlertService.cs:299-310 | 📄 appsettings.json | See below | No (has default) |
| **Alerts:FraudTypeSettings:ImpossibleTravel** | Object | HybridAlertService.cs:299-310 | 📄 appsettings.json | See below | No (has default) |
| **Alerts:FraudTypeSettings:OtpBruteforce** | Object | HybridAlertService.cs:299-310 | 📄 appsettings.json | See below | No (has default) |
| **Alerts:FraudTypeSettings:DeviceSpoofing** | Object | HybridAlertService.cs:299-310 | 📄 appsettings.json | See below | No (has default) |
| **Alerts:FraudTypeSettings:VpnUsage** | Object | HybridAlertService.cs:299-310 | 📄 appsettings.json | See below | No (has default) |
| **Alerts:FraudTypeSettings:UnusualTiming** | Object | HybridAlertService.cs:299-310 | 📄 appsettings.json | See below | No (has default) |
| **Alerts:FraudTypeSettings:GeneralSuspicious** | Object | HybridAlertService.cs:299-310 | 📄 appsettings.json | See below | No (has default) |
| **Serilog:*** | Object | Program.cs:8-12 | 📄 appsettings.json | See below | No (has default) |
| **Logging:LogLevel:*** | Object | Entire application | 📄 appsettings.json | See below | No (has default) |

---

## 📁 File-by-File Configuration Usage

### 1️⃣ **Services/ClickHouseService.cs**

**Line 16:**
```csharp
_connectionString = config.GetConnectionString("ClickHouse")
    ?? throw new ArgumentNullException("ClickHouse connection string not found");
```

**Configuration:**
```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=yourpassword"
  }
}
```

**Storage:** 🔐 **User Secrets (dev) / Environment Variables (prod)**

---

### 2️⃣ **Services/AlertHistoryService.cs**

**Line 21:**
```csharp
_connectionString = config.GetConnectionString("ClickHouse")
    ?? throw new ArgumentNullException("ClickHouse connection string not found");
```

**Line 26:**
```csharp
var hours = config.GetValue<int>("Alerts:ThrottleWindowHours", 24);
```

**Configuration:**
```json
{
  "ConnectionStrings": {
    "ClickHouse": "..."  // Same as above
  },
  "Alerts": {
    "ThrottleWindowHours": 24
  }
}
```

**Storage:**
- ConnectionStrings → 🔐 **SECRETS**
- Alerts:ThrottleWindowHours → 📄 **appsettings.json**

---

### 3️⃣ **Services/NotificationService.cs**

**Lines 22-24:**
```csharp
_teamsWebhookUrl = config["Notifications:TeamsWebhook"];
_telegramBotToken = config["Notifications:TelegramBotToken"];
_telegramChatId = config["Notifications:TelegramChatId"];
```

**Configuration:**
```json
{
  "Notifications": {
    "TeamsWebhook": "https://outlook.office.com/webhook/your-webhook-url",
    "TelegramBotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz",
    "TelegramChatId": "-1001234567890"
  }
}
```

**Storage:** 🔐 **User Secrets (dev) / Environment Variables (prod)**

**Behavior:**
- If `TeamsWebhook` is set → Sends ALL alerts to Teams
- If `TelegramBotToken` + `TelegramChatId` are set → Sends CRITICAL alerts to Telegram

---

### 4️⃣ **Services/HybridAlertService.cs**

**Line 30:**
```csharp
_enableDatabasePersistence = config.GetValue<bool>("Alerts:EnableDatabasePersistence", true);
```

**Line 31:**
```csharp
var defaultMinutes = config.GetValue<int>("Alerts:DefaultThrottleWindowMinutes", 60);
```

**Lines 299-310:**
```csharp
var section = _config.GetSection("Alerts:FraudTypeSettings");
foreach (var fraudType in fraudTypes)
{
    var typeSection = section.GetSection(fraudType);
    var enabled = typeSection.GetValue<bool>("Enabled", true);
    var minutes = typeSection.GetValue<int>("ThrottleWindowMinutes", (int)_defaultThrottleWindow.TotalMinutes);
    var priority = typeSection.GetValue<string>("Priority", "MEDIUM");
}
```

**Configuration:**
```json
{
  "Alerts": {
    "EnableDatabasePersistence": true,
    "DefaultThrottleWindowMinutes": 60,
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

**Storage:** 📄 **appsettings.json**

---

### 5️⃣ **BackgroundJobs/ModelTrainingJob.cs**

**Lines 21-22:**
```csharp
_trainingInterval = TimeSpan.FromHours(config.GetValue<int>("ML:TrainingIntervalHours", 6));
_minSessionsForTraining = config.GetValue<int>("ML:MinSessionsForTraining", 1000);
```

**Configuration:**
```json
{
  "ML": {
    "TrainingIntervalHours": 6,
    "MinSessionsForTraining": 1000
  }
}
```

**Storage:** 📄 **appsettings.json**

---

### 6️⃣ **BackgroundJobs/RealTimeScoringJob.cs**

**Lines 22-23:**
```csharp
_checkInterval = TimeSpan.FromSeconds(config.GetValue<int>("ML:ScoringIntervalSeconds", 10));
_batchSize = config.GetValue<int>("ML:ScoringBatchSize", 100);
```

**Configuration:**
```json
{
  "ML": {
    "ScoringIntervalSeconds": 10,
    "ScoringBatchSize": 100
  }
}
```

**Storage:** 📄 **appsettings.json**

---

### 7️⃣ **BackgroundJobs/DailyAnalysisJob.cs**

**Line 19:**
```csharp
var reportHour = config.GetValue<int>("ML:DailyReportHour", 8);
```

**Configuration:**
```json
{
  "ML": {
    "DailyReportHour": 8
  }
}
```

**Storage:** 📄 **appsettings.json**

---

### 8️⃣ **Program.cs**

**Lines 8-12:**
```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Beepul.Afs.FraudDetection.ML.Host")
    .CreateLogger();
```

**Configuration:**
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

**Storage:** 📄 **appsettings.json**

---

## 🎯 Summary by Storage Location

### 🔐 **SECRETS (User Secrets / Environment Variables)**

**Required:**
1. ✅ `ConnectionStrings:ClickHouse`

**Optional but Recommended:**
2. ⚠️ `Notifications:TeamsWebhook`
3. ⚠️ `Notifications:TelegramBotToken`
4. ⚠️ `Notifications:TelegramChatId`

**How to Set:**

**Development:**
```bash
dotnet user-secrets set "ConnectionStrings:ClickHouse" "Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=yourpass"
dotnet user-secrets set "Notifications:TeamsWebhook" "https://outlook.office.com/webhook/..."
dotnet user-secrets set "Notifications:TelegramBotToken" "1234567890:ABC..."
dotnet user-secrets set "Notifications:TelegramChatId" "-1001234567890"
```

**Production (Environment Variables):**
```bash
export ConnectionStrings__ClickHouse="Host=...;Password=..."
export Notifications__TeamsWebhook="https://..."
export Notifications__TelegramBotToken="1234567890:..."
export Notifications__TelegramChatId="-1001234567890"
```

---

### 📄 **appsettings.json (Committed to Git)**

**ML Settings:**
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

**Alert Settings:**
```json
{
  "Alerts": {
    "EnableDatabasePersistence": true,
    "DefaultThrottleWindowMinutes": 60,
    "ThrottleWindowHours": 24,
    "FraudTypeSettings": {
      "MultiAccounting": { "Enabled": true, "ThrottleWindowMinutes": 60, "Priority": "HIGH" },
      "MultiDevicing": { "Enabled": true, "ThrottleWindowMinutes": 60, "Priority": "HIGH" },
      "AccountTakeover": { "Enabled": true, "ThrottleWindowMinutes": 30, "Priority": "CRITICAL" },
      "ImpossibleTravel": { "Enabled": true, "ThrottleWindowMinutes": 30, "Priority": "CRITICAL" },
      "OtpBruteforce": { "Enabled": true, "ThrottleWindowMinutes": 45, "Priority": "MEDIUM" },
      "DeviceSpoofing": { "Enabled": true, "ThrottleWindowMinutes": 120, "Priority": "MEDIUM" },
      "VpnUsage": { "Enabled": true, "ThrottleWindowMinutes": 120, "Priority": "LOW" },
      "UnusualTiming": { "Enabled": true, "ThrottleWindowMinutes": 180, "Priority": "LOW" },
      "GeneralSuspicious": { "Enabled": true, "ThrottleWindowMinutes": 90, "Priority": "MEDIUM" }
    }
  }
}
```

**Logging (Serilog):**
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

---

## ✅ Verification Checklist

After configuration, check that these services can access their configs:

- [ ] **ClickHouseService** → Can connect to database
- [ ] **AlertHistoryService** → Can connect to database, throttle window set
- [ ] **NotificationService** → Webhooks/tokens configured
- [ ] **HybridAlertService** → Alert settings loaded, fraud types configured
- [ ] **ModelTrainingJob** → Training interval and minimum sessions set
- [ ] **RealTimeScoringJob** → Scoring interval and batch size set
- [ ] **DailyAnalysisJob** → Report hour configured
- [ ] **Program.cs** → Serilog initialized correctly

---

## 🔍 How to Find Configuration Usage

Search in code:
```bash
# Find all GetConnectionString calls
grep -r "GetConnectionString" Services/

# Find all config.GetValue calls
grep -r "config.GetValue" BackgroundJobs/ Services/

# Find all config["..."] calls
grep -r 'config\["' Services/

# Find all GetSection calls
grep -r "GetSection" Services/
```
