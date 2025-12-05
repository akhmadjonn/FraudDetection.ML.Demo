# 🔐 How to Save Notification Secrets

## Method 1: Direct JSON Format (secrets.json)

**Location:** `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

**Your UserSecretsId:** `dotnet-FraudDetection.ML-aed1f83c-3802-4353-82f7-ba6cb8eddaae`

**Full Path:** `~/.microsoft/usersecrets/dotnet-FraudDetection.ML-aed1f83c-3802-4353-82f7-ba6cb8eddaae/secrets.json`

### JSON Format:

```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=your_real_password"
  },
  "Notifications": {
    "TeamsWebhook": "https://outlook.office.com/webhook/abc123-def456-ghi789@xyz123/IncomingWebhook/abc/def123456789",
    "TelegramBotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz",
    "TelegramChatId": "-1001234567890"
  }
}
```

**Note:** The keys are:
- `Notifications:TeamsWebhook` (NOT `Notifications__TeamsWebhook_EXAMPLE`)
- `Notifications:TelegramBotToken` (NOT `Notifications__TelegramBotToken_EXAMPLE`)
- `Notifications:TelegramChatId` (NOT `Notifications__TelegramChatId_EXAMPLE`)

---

## Method 2: Using dotnet CLI (Recommended)

### Step-by-step:

```bash
# 1. Navigate to project directory
cd /home/user/FraudDetection.ML.Demo

# 2. Set ClickHouse connection (REQUIRED)
dotnet user-secrets set "ConnectionStrings:ClickHouse" "Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=your_real_password"

# 3. Set Teams Webhook (OPTIONAL)
dotnet user-secrets set "Notifications:TeamsWebhook" "https://outlook.office.com/webhook/abc123-def456-ghi789@xyz123/IncomingWebhook/abc/def123456789"

# 4. Set Telegram Bot Token (OPTIONAL)
dotnet user-secrets set "Notifications:TelegramBotToken" "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"

# 5. Set Telegram Chat ID (OPTIONAL)
dotnet user-secrets set "Notifications:TelegramChatId" "-1001234567890"

# 6. Verify all secrets are set
dotnet user-secrets list
```

---

## 📋 Real Examples with REAL Format

### Example 1: Microsoft Teams Webhook

**Real webhook URL looks like:**
```
https://outlook.office.com/webhook/a1b2c3d4-e5f6-7890-abcd-ef1234567890@12345678-90ab-cdef-1234-567890abcdef/IncomingWebhook/1a2b3c4d5e6f7890abcdef123456/12345678-90ab-cdef-1234-567890abcdef
```

**How to set:**

**CLI:**
```bash
dotnet user-secrets set "Notifications:TeamsWebhook" "https://outlook.office.com/webhook/a1b2c3d4-e5f6-7890-abcd-ef1234567890@12345678-90ab-cdef-1234-567890abcdef/IncomingWebhook/1a2b3c4d5e6f7890abcdef123456/12345678-90ab-cdef-1234-567890abcdef"
```

**JSON:**
```json
{
  "Notifications": {
    "TeamsWebhook": "https://outlook.office.com/webhook/a1b2c3d4-e5f6-7890-abcd-ef1234567890@12345678-90ab-cdef-1234-567890abcdef/IncomingWebhook/1a2b3c4d5e6f7890abcdef123456/12345678-90ab-cdef-1234-567890abcdef"
  }
}
```

---

### Example 2: Telegram Bot Token

**Real bot token looks like:**
```
1234567890:ABCdefGHIjklMNOpqrsTUVwxyz-1234567890
```

**How to set:**

**CLI:**
```bash
dotnet user-secrets set "Notifications:TelegramBotToken" "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz-1234567890"
```

**JSON:**
```json
{
  "Notifications": {
    "TelegramBotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz-1234567890"
  }
}
```

---

### Example 3: Telegram Chat ID

**Real chat ID looks like:**
- For personal chat: `987654321` (positive number)
- For group chat: `-1001234567890` (negative number, starts with -100)

**How to set:**

**CLI:**
```bash
# Personal chat
dotnet user-secrets set "Notifications:TelegramChatId" "987654321"

# Group chat (recommended)
dotnet user-secrets set "Notifications:TelegramChatId" "-1001234567890"
```

**JSON:**
```json
{
  "Notifications": {
    "TelegramChatId": "-1001234567890"
  }
}
```

---

### Example 4: ClickHouse Connection String

**Real connection string looks like:**
```
Host=192.168.1.100;Port=8123;Database=fraud_detection;Username=default;Password=MySecureP@ssw0rd
```

**How to set:**

**CLI:**
```bash
dotnet user-secrets set "ConnectionStrings:ClickHouse" "Host=192.168.1.100;Port=8123;Database=fraud_detection;Username=default;Password=MySecureP@ssw0rd"
```

**JSON:**
```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=192.168.1.100;Port=8123;Database=fraud_detection;Username=default;Password=MySecureP@ssw0rd"
  }
}
```

---

## 🎯 Complete Example - All Secrets Together

### Full secrets.json File:

```json
{
  "ConnectionStrings": {
    "ClickHouse": "Host=192.168.1.100;Port=8123;Database=fraud_detection;Username=fraud_user;Password=MySecureP@ssw0rd123"
  },
  "Notifications": {
    "TeamsWebhook": "https://outlook.office.com/webhook/a1b2c3d4-e5f6-7890-abcd-ef1234567890@12345678-90ab-cdef-1234-567890abcdef/IncomingWebhook/1a2b3c4d5e6f7890abcdef123456/12345678-90ab-cdef-1234-567890abcdef",
    "TelegramBotToken": "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz-1234567890",
    "TelegramChatId": "-1001234567890"
  }
}
```

### OR using CLI (one by one):

```bash
cd /home/user/FraudDetection.ML.Demo

