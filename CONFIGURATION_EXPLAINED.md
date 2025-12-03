# Configuration System Explained

## 📋 Overview

This document explains how the configuration system works in the Fraud Detection ML service, particularly focusing on the **unified configuration approach** that eliminates reload issues in production environments.

---

## 🎯 The Problem We Solved

### **Original Code (What You Didn't Like)**

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Beepul.Afs.FraudDetection.ML.Host")
    .Enrich.WithProperty("RunMode", runMode.ToString())
    .CreateLogger();
```

### **Problems with This Approach**

1. **❌ Limited Configuration Sources**
   - Only reads from `appsettings.json` and `appsettings.{Environment}.json`
   - Doesn't include `/settings.json` and `/run/secrets/secrets.json` used in Docker
   - Serilog configuration wouldn't see your production settings!

2. **❌ Default Reload Behavior**
   - By default, `AddJsonFile()` has `reloadOnChange: true`
   - Configuration files are monitored and reloaded when changed
   - This is DANGEROUS in containerized environments where:
     - Config files might be mounted as read-only
     - File watchers don't work properly with Docker volumes
     - Config changes during runtime can cause inconsistent state

3. **❌ Separate Configuration for Serilog**
   - Serilog uses one configuration builder
   - Application uses a different configuration builder
   - Can lead to inconsistencies between logging config and app config

---

## ✅ The Solution: Unified Configuration

### **New Approach**

```csharp
// Build unified configuration (used by both Serilog and application)
// This configuration is built ONCE and NOT reloaded during runtime
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
    .AddJsonFile("/settings.json", optional: true, reloadOnChange: false)  // Docker secrets/config
    .AddJsonFile("/run/secrets/secrets.json", optional: true, reloadOnChange: false)  // Docker secrets
    .AddEnvironmentVariables()
    .Build();

// Configure Serilog using unified configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)  // Use unified configuration
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Beepul.Afs.FraudDetection.ML.Host")
    .Enrich.WithProperty("RunMode", runMode.ToString())
    .CreateLogger();
```

---

## 🔍 Step-by-Step Explanation

### **Step 1: Build Unified Configuration**

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
```
- Sets the base directory for relative paths
- Usually the directory where the application runs

### **Step 2: Add Configuration Sources (In Order)**

```csharp
.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
```
- **File**: `appsettings.json`
- **Required**: `optional: false` - app fails if missing
- **Static**: `reloadOnChange: false` - NEVER reload during runtime
- **Purpose**: Base configuration for all environments

```csharp
.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
```
- **File**: `appsettings.Development.json`, `appsettings.Testing.json`, or `appsettings.Production.json`
- **Optional**: `optional: true` - OK if missing
- **Static**: `reloadOnChange: false` - NEVER reload
- **Purpose**: Environment-specific overrides
- **Example**:
  - Development: Swagger enabled, verbose logging
  - Production: Swagger disabled, minimal logging

