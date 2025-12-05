# 🔐 API Key Authentication Configuration

## Overview

Based on the implementation in `claude/refactor-fraud-detection-service-015JmzKr9wUBXunBixHpnTmY` branch, the Web API mode uses **API Key authentication** via HTTP headers.

---

## 🏗️ How It Works

### Middleware Implementation

**File:** `Middleware/ApiKeyAuthMiddleware.cs`

**Authentication Flow:**

1. **Request arrives** at the API endpoint
2. **Middleware checks** for `X-API-Key` header
3. **Validates** the provided key against configured valid keys
4. **Allows or blocks** the request based on validation

**Exempted Endpoints** (No authentication required):
- `/health`
- `/ping`
- `/swagger`

---

## 📋 Configuration Structure

### In appsettings.json (Development - EXAMPLE ONLY):

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

⚠️ **WARNING:** These are development/test keys. **NEVER** commit production keys to git!

---

## 🔐 Where to Store API Keys

### ✅ Development (User Secrets):

```bash
# Navigate to project
cd /home/user/FraudDetection.ML.Demo

# Set API keys as JSON array
dotnet user-secrets set "ApiKeys:ValidKeys:0" "dev-key-your-secure-key-here"
dotnet user-secrets set "ApiKeys:ValidKeys:1" "dev-key-another-key-here"

# Verify
dotnet user-secrets list
```

**JSON format in secrets.json:**
```json
{
  "ApiKeys": {
    "ValidKeys": [
      "dev-key-your-secure-key-here",
      "dev-key-another-key-here"
    ]
  }
}
```

---

### ✅ Production (Environment Variables):

**Method 1: JSON Array Format**
```bash
export ApiKeys__ValidKeys__0="prod-key-secure-key-1234567890abcdef"
export ApiKeys__ValidKeys__1="prod-key-another-secure-key-xyz"
```

**Method 2: Using appsettings.Production.json (Only if secured)**
```json
{
  "ApiKeys": {
    "ValidKeys": [
      "prod-key-from-secrets-manager"
    ]
  }
}
```

⚠️ **Best Practice:** Load from Azure Key Vault, AWS Secrets Manager, or Docker secrets

---

### ✅ Docker Compose:

```yaml
services:
  frauddetection-api:
    image: frauddetection-ml:latest
    environment:
      - ApiKeys__ValidKeys__0=prod-key-secure-key-1234567890abcdef
      - ApiKeys__ValidKeys__1=prod-key-another-secure-key-xyz
```

---

### ✅ Kubernetes Secrets:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: frauddetection-api-secrets
type: Opaque
stringData:
  ApiKeys__ValidKeys__0: "prod-key-secure-key-1234567890abcdef"
  ApiKeys__ValidKeys__1: "prod-key-another-secure-key-xyz"
```

---

## 🔑 Generating Secure API Keys

### Recommended Format:

```bash
# Generate a secure random API key (32 characters)
openssl rand -base64 32

# Output example:
# xK9mP2vL4qR8wE3nT7yU5zH1jF6sD0aQ

# Your API key format:
# prod-key-xK9mP2vL4qR8wE3nT7yU5zH1jF6sD0aQ
```

**Format Convention:**
```
{environment}-key-{random-string}

Examples:
- dev-key-xK9mP2vL4qR8wE3nT7yU5zH1jF6sD0aQ
- prod-key-zH8nR4qP9wE2vL6yU1xK5sD3jF7aT0mQ
- test-key-yU7mL2vK6qR9wE4nT8zH3jF1sD5aQ0pP
```

---

## 📡 How to Use API Keys (Client Side)

### cURL Example:

```bash
curl -X POST "https://your-api.com/api/frauddetection/analyze" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: prod-key-xK9mP2vL4qR8wE3nT7yU5zH1jF6sD0aQ" \
  -d '{
    "sessionId": "session-abc-123"
  }'
```

### C# Example:

```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-API-Key", "prod-key-your-key-here");

var request = new HttpRequestMessage(HttpMethod.Post, "https://your-api.com/api/frauddetection/analyze");
request.Content = JsonContent.Create(new { sessionId = "session-abc-123" });

var response = await client.SendAsync(request);
```

### JavaScript/TypeScript Example:

```javascript
const response = await fetch('https://your-api.com/api/frauddetection/analyze', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': 'prod-key-your-key-here'
  },
  body: JSON.stringify({
    sessionId: 'session-abc-123'
  })
});
```

### Python Example:

```python
import requests

headers = {
    'Content-Type': 'application/json',
    'X-API-Key': 'prod-key-your-key-here'
}

data = {
    'sessionId': 'session-abc-123'
}

response = requests.post(
    'https://your-api.com/api/frauddetection/analyze',
    headers=headers,
    json=data
)
```

---

## 🚨 Error Responses

### Missing API Key (401 Unauthorized):

```json
{
  "Error": "Unauthorized",
  "Message": "API Key is missing. Please provide X-API-Key header."
}
```

### Invalid API Key (403 Forbidden):

```json
{
  "Error": "Forbidden",
  "Message": "Invalid API Key"
}
```

### No API Keys Configured (500 Internal Server Error):

```json
{
  "Error": "Internal Server Error",
  "Message": "API authentication not properly configured"
}
```

---

## 🔍 Code Implementation Details

### Middleware Logic (from `ApiKeyAuthMiddleware.cs`):

```csharp
// Read API key from header
const string API_KEY_HEADER = "X-API-Key";
if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
{
    // Return 401 Unauthorized
}

