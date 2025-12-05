# 📖 Complete Configuration Reference

## Overview

This document explains **EVERY** configuration option available in the Fraud Detection ML system, including:
- ✅ **Currently Used** - Actively used in the codebase
- ⚠️ **Partially Used** - Some parts implemented
- 🔮 **Future/Optional** - Not yet implemented but prepared for future use

---

## 🔗 ConnectionStrings

### `ConnectionStrings:ClickHouse` ✅ **REQUIRED - Currently Used**

**Type:** String
**Example:** `Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=secret`
**Used In:**
- `Services/ClickHouseService.cs:16`
- `Services/AlertHistoryService.cs:21`

**Format Components:**
- `Host` - ClickHouse server hostname or IP
- `Port` - ClickHouse HTTP port (default: 8123)
- `Database` - Database name
- `Username` - Database user
- `Password` - Database password (SENSITIVE)

**Security:** ⚠️ **NEVER commit this to git!** Use User Secrets (dev) or Environment Variables (prod).

---

## 📝 Logging

### `Logging:LogLevel:*` ✅ **Currently Used**

**Type:** Object with log level values
**Values:** `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`

**Used By:** Microsoft.Extensions.Logging framework

**Configuration:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "System": "Warning"
    }
  }
}
```

---

## 📊 Serilog Configuration

### `Serilog` ✅ **Currently Used**

**Used In:** `Program.cs:8-12`

**Complete Configuration:**

#### `Serilog:Using` ✅
**Type:** Array of strings
**Purpose:** Sinks to load
**Example:** `["Serilog.Sinks.Console", "Serilog.Sinks.File"]`

#### `Serilog:MinimumLevel` ✅
**Type:** Object
**Purpose:** Minimum log levels per namespace
**Default:** `Information`
**Overrides:** Per-namespace log levels

#### `Serilog:WriteTo` ✅
**Type:** Array of sink configurations
**Available Sinks:**
1. **Console** - Writes to stdout
2. **File** - Writes to rolling file

**Console Configuration:**
```json
{
  "Name": "Console",
  "Args": {
    "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
  }
}
```

**File Configuration:**
```json
{
  "Name": "File",
  "Args": {
    "path": "/var/log/frauddetection/log-.txt",
    "rollingInterval": "Day",
    "retainedFileCountLimit": 30,
    "fileSizeLimitBytes": 104857600,
    "rollOnFileSizeLimit": true,
    "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
  }
}
```

#### `Serilog:Enrich` ✅
**Type:** Array of strings
**Purpose:** Add contextual information to logs
**Options:**
- `FromLogContext` - Include log scope properties
- `WithMachineName` - Add machine name
- `WithThreadId` - Add thread ID
- `WithEnvironmentName` - Add environment name

---

## 🤖 ML Settings

### `ML:TrainingIntervalHours` ✅ **Currently Used**

**Type:** Integer
**Default:** `6`
**Used In:** `BackgroundJobs/ModelTrainingJob.cs:21`
**Purpose:** Hours between automatic model retraining
**Recommended:**
- Development: `1-2` hours
- Production: `6-12` hours

### `ML:MinSessionsForTraining` ✅ **Currently Used**

**Type:** Integer
**Default:** `1000`
**Used In:** `BackgroundJobs/ModelTrainingJob.cs:22`
**Purpose:** Minimum sessions required before training
**Recommended:**
- Development: `100-500`
- Production: `1000-5000`

### `ML:ScoringIntervalSeconds` ✅ **Currently Used**

**Type:** Integer
**Default:** `10`
**Used In:** `BackgroundJobs/RealTimeScoringJob.cs:22`
**Purpose:** Seconds between scoring batches
**Recommended:** `5-30` seconds

### `ML:ScoringBatchSize` ✅ **Currently Used**

**Type:** Integer
**Default:** `100`
**Used In:** `BackgroundJobs/RealTimeScoringJob.cs:23`
**Purpose:** Number of sessions to score per batch
**Recommended:** `50-500`

### `ML:DailyReportHour` ✅ **Currently Used**

**Type:** Integer (0-23)
**Default:** `8`
**Used In:** `BackgroundJobs/DailyAnalysisJob.cs:19`
**Purpose:** Hour of day to send daily reports (UTC)

### 🔮 Future ML Settings (Not Yet Implemented)

- `ML:MaxSessionsForTraining` - Cap on training data
- `ML:MaxConcurrentScoringTasks` - Parallel scoring limit
- `ML:IsolationForest:*` - Isolation Forest parameters
- `ML:Clustering:*` - Clustering parameters
- `ML:DeviceHistoryDays` - History window
- `ML:UserHistoryDays` - User history window

---

## 🚨 Alerts Configuration

### `Alerts:EnableDatabasePersistence` ✅ **Currently Used**

**Type:** Boolean
**Default:** `true`
**Used In:** `Services/HybridAlertService.cs:30`
**Purpose:** Save alerts to ClickHouse database

### `Alerts:DefaultThrottleWindowMinutes` ✅ **Currently Used**

**Type:** Integer
**Default:** `60`
**Used In:** `Services/HybridAlertService.cs:31`
**Purpose:** Default minutes before re-alerting on same fraud

### `Alerts:ThrottleWindowHours` ✅ **Currently Used**

**Type:** Integer
**Default:** `24`
**Used In:** `Services/AlertHistoryService.cs:26`
**Purpose:** How long to keep alerts in cache (hours)

### `Alerts:FraudTypeSettings:*` ✅ **Currently Used**

**Used In:** `Services/HybridAlertService.cs:299-310`

**Structure for each fraud type:**
```json
{
  "Enabled": true,
  "ThrottleWindowMinutes": 60,
  "Priority": "HIGH"
}
```

**Fields:**
- `Enabled` (Boolean) - Enable/disable detection
- `ThrottleWindowMinutes` (Integer) - Minutes before re-alerting
- `Priority` (String) - `CRITICAL`, `HIGH`, `MEDIUM`, `LOW`

**Available Fraud Types:**

#### ✅ Currently Implemented:
1. **MultiAccounting** - Multiple users on one device
2. **MultiDevicing** - One user on multiple devices
3. **AccountTakeover** - Suspicious account access
4. **ImpossibleTravel** - Geographic impossibilities
5. **OtpBruteforce** - Pattern-based detection
6. **DeviceSpoofing** - Pattern-based detection
7. **VpnUsage** - Pattern-based detection
8. **UnusualTiming** - Pattern-based detection
9. **GeneralSuspicious** - Pattern-based detection

### 🔮 Future Alert Settings (Not Yet Implemented)

- `Alerts:MaxAlertsPerHour` - Rate limiting
- `Alerts:MaxAlertsPerDay` - Daily cap
- `Alerts:FraudTypeSettings:*.Thresholds` - Configurable thresholds

---

## 📧 Notifications

### `Notifications:TeamsWebhook` ✅ **Currently Used**

**Type:** String (URL)
**Used In:** `Services/NotificationService.cs:22`
**Purpose:** Microsoft Teams incoming webhook URL
**Security:** ⚠️ **SENSITIVE** - Use User Secrets or Env Vars
**Format:** `https://outlook.office.com/webhook/...`
**Behavior:** Receives ALL alert levels (CRITICAL, HIGH, MEDIUM, LOW) + daily reports