```csharp
.AddJsonFile("/settings.json", optional: true, reloadOnChange: false)  // Docker secrets/config
```
- **File**: `/settings.json` (absolute path)
- **Optional**: `optional: true` - OK if missing (local dev won't have it)
- **Static**: `reloadOnChange: false` - NEVER reload
- **Purpose**: Docker Config in Testing/Production
- **Mounted via**: Docker Compose `configs:` section

```csharp
.AddJsonFile("/run/secrets/secrets.json", optional: true, reloadOnChange: false)  // Docker secrets
```
- **File**: `/run/secrets/secrets.json` (absolute path)
- **Optional**: `optional: true` - OK if missing (local dev won't have it)
- **Static**: `reloadOnChange: false` - NEVER reload
- **Purpose**: Docker Secrets (passwords, connection strings, API keys)
- **Mounted via**: Docker Compose `secrets:` section
- **Security**: More secure than environment variables

```csharp
.AddEnvironmentVariables()
```
- **Source**: Environment variables
- **Purpose**: Override any setting via environment
- **Priority**: HIGHEST (overrides all previous sources)
- **Example**:
  ```bash
  export ConnectionStrings__ClickHouse="Host=prod-db;Port=9000"
  ```
  This overrides the ConnectionStrings:ClickHouse from all JSON files

```csharp
.Build();
```
- Builds the final `IConfiguration` object
- Merges all sources with later sources overriding earlier ones

---

## 📊 Configuration Priority (Last Wins)

```
┌────────────────────────────────┐
│ 1. appsettings.json            │  ← Base (lowest priority)
├────────────────────────────────┤
│ 2. appsettings.{Env}.json      │  ← Environment override
├────────────────────────────────┤
│ 3. /settings.json              │  ← Docker Config
├────────────────────────────────┤
│ 4. /run/secrets/secrets.json   │  ← Docker Secrets
├────────────────────────────────┤
│ 5. Environment Variables       │  ← Highest priority
└────────────────────────────────┘
```

### **Example Scenario**

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=localhost;Port=9000"
  },
  "ApiKeys": {
    "ValidKeys": ["dev-key"]
  }
}
```

**appsettings.Production.json:**
```json
{
  "ApiKeys": {
    "ValidKeys": ["prod-key-from-file"]
  }
}
```

**/settings.json (Docker Config):**
```json
{
  "ML": {
    "TrainingIntervalHours": 24,
    "ScoringBatchSize": 1000
  }
}
```

**/run/secrets/secrets.json (Docker Secret):**
```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=prod-clickhouse;Port=9000;Password=secret123"
  },
  "ApiKeys": {
    "ValidKeys": ["prod-secret-key-1", "prod-secret-key-2"]
  }
}
```

**Environment Variable:**
```bash
export ML__ScoringBatchSize=5000
```

**Final Merged Configuration:**
```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=prod-clickhouse;Port=9000;Password=secret123"  // From secrets.json
  },
  "ApiKeys": {
    "ValidKeys": ["prod-secret-key-1", "prod-secret-key-2"]  // From secrets.json
  },
  "ML": {
    "TrainingIntervalHours": 24,      // From settings.json
    "ScoringBatchSize": 5000          // From environment variable (highest priority!)
  }
}
```

---

## 🔧 Using Configuration in Application

### **Step 3: Pass Configuration to Application Builders**

```csharp
async Task RunWebApiAsync(string[] arguments, IConfiguration config)
{
    var builder = WebApplication.CreateBuilder(arguments);

    // IMPORTANT: Clear default configuration and use unified configuration
    builder.Configuration.Sources.Clear();
    builder.Configuration.AddConfiguration(config);

    // Now builder.Configuration uses the unified config with all sources
}
```

### **Why Clear and Replace?**

- `WebApplication.CreateBuilder()` creates its own default configuration
- We want to use our **unified configuration** instead
- `Sources.Clear()` removes the default configuration sources
- `AddConfiguration(config)` uses our pre-built unified configuration

This ensures:
- ✅ Same configuration used everywhere
- ✅ No duplicate configuration loading
- ✅ Serilog and app use identical settings
- ✅ No reload during runtime

---

## 🎨 LaunchSettings.json Explained

### **File Location**
`Properties/launchSettings.json`

### **Purpose**
- Configures how the app runs in **Visual Studio** or **Rider**
- Defines **launch profiles** for different environments
- Sets **environment variables** and **application URLs**
- **NOT used in Docker** - only for local development

### **Your Configuration**

```json
{
  "profiles": {
    "Development": {
      "commandName": "Project",
      "applicationUrl": "http://0.0.0.0:5100",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "Testing": {
      "commandName": "Project",
      "applicationUrl": "http://0.0.0.0:80",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Testing",
        "OTL_INSTANCE_ID": "test"
      }
    }
  }
}
```

### **How to Use**

**In Visual Studio / Rider:**
```
Run → Select Profile → Development
```

**In Command Line:**
```bash
# Run Development profile
dotnet run --launch-profile Development

# Run Testing profile
dotnet run --launch-profile Testing
```

**What Happens:**
1. Sets `ASPNETCORE_ENVIRONMENT=Development` (or Testing, etc.)
2. App listens on specified `applicationUrl`
3. Loads corresponding `appsettings.{Environment}.json`
4. Sets additional environment variables (like `OTL_INSTANCE_ID`)

---

## 🔒 OpenTelemetry Instrumentation Explained

### **Code Added**

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(providerBuilder =>
    {
        providerBuilder.AddEntityFrameworkCoreInstrumentation();
    });
```

### **What It Does**

