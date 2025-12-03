# Why We Use Custom Configuration Instead of Default

## 📋 Overview

This document explains why we build our own configuration instead of using ASP.NET Core's default configuration system, and how the two approaches differ.

---

## 🤔 The Question

**"When I create a new Web API project, it automatically reads from `appsettings.json`. How does that work? Does it have a built-in function?"**

**Answer:** Yes! ASP.NET Core has built-in configuration that works automatically.

---

## ✅ ASP.NET Core's Built-In Configuration

### **What Happens Automatically**

When you create a new Web API project and use:

```csharp
var builder = WebApplication.CreateBuilder(args);
```

**It automatically configures everything behind the scenes!**

### **What `WebApplication.CreateBuilder(args)` Does Internally**

```csharp
// This is what happens internally when you call CreateBuilder():
var builder = WebApplication.CreateBuilder(args);

// Behind the scenes, ASP.NET Core does this:
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)  // ⚠️ Note: reloadOnChange: TRUE by default!
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// In Development environment, it also adds:
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}
```

**So yes, it's completely built-in!** You don't need to manually configure anything for basic usage.

---

## 🚨 The Problem: Default Behavior vs Our Requirements

### **Default Behavior (What ASP.NET Core Does)**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configuration is already loaded:
// ✅ appsettings.json (reloadOnChange: TRUE)
// ✅ appsettings.Development.json (reloadOnChange: TRUE)
// ✅ Environment Variables
// ✅ Command Line Arguments

// Configure Serilog AFTER builder creation
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

var app = builder.Build();
```

### **Problems with Default Approach**

| Problem | Description | Impact |
|---------|-------------|--------|
| **1. Configuration Reloads** | `reloadOnChange: true` by default | ❌ Files monitored, reloaded during runtime |
| **2. Limited Sources** | Only reads appsettings files | ❌ Doesn't include `/settings.json` or `/run/secrets/secrets.json` |
| **3. Late Serilog Config** | Serilog configured after builder creation | ❌ Can't log early startup issues |
| **4. Different Configs** | Serilog uses different config than early startup | ❌ Inconsistencies between logging sources |

### **Our Requirements**

Based on your explicit requirements:

1. ❌ **"I don't like appsettings to reload"** - No runtime reloading
2. ✅ Need to read `/settings.json` (Docker Config)
3. ✅ Need to read `/run/secrets/secrets.json` (Docker Secrets)
4. ✅ Serilog must use the **same configuration** as the application
5. ✅ Serilog must be configured **before** creating the builder (for early logging)

---

## 💡 Our Solution: Custom Unified Configuration

### **How We Build Configuration**

```csharp
// Step 1: Build unified configuration OUTSIDE the builder
// This configuration is built ONCE and NOT reloaded during runtime
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)  // ✅ reloadOnChange: FALSE
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
    .AddJsonFile("/settings.json", optional: true, reloadOnChange: false)  // ✅ Docker Config
    .AddJsonFile("/run/secrets/secrets.json", optional: true, reloadOnChange: false)  // ✅ Docker Secrets
    .AddEnvironmentVariables()
    .Build();

// Step 2: Configure Serilog using unified configuration BEFORE builder creation
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)  // ✅ Uses our unified config
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Beepul.Afs.FraudDetection.ML.Host")
    .Enrich.WithProperty("RunMode", runMode.ToString())
    .CreateLogger();

// Step 3: Create the Web API builder
var builder = WebApplication.CreateBuilder(args);

// Step 4: Clear default configuration and use our unified configuration
builder.Configuration.Sources.Clear();  // ⚠️ Remove default config sources
builder.Configuration.AddConfiguration(configuration);  // ✅ Use our unified config

// Step 5: Tell the builder to use Serilog
builder.Host.UseSerilog();  // Uses Log.Logger we configured earlier

var app = builder.Build();
```

---

## 🔍 Why We Clear Default Configuration

### **Without Clearing**

If we **don't clear** the default sources, you'd have **DUPLICATE configuration sources**:

```csharp
var builder = WebApplication.CreateBuilder(args);

// At this point, builder.Configuration.Sources contains:
[
    // ❌ Default sources added by CreateBuilder():
    JsonConfigurationSource { Path: "appsettings.json", ReloadOnChange: true },
    JsonConfigurationSource { Path: "appsettings.Development.json", ReloadOnChange: true },
    EnvironmentVariablesConfigurationSource { },
    CommandLineConfigurationSource { }
]