// Get valid keys from configuration
var validApiKeys = _configuration.GetSection("ApiKeys:ValidKeys").Get<string[]>();

// Validate
if (!validApiKeys.Contains(providedKey))
{
    // Return 403 Forbidden
}

// Continue to endpoint
await _next(context);
```

### Usage in Program.cs:

```csharp
// Line 104
app.UseApiKeyAuth();
```

---

## 📊 Configuration Summary

| Configuration | Type | Storage Location | Required | Example |
|--------------|------|------------------|----------|---------|
| `ApiKeys:ValidKeys` | Array of Strings | 🔐 **User Secrets** (dev)<br>🔐 **Environment Variables** (prod) | ✅ **YES** (for Web API mode) | `["dev-key-abc123", "prod-key-xyz789"]` |

---

## 🎯 Complete Configuration Example

### User Secrets (Development):

```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=devpass"
  },
  "ApiKeys": {
    "ValidKeys": [
      "dev-key-xK9mP2vL4qR8wE3nT7yU5zH1jF6sD0aQ",
      "test-key-yU7mL2vK6qR9wE4nT8zH3jF1sD5aQ0pP"
    ]
  },
  "Notifications": {
    "TeamsWebhook": "https://outlook.office.com/webhook/...",
    "TelegramBotToken": "1234567890:ABC...",
    "TelegramChatId": "-1001234567890"
  }
}
```

### Environment Variables (Production):

```bash
# Database
export ConnectionStrings__ClickHouse="Host=prod-db;Port=8123;Database=fraud_prod;Username=fraud_user;Password=SecurePass"

# API Keys (Multiple keys for different clients)
export ApiKeys__ValidKeys__0="prod-key-client1-xK9mP2vL4qR8wE3nT7yU5zH1jF6sD0aQ"
export ApiKeys__ValidKeys__1="prod-key-client2-zH8nR4qP9wE2vL6yU1xK5sD3jF7aT0mQ"
export ApiKeys__ValidKeys__2="prod-key-internal-yU7mL2vK6qR9wE4nT8zH3jF1sD5aQ0pP"

# Notifications
export Notifications__TeamsWebhook="https://outlook.office.com/webhook/..."
export Notifications__TelegramBotToken="1234567890:ABC..."
export Notifications__TelegramChatId="-1001234567890"
```

---

## 🔒 Security Best Practices

### ✅ DO:
- ✅ Use strong, random API keys (minimum 32 characters)
- ✅ Store in User Secrets (dev) or Environment Variables (prod)
- ✅ Use different keys per environment (dev, test, prod)
- ✅ Use different keys per client/application
- ✅ Rotate keys periodically
- ✅ Log failed authentication attempts
- ✅ Use HTTPS only in production

### ❌ DON'T:
- ❌ Commit API keys to git
- ❌ Use simple/guessable keys like "test-key-123"
- ❌ Share API keys between environments
- ❌ Send API keys in URL query parameters
- ❌ Log API keys in application logs
- ❌ Hardcode API keys in source code

---

## 📝 Adding API Key to Current Branch

If you want to add API key authentication to the current branch, you need:

1. **Change project type** from `Microsoft.NET.Sdk.Worker` to `Microsoft.NET.Sdk.Web`
2. **Add Web API packages** (see refactor branch `.csproj`)
3. **Add Middleware** (`ApiKeyAuthMiddleware.cs`)
4. **Add Controllers** (FraudDetectionController, etc.)
5. **Update Program.cs** to support both Web API and Background modes
6. **Configure API keys** in User Secrets/Environment Variables

---

## 🔗 Related Documentation

- [Configuration Guide](../CONFIGURATION_SETUP.md) - Main configuration setup
- [Secrets Guide](../HOW_TO_SAVE_SECRETS.md) - How to save secrets in JSON format
- [Which Config Where](../WHICH_CONFIG_WHERE.md) - Configuration storage reference

---

## 📍 Implementation Location

**Branch:** `claude/refactor-fraud-detection-service-015JmzKr9wUBXunBixHpnTmY`

**Files:**
- `Middleware/ApiKeyAuthMiddleware.cs` - Authentication middleware
- `Controllers/FraudDetectionController.cs` - Example controller
- `Program.cs:104` - Middleware registration
- `appsettings.json:9-14` - Configuration structure

---

## 🚀 Quick Setup Commands

```bash
# For Web API mode (if using refactor branch)
cd /home/user/FraudDetection.ML.Demo

# Set API keys
dotnet user-secrets set "ApiKeys:ValidKeys:0" "dev-key-$(openssl rand -base64 32)"
dotnet user-secrets set "ApiKeys:ValidKeys:1" "test-key-$(openssl rand -base64 32)"

# Verify
dotnet user-secrets list | grep ApiKeys

# Run in Web API mode
dotnet run

# Test API (in another terminal)
curl -X GET "http://localhost:5000/health" -H "X-API-Key: dev-key-your-key-here"
```