dotnet user-secrets set "ConnectionStrings:ClickHouse" "Host=192.168.1.100;Port=8123;Database=fraud_detection;Username=fraud_user;Password=MySecureP@ssw0rd123"

dotnet user-secrets set "Notifications:TeamsWebhook" "https://outlook.office.com/webhook/a1b2c3d4-e5f6-7890-abcd-ef1234567890@12345678-90ab-cdef-1234-567890abcdef/IncomingWebhook/1a2b3c4d5e6f7890abcdef123456/12345678-90ab-cdef-1234-567890abcdef"

dotnet user-secrets set "Notifications:TelegramBotToken" "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz-1234567890"

dotnet user-secrets set "Notifications:TelegramChatId" "-1001234567890"
```

---

## 🔍 How to Verify

```bash
# List all secrets
dotnet user-secrets list

# Expected output:
# ConnectionStrings:ClickHouse = Host=192.168.1.100;Port=8123;Database=fraud_detection;Username=fraud_user;Password=MySecureP@ssw0rd123
# Notifications:TeamsWebhook = https://outlook.office.com/webhook/...
# Notifications:TelegramBotToken = 1234567890:ABCdefGHIjklMNOpqrsTUVwxyz-1234567890
# Notifications:TelegramChatId = -1001234567890
```

---

## 📁 Where is secrets.json Located?

### Linux/macOS:
```
~/.microsoft/usersecrets/dotnet-FraudDetection.ML-aed1f83c-3802-4353-82f7-ba6cb8eddaae/secrets.json
```

### Windows:
```
%APPDATA%\Microsoft\UserSecrets\dotnet-FraudDetection.ML-aed1f83c-3802-4353-82f7-ba6cb8eddaae\secrets.json
```

### To find it:
```bash
# View UserSecretsId from .csproj
cat Beepul.Afs.FraudDetection.ML.Host.csproj | grep UserSecretsId

# The path will be:
# ~/.microsoft/usersecrets/<UserSecretsId>/secrets.json
```

---

## ⚙️ For Production (Environment Variables)

**In production, DON'T use User Secrets. Use Environment Variables instead:**

### Format (notice double underscores `__` instead of colons `:`):

```bash
# Linux/Docker/Kubernetes
export ConnectionStrings__ClickHouse="Host=prod-server;Port=8123;Database=fraud_prod;Username=fraud_user;Password=ProdP@ssw0rd"
export Notifications__TeamsWebhook="https://outlook.office.com/webhook/..."
export Notifications__TelegramBotToken="1234567890:ABCdef..."
export Notifications__TelegramChatId="-1001234567890"
```

### In Docker Compose:
```yaml
environment:
  - ConnectionStrings__ClickHouse=Host=prod-server;Port=8123;Database=fraud_prod;Username=fraud_user;Password=ProdP@ssw0rd
  - Notifications__TeamsWebhook=https://outlook.office.com/webhook/...
  - Notifications__TelegramBotToken=1234567890:ABCdef...
  - Notifications__TelegramChatId=-1001234567890
```

### In systemd service file:
```ini
# /etc/frauddetection/secrets.env
ConnectionStrings__ClickHouse=Host=prod-server;Port=8123;Database=fraud_prod;Username=fraud_user;Password=ProdP@ssw0rd
Notifications__TeamsWebhook=https://outlook.office.com/webhook/...
Notifications__TelegramBotToken=1234567890:ABCdef...
Notifications__TelegramChatId=-1001234567890
```

---

## 🎯 Key Takeaways

1. **User Secrets (Development):**
   - Use `:` (colon) separator → `Notifications:TeamsWebhook`
   - Set via CLI: `dotnet user-secrets set "Key" "Value"`
   - Or edit JSON directly at `~/.microsoft/usersecrets/<id>/secrets.json`

2. **Environment Variables (Production):**
   - Use `__` (double underscore) separator → `Notifications__TeamsWebhook`
   - Set in system environment, Docker, Kubernetes, systemd, etc.

3. **NEVER commit secrets to git!**
   - User Secrets are automatically excluded
   - Environment files should be in `.gitignore`

4. **Key names (correct format):**
   - ✅ `Notifications:TeamsWebhook` (in User Secrets/JSON)
   - ✅ `Notifications__TeamsWebhook` (in Environment Variables)
   - ❌ `Notifications__TeamsWebhook_EXAMPLE` (this is just for documentation)