// If we just add our configuration without clearing:
builder.Configuration.AddConfiguration(configuration);

// Now builder.Configuration.Sources contains:
[
    // ❌ Default sources (still present!):
    JsonConfigurationSource { Path: "appsettings.json", ReloadOnChange: true },
    JsonConfigurationSource { Path: "appsettings.Development.json", ReloadOnChange: true },
    EnvironmentVariablesConfigurationSource { },
    CommandLineConfigurationSource { },

    // ✅ Our sources (added):
    JsonConfigurationSource { Path: "appsettings.json", ReloadOnChange: false },
    JsonConfigurationSource { Path: "appsettings.Development.json", ReloadOnChange: false },
    JsonConfigurationSource { Path: "/settings.json", ReloadOnChange: false },
    JsonConfigurationSource { Path: "/run/secrets/secrets.json", ReloadOnChange: false },
    EnvironmentVariablesConfigurationSource { }
]
```

### **Problems with Duplicates**

| Problem | Description |
|---------|-------------|
| **Duplicate Keys** | Same configuration keys appear twice |
| **Reload Behavior** | File watchers still active from default sources |
| **Confusion** | Hard to know which source provides which value |
| **Performance** | Reading same files twice |
| **Inconsistency** | Default sources might override our values |

### **With Clearing**

```csharp
builder.Configuration.Sources.Clear();  // Remove all default sources
builder.Configuration.AddConfiguration(configuration);  // Add only our sources

// Now builder.Configuration.Sources contains:
[
    // ✅ Only our sources:
    JsonConfigurationSource { Path: "appsettings.json", ReloadOnChange: false },
    JsonConfigurationSource { Path: "appsettings.Development.json", ReloadOnChange: false },
    JsonConfigurationSource { Path: "/settings.json", ReloadOnChange: false },
    JsonConfigurationSource { Path: "/run/secrets/secrets.json", ReloadOnChange: false },
    EnvironmentVariablesConfigurationSource { }
]
```

**Benefits:**
- ✅ Clean, predictable configuration sources
- ✅ No duplicates
- ✅ No reload behavior
- ✅ Single source of truth
- ✅ Same configuration everywhere

---

## 📊 Comparison: Default vs Custom

| Aspect | Default (Built-In) | Custom (Our Approach) |
|--------|-------------------|----------------------|
| **Configuration Files** | `appsettings.json`<br>`appsettings.{Env}.json` | `appsettings.json`<br>`appsettings.{Env}.json`<br>`/settings.json`<br>`/run/secrets/secrets.json` |
| **Reload Behavior** | ✅ `reloadOnChange: true` | ❌ `reloadOnChange: false` |
| **Serilog Configuration** | After builder creation | Before builder creation |
| **Early Startup Logging** | ❌ Limited | ✅ Full logging capability |
| **Docker Support** | ❌ No built-in support for configs/secrets | ✅ Reads from Docker mounts |
| **Configuration Consistency** | ⚠️ Serilog uses different config initially | ✅ Single unified config |
| **Code Complexity** | Simple (automatic) | More explicit (manual) |
| **Production Readiness** | ⚠️ Requires workarounds | ✅ Production-ready |

---

## 🎯 When to Use Each Approach

### **Use Default Configuration When:**

✅ Simple application with no special requirements
✅ Okay with configuration reloading during runtime
✅ Don't need Docker configs/secrets
✅ Don't need early startup logging
✅ Standard ASP.NET Core deployment

### **Use Custom Configuration When:**

✅ Need to disable configuration reloading (production stability)
✅ Need Docker Config and Secrets support
✅ Need early startup logging with Serilog
✅ Need unified configuration across all components
✅ Need explicit control over configuration sources
✅ Need to meet specific production requirements

**Your application needs custom configuration because:**
- You explicitly don't want reloading
- You use Docker Swarm with configs and secrets
- You need comprehensive Serilog configuration
- You need production-grade stability

---

## 🔄 Configuration Flow Comparison

### **Default Approach Flow**

```
1. WebApplication.CreateBuilder(args)
   ↓
2. Automatic configuration loading:
   - appsettings.json (reloadOnChange: true)
   - appsettings.{Environment}.json (reloadOnChange: true)
   - Environment Variables
   ↓
3. Serilog configured later
   ↓