**Setup Guide:** See `docs/TEAMS_SETUP.md`

### `Notifications:TelegramBotToken` ✅ **Currently Used**

**Type:** String
**Used In:** `Services/NotificationService.cs:23`
**Purpose:** Telegram bot authentication token
**Security:** ⚠️ **SENSITIVE** - Use User Secrets or Env Vars
**Format:** `1234567890:ABCdefGHIjklMNOpqrsTUVwxyz`
**Behavior:** Receives CRITICAL alerts only + daily reports

**Setup Guide:** See `docs/TELEGRAM_SETUP.md`

### `Notifications:TelegramChatId` ✅ **Currently Used**

**Type:** String
**Used In:** `Services/NotificationService.cs:24`
**Purpose:** Telegram chat ID to send messages to
**Security:** ⚠️ **SENSITIVE** - Use User Secrets or Env Vars
**Format:** `-1001234567890` (group) or `987654321` (personal)

**Setup Guide:** See `docs/TELEGRAM_SETUP.md`

### 🔮 Future Notification Settings (Not Yet Implemented)

- `Notifications:TeamsWebhookCritical` - Separate webhook for critical alerts
- `Notifications:TeamsWebhookDaily` - Separate webhook for reports
- `Notifications:EnableTeamsNotifications` - Toggle Teams on/off
- `Notifications:TeamsRetryAttempts` - Retry logic
- `Notifications:TelegramChatIdCritical` - Separate chat for critical
- `Notifications:EnableTelegramNotifications` - Toggle Telegram on/off
- `Notifications:SlackWebhook` - Slack integration
- `Notifications:EnableEmailNotifications` - Email support
- `Notifications:SmtpHost/Port/Username/Password` - Email SMTP
- `Notifications:CustomWebhookUrl` - Custom webhook