1. **AddOpenTelemetry()**: Registers OpenTelemetry services
2. **WithTracing()**: Enables distributed tracing
3. **AddEntityFrameworkCoreInstrumentation()**:
   - Automatically traces all database queries
   - Tracks query execution time
   - Records query parameters
   - Helps identify slow queries

### **Benefits**

- 🔍 **Visibility**: See all database operations
- ⏱️ **Performance**: Track query execution times
- 🐛 **Debugging**: Identify slow queries causing issues
- 📊 **Monitoring**: Export traces to monitoring tools (Jaeger, Zipkin, AppDynamics)

### **Example Trace**

When you call `ClickHouseService.GetSessionsByIdAsync()`:
```
Span: GetSessionsByIdAsync
  ├─ Span: ClickHouse Connection Open (2ms)
  ├─ Span: Execute Query: SELECT * FROM Sessions... (45ms)
  └─ Span: Map Results (3ms)
Total: 50ms
```

---

## 🎯 Benefits Summary

### **Why This Approach is Better**

1. ✅ **Single Source of Truth**
   - One configuration object used everywhere
   - Serilog and app use same settings
   - No configuration drift

2. ✅ **No Runtime Reloads**
   - Configuration loaded ONCE at startup
   - No file watchers
   - No surprises during runtime
   - Predictable behavior

3. ✅ **Docker-Friendly**
   - Reads from `/settings.json` (Docker Config)
   - Reads from `/run/secrets/secrets.json` (Docker Secrets)
   - Works with read-only file systems

4. ✅ **Flexible Override Hierarchy**
   - Base → Environment → Docker Config → Docker Secrets → Env Vars
   - Easy to override specific settings
   - Clear priority chain

5. ✅ **Secure**
   - Secrets in `/run/secrets/` (encrypted at rest in Swarm)
   - Not visible in container inspect
   - Not in environment variables (can leak in logs)

6. ✅ **Easy Local Development**
   - `settings.json` and `secrets.json` are optional
   - Works fine with just `appsettings.json`
   - Use launchSettings.json for different environments

---

## 🚀 Usage Examples

### **Local Development**

```bash
# Uses appsettings.json + appsettings.Development.json
dotnet run --launch-profile Development

# API available at: http://localhost:5100
# Swagger available at: http://localhost:5100
```

### **Docker Development**

```bash
docker run -p 5100:80 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -v $(pwd)/Models:/app/Models \
  fraud-detection:latest
```
Uses: `appsettings.json` + `appsettings.Development.json`

### **Docker Production with Secrets**

```yaml
# docker-compose.yml
services:
  fraud-detection-api:
    image: fraud-detection:latest
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    configs:
      - source: settings_source
        target: /settings.json
    secrets:
      - source: secrets_source
        target: /run/secrets/secrets.json
```

Uses (in order):
1. `appsettings.json`
2. `appsettings.Production.json`
3. `/settings.json` (Docker Config)
4. `/run/secrets/secrets.json` (Docker Secret)

---

## 🐛 Troubleshooting

### **Configuration Not Loading**

**Problem**: Settings from `/settings.json` not working

**Check**:
```bash
# Inside container
docker exec -it <container> ls -la /settings.json
docker exec -it <container> cat /settings.json
```

**Solution**: Verify Docker config is mounted correctly

### **Secrets Not Found**

**Problem**: Connection string from secrets not working

**Check**:
```bash
# Inside container
docker exec -it <container> ls -la /run/secrets/
docker exec -it <container> cat /run/secrets/secrets.json
```

**Solution**: Verify Docker secret is created and mounted

### **Environment Variable Override Not Working**

**Problem**: Set environment variable but not taking effect

**Check syntax**:
```bash
# WRONG
export ConnectionStrings:ClickHouse="..."

# CORRECT (use double underscore)
export ConnectionStrings__ClickHouse="..."
```

---

## 📝 Best Practices

1. **Development**: Use `appsettings.Development.json` only
2. **Testing**: Use Docker Config (`/settings.json`)
3. **Production**: Use Docker Secrets (`/run/secrets/secrets.json`)
4. **Never Commit Secrets**: Add to `.gitignore`
5. **Use Environment Variables**: For quick overrides (CI/CD)
6. **Keep Reload Off**: `reloadOnChange: false` always

---

**Questions?** Check the logs - configuration loading is logged on startup!