4. Application starts
```

**Issue:** Configuration can reload during runtime, limited sources, late Serilog initialization.

---

### **Custom Approach Flow**

```
1. Build unified configuration ONCE:
   - appsettings.json (reloadOnChange: false)
   - appsettings.{Environment}.json (reloadOnChange: false)
   - /settings.json (reloadOnChange: false)
   - /run/secrets/secrets.json (reloadOnChange: false)
   - Environment Variables
   ↓
2. Configure Serilog with unified configuration
   ↓
3. WebApplication.CreateBuilder(args)
   ↓
4. Clear default configuration
   ↓
5. Use unified configuration
   ↓
6. Application starts
```

**Benefits:** Static configuration, all sources included, early Serilog, single source of truth.

---

## 💻 Code Examples

### **Example 1: Default ASP.NET Core Configuration**

```csharp
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configuration already loaded automatically:
// - appsettings.json (reloadOnChange: true)
// - appsettings.Development.json (reloadOnChange: true)
// - Environment Variables

// Configure Serilog (AFTER builder creation)
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
```

**Limitations:**
- ❌ Configuration reloads during runtime
- ❌ Can't read from `/settings.json` or `/run/secrets/secrets.json`
- ❌ Late Serilog initialization (can't log early startup)

---

### **Example 2: Custom Unified Configuration (Our Approach)**

```csharp
using Serilog;

// Build unified configuration FIRST
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
    .AddJsonFile("/settings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("/run/secrets/secrets.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

// Configure Serilog EARLY
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting application...");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default configuration with unified configuration
    builder.Configuration.Sources.Clear();
    builder.Configuration.AddConfiguration(configuration);

    // Use Serilog
    builder.Host.UseSerilog();

    builder.Services.AddControllers();

    var app = builder.Build();

    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

**Benefits:**
- ✅ Configuration never reloads during runtime
- ✅ Reads from all sources (appsettings, settings, secrets)
- ✅ Early Serilog initialization (logs everything)
- ✅ Single unified configuration

---

## 🔐 Security Implications

### **Default Configuration**

```csharp
var builder = WebApplication.CreateBuilder(args);
// Only reads appsettings.json and environment variables
// Secrets must be in environment variables (visible in docker inspect)
```

**Security Issues:**
- Environment variables visible in `docker inspect`
- Environment variables can leak in logs
- No built-in Docker Secrets support

---

### **Custom Configuration**

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("/run/secrets/secrets.json", optional: true, reloadOnChange: false)
    .Build();
```

**Security Benefits:**
- ✅ Secrets stored in encrypted Docker Secrets
- ✅ Secrets not visible in `docker inspect`
- ✅ Secrets not in environment variables
- ✅ Secrets file readable only by container user

---

## 📝 Summary

| Question | Answer |
|----------|--------|
| **Does ASP.NET Core have built-in configuration?** | ✅ Yes! `WebApplication.CreateBuilder()` loads configuration automatically |
| **Why don't we use it?** | ❌ It uses `reloadOnChange: true` by default (you don't want this) |
| **What's different in our approach?** | ✅ We build configuration with `reloadOnChange: false` and include Docker configs/secrets |
| **Why do we clear default sources?** | ✅ To avoid duplicates and ensure our configuration is used |
| **Is this standard practice?** | ⚠️ Default is fine for most apps; custom is needed for production with specific requirements |

---

## 🎓 Key Takeaways

1. **ASP.NET Core has excellent built-in configuration** - It works great for 90% of applications
2. **Our requirements are specific** - No reloading, Docker support, early Serilog, unified config
3. **We override the default** - Clear built-in sources and use our own
4. **This is intentional and correct** - Not a workaround, but a deliberate design choice
5. **Both approaches are valid** - Use default for simple apps, custom for production with specific needs

---

## 📚 Related Documentation

- [CONFIGURATION_EXPLAINED.md](./CONFIGURATION_EXPLAINED.md) - Detailed configuration system explanation
- [CONFIGURATION_VALUES.md](./CONFIGURATION_VALUES.md) - All configuration values reference
- [DEPLOYMENT_GUIDE.md](./deploy/DEPLOYMENT_GUIDE.md) - Deployment procedures

---

**Last Updated:** December 2024
**Version:** 2.0.0
**Reason for Custom Configuration:** Production stability, Docker support, no runtime reloading