---

## 🔐 Security Settings

### 🔮 All Security Settings (Not Yet Implemented)

These are prepared for future use if you expose HTTP API endpoints:

- `Security:EnableApiAuthentication`
- `Security:ApiKey`
- `Security:JwtSecret`
- `Security:JwtIssuer/Audience`
- `Security:DataEncryptionKey`

---

## ⚡ Performance Settings

### 🔮 All Performance Settings (Not Yet Implemented)

Prepared for future optimization:

- `Performance:MaxDegreeOfParallelism`
- `Performance:MaxConcurrentDatabaseConnections`
- `Performance:DatabaseCommandTimeoutSeconds`
- `Performance:MaxCachedSessionsCount`
- `Performance:MaxRequestsPerSecond`

---

## 🏥 Health Checks

### 🔮 All Health Check Settings (Not Yet Implemented)

Prepared for monitoring integration:

- `HealthChecks:EnableHealthChecks`
- `HealthChecks:HealthCheckIntervalSeconds`
- `HealthChecks:EnableMetrics`
- `HealthChecks:PrometheusPort`

---

## 🎚️ Feature Flags

### 🔮 All Feature Flags (Not Yet Implemented)

Prepared for feature toggling:

- `Features:EnableRealTimeScoring` (currently always on)
- `Features:EnableModelTraining` (currently always on)
- `Features:EnableDailyAnalysis` (currently always on)
- `Features:EnableExperimentalFeatures`
- `Features:EnableAdvancedClustering`
- `Features:EnableDeepLearningModels`

---

## 🗑️ Data Retention

### 🔮 All Data Retention Settings (Not Yet Implemented)

Prepared for automatic cleanup:

- `DataRetention:AlertRetentionDays`
- `DataRetention:AnalysisResultsRetentionDays`
- `DataRetention:ModelHistoryRetentionDays`
- `DataRetention:LogRetentionDays`

---

## 🌐 External Services

### 🔮 All External Service Settings (Not Yet Implemented)

Prepared for third-party integrations:

- `ExternalServices:IpGeolocationApiUrl/ApiKey`
- `ExternalServices:VpnDetectionApiUrl/ApiKey`
- `ExternalServices:DeviceFingerprintApiUrl/ApiKey`

---

## 🗄️ ClickHouse Specific

### 🔮 All ClickHouse Settings (Not Yet Implemented)

Advanced ClickHouse connection settings:

- `ClickHouse:MaxPoolSize`
- `ClickHouse:ConnectionTimeoutSeconds`
- `ClickHouse:UseCompression`
- `ClickHouse:BatchInsertSize`

**Note:** Currently using basic connection string from `ConnectionStrings:ClickHouse`

---

## 🌍 Globalization

### 🔮 All Globalization Settings (Not Yet Implemented)

- `Globalization:DefaultCulture`
- `Globalization:DefaultTimeZone`
- `Globalization:SupportedCultures`

---

## 🐛 Development Settings

### 🔮 All Development Settings (Not Yet Implemented)

Debug settings for development:

- `Development:EnableDetailedErrors`
- `Development:EnableSensitiveDataLogging`
- `Development:MockExternalServices`

---

## 🌐 ASP.NET Core Settings

### `AllowedHosts` ⚠️ **Partially Used**

**Type:** String
**Default:** `*`
**Purpose:** Host filtering (if exposing HTTP endpoints)

### `ASPNETCORE_ENVIRONMENT` ✅ **Used**

**Type:** String
**Values:** `Development`, `Staging`, `Production`
**Purpose:** Determines which appsettings file to load

---

## 🔧 Kestrel Web Server

### 🔮 All Kestrel Settings (Not Yet Implemented)

For HTTP endpoint hosting:

- `Kestrel:Endpoints:Http/Https`
- `Kestrel:Limits:MaxConcurrentConnections`

**Note:** Currently this is a background service, not a web API

---

## 🔄 Resilience & Circuit Breaker

### 🔮 All Resilience Settings (Not Yet Implemented)

- `Resilience:EnableCircuitBreaker`
- `Resilience:EnableRetryPolicy`
- `Resilience:MaxRetryAttempts`

---

## 🏢 Custom Application Settings

### 🔮 All Custom Settings (Not Yet Implemented)

Company-specific metadata:

- `CustomSettings:CompanyName`
- `CustomSettings:ApplicationName`
- `CustomSettings:Version`
- `CustomSettings:SupportEmail`

---

## 📊 Configuration Summary by Status

### ✅ **CURRENTLY USED (REQUIRED)**

1. `ConnectionStrings:ClickHouse` ⚠️ SENSITIVE
2. `Notifications:TeamsWebhook` ⚠️ SENSITIVE (optional but recommended)
3. `Notifications:TelegramBotToken` ⚠️ SENSITIVE (optional but recommended)
4. `Notifications:TelegramChatId` ⚠️ SENSITIVE (optional but recommended)

### ✅ **CURRENTLY USED (WITH DEFAULTS)**

5. `ML:TrainingIntervalHours`
6. `ML:MinSessionsForTraining`
7. `ML:ScoringIntervalSeconds`
8. `ML:ScoringBatchSize`
9. `ML:DailyReportHour`
10. `Alerts:EnableDatabasePersistence`
11. `Alerts:DefaultThrottleWindowMinutes`
12. `Alerts:ThrottleWindowHours`
13. `Alerts:FraudTypeSettings:*`
14. `Serilog:*` (all settings)
15. `Logging:LogLevel:*`

### 🔮 **PREPARED FOR FUTURE**

- Performance tuning
- Advanced ML parameters
- Health checks & monitoring
- Feature flags
- Data retention policies
- External service integrations
- Security & authentication
- Resilience patterns

---

## 📁 Where to Set Each Configuration

### ✅ **appsettings.json** (Non-sensitive, committed to git)
- All ML settings
- All Alert settings
- Serilog configuration
- Logging levels
- Feature flags (when implemented)

### 🔐 **User Secrets** (Sensitive, development only)
```bash
dotnet user-secrets set "ConnectionStrings:ClickHouse" "..."
dotnet user-secrets set "Notifications:TeamsWebhook" "..."
dotnet user-secrets set "Notifications:TelegramBotToken" "..."
dotnet user-secrets set "Notifications:TelegramChatId" "..."
```

### 🌍 **Environment Variables** (Sensitive, production)
```bash
ConnectionStrings__ClickHouse=...
Notifications__TeamsWebhook=...
Notifications__TelegramBotToken=...
Notifications__TelegramChatId=...
```

---

## 🔍 Finding Configuration Usage in Code

### Search Patterns:

**Connection Strings:**
```csharp
config.GetConnectionString("ClickHouse")
```

**Direct Values:**
```csharp
config["Notifications:TeamsWebhook"]
config.GetValue<int>("ML:TrainingIntervalHours", 6)
```

**Sections:**
```csharp
config.GetSection("Alerts:FraudTypeSettings")
```

### Used In Files:
- `Services/ClickHouseService.cs`
- `Services/AlertHistoryService.cs`
- `Services/NotificationService.cs`
- `Services/HybridAlertService.cs`
- `BackgroundJobs/ModelTrainingJob.cs`
- `BackgroundJobs/RealTimeScoringJob.cs`
- `BackgroundJobs/DailyAnalysisJob.cs`
- `Program.cs`

---

## 🎯 Minimum Configuration to Run

**Absolute minimum:**
```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=yourpass"
  }
}
```

**Recommended minimum:**
Add at least ONE notification method (Teams or Telegram).

**Everything else has sensible defaults!** ✅

---

## 📚 Related Documentation

- [Quick Setup Guide](../CONFIGURATION_SETUP.md)
- [Complete Configuration Guide](./CONFIGURATION_GUIDE.md)
- [Telegram Setup](./TELEGRAM_SETUP.md)
- [Teams Setup](./TEAMS_SETUP.md)
