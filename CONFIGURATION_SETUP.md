# ⚡ Quick Configuration Setup

## 🎯 What You Need to Configure

### 🔴 REQUIRED (Application won't start without these):
1. **ClickHouse Connection String** - Database connection
2. Either **Teams Webhook** OR **Telegram Bot** (or both) - For alerts

### 🟡 OPTIONAL (Application will work without these):
- ML interval customizations
- Alert throttle window overrides
- Custom logging paths

---

## 🚀 Setup in 3 Steps

### Step 1: Copy Example Configuration Files

```bash
cd /home/user/FraudDetection.ML.Demo

# Copy base configuration (already exists, but update if needed)
# Edit appsettings.json - no secrets here, only non-sensitive defaults

# For Development
cp appsettings.Development.example.json appsettings.Development.json
```

### Step 2: Set Up Development Secrets (User Secrets)

```bash
# Initialize user secrets (already done, but run if needed)
dotnet user-secrets init

# ========== CLICKHOUSE (REQUIRED) ==========
dotnet user-secrets set "ConnectionStrings:ClickHouse" "Host=YOUR_HOST;Port=8123;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD"

# Example for local ClickHouse:
# dotnet user-secrets set "ConnectionStrings:ClickHouse" "Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=yourpassword"

# ========== MICROSOFT TEAMS (OPTIONAL) ==========
# See docs/TEAMS_SETUP.md for how to get this URL
dotnet user-secrets set "Notifications:TeamsWebhook" "https://outlook.office.com/webhook/YOUR-WEBHOOK-URL"

# ========== TELEGRAM (OPTIONAL) ==========
# See docs/TELEGRAM_SETUP.md for how to get these
dotnet user-secrets set "Notifications:TelegramBotToken" "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
dotnet user-secrets set "Notifications:TelegramChatId" "-1001234567890"
```

### Step 3: Verify Configuration

```bash
# List all secrets (to verify they're set)
dotnet user-secrets list

# Run the application
dotnet run

# Check logs for successful startup
# You should see:
# - "Fraud Detection ML Service Starting..."
# - "AlertHistory table initialized successfully"
# - "Model Training Job started"
```

---

## 🔧 Configuration by Environment

### 💻 LOCAL DEVELOPMENT

**Files you need:**
- ✅ `appsettings.json` (already configured)
- ✅ `appsettings.Development.json` (copy from example)
- ✅ User Secrets (run commands above)

**Commands:**
```bash
# Set all secrets
dotnet user-secrets set "ConnectionStrings:ClickHouse" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "Notifications:TeamsWebhook" "YOUR_TEAMS_WEBHOOK"
dotnet user-secrets set "Notifications:TelegramBotToken" "YOUR_BOT_TOKEN"
dotnet user-secrets set "Notifications:TelegramChatId" "YOUR_CHAT_ID"

# Run
dotnet run
```

---

### 🐳 DOCKER DEPLOYMENT

**File you need:**
- ✅ `docker-compose.yml` (copy from docker-compose.secrets.example.yml)

**Steps:**
```bash
# Copy example
cp docker-compose.secrets.example.yml docker-compose.yml

# Edit and replace all YOUR_* placeholders
nano docker-compose.yml

# Start
docker-compose up -d

# Check logs
docker-compose logs -f frauddetection-ml
```

**Edit these values in docker-compose.yml:**
```yaml
environment:
  - ConnectionStrings__ClickHouse=Host=clickhouse;Port=8123;Database=fraud_detection;Username=fraud_user;Password=CHANGE_THIS
  - Notifications__TeamsWebhook=https://outlook.office.com/webhook/CHANGE_THIS
  - Notifications__TelegramBotToken=CHANGE_THIS
  - Notifications__TelegramChatId=CHANGE_THIS
```

---

### ⚙️ LINUX SERVER (systemd)

**Files you need:**
- ✅ `/etc/frauddetection/secrets.env` (copy from deploy/systemd/secrets.env.example)
- ✅ `/etc/systemd/system/frauddetection.service` (copy from deploy/systemd/frauddetection.service.example)

**Steps:**
```bash
# Create directory
sudo mkdir -p /etc/frauddetection

# Copy and edit secrets file
sudo cp deploy/systemd/secrets.env.example /etc/frauddetection/secrets.env
sudo nano /etc/frauddetection/secrets.env

# Edit these values:
# ConnectionStrings__ClickHouse=...
# Notifications__TeamsWebhook=...
# Notifications__TelegramBotToken=...
# Notifications__TelegramChatId=...

# Set secure permissions
sudo chmod 600 /etc/frauddetection/secrets.env
sudo chown frauddetection:frauddetection /etc/frauddetection/secrets.env

# Install service
sudo cp deploy/systemd/frauddetection.service.example /etc/systemd/system/frauddetection.service
sudo systemctl daemon-reload
sudo systemctl enable frauddetection
sudo systemctl start frauddetection

# Check status
sudo systemctl status frauddetection
sudo journalctl -u frauddetection -f
```

---

### ☸️ KUBERNETES

**Files you need:**
- ✅ `deploy/kubernetes/secrets.yaml` (copy from secrets.example.yaml)

**Steps:**
```bash
# Copy example
cp deploy/kubernetes/secrets.example.yaml deploy/kubernetes/secrets.yaml

# Edit and replace all YOUR_* placeholders
nano deploy/kubernetes/secrets.yaml

# Apply
kubectl apply -f deploy/kubernetes/secrets.yaml

# Check status
kubectl get pods
kubectl logs -f deployment/frauddetection-ml
```

---

## 📝 Configuration Values Reference

### 🔗 ClickHouse Connection String Format

```
Host=<hostname>;Port=8123;Database=<database_name>;Username=<username>;Password=<password>
```

**Examples:**
```bash
# Local ClickHouse
Host=localhost;Port=8123;Database=fraud_detection;Username=default;Password=

# Remote ClickHouse
Host=clickhouse.example.com;Port=8123;Database=fraud_detection_prod;Username=fraud_user;Password=SecureP@ssw0rd

# Docker Compose
Host=clickhouse;Port=8123;Database=fraud_detection;Username=fraud_user;Password=your_password
```

### 📧 Teams Webhook Format

```
https://outlook.office.com/webhook/abc-def-ghi@xyz/IncomingWebhook/...
```

**How to get:** See [docs/TEAMS_SETUP.md](docs/TEAMS_SETUP.md)

### 📱 Telegram Configuration

**Bot Token Format:**
```
1234567890:ABCdefGHIjklMNOpqrsTUVwxyz-1234567890
```

**Chat ID Format:**
```
-1001234567890    # Group chat (negative number)
987654321          # Personal chat (positive number)
```

**How to get:** See [docs/TELEGRAM_SETUP.md](docs/TELEGRAM_SETUP.md)

---

## ✅ Verification Checklist

After configuration, verify:

```bash
# 1. Check application starts
dotnet run
# Should see: "Fraud Detection ML Service Starting..."

# 2. Check ClickHouse connection
# Should see: "AlertHistory table initialized successfully"

# 3. Check ML training
# Should see: "Model Training Job started"
# Should see: "Retrieved X sessions for training"

# 4. Check scoring
# Should see: "Real-Time Scoring Job started"

# 5. Test notifications (manual test)
# Trigger a test alert or wait for real fraud detection
```

---

## 🔍 Troubleshooting

### ❌ "ClickHouse connection string not found"
```bash
# Solution: Set the connection string
dotnet user-secrets set "ConnectionStrings:ClickHouse" "YOUR_CONNECTION_STRING"
```

### ❌ "Failed to connect to ClickHouse"
```bash
# Check if ClickHouse is running
curl http://localhost:8123/ping

# Verify connection string
dotnet user-secrets list | grep ClickHouse
```

### ❌ Notifications not working
```bash
# Check if webhook URLs are set
dotnet user-secrets list | grep Notifications

# Test Teams webhook manually
curl -H 'Content-Type: application/json' -d '{"text":"Test"}' YOUR_WEBHOOK_URL

# Test Telegram bot manually
curl "https://api.telegram.org/botYOUR_BOT_TOKEN/sendMessage?chat_id=YOUR_CHAT_ID&text=Test"
```

### ❌ User secrets not found
```bash
# Initialize user secrets
dotnet user-secrets init

# Verify UserSecretsId in .csproj
cat Beepul.Afs.FraudDetection.ML.Host.csproj | grep UserSecretsId
```

---

## 📚 Full Documentation

For complete details, see:
- 📖 [Complete Configuration Guide](docs/CONFIGURATION_GUIDE.md)
- 📖 [Telegram Setup Guide](docs/TELEGRAM_SETUP.md)
- 📖 [Teams Setup Guide](docs/TEAMS_SETUP.md)

---

## 🎯 Minimum Required Configuration

**To run the application, you MUST configure:**

1. **ClickHouse Connection** ✅ REQUIRED
   ```bash
   dotnet user-secrets set "ConnectionStrings:ClickHouse" "YOUR_CONNECTION"
   ```

2. **At least ONE notification method** (Teams OR Telegram) ⚠️ HIGHLY RECOMMENDED
   ```bash
   # Option A: Teams
   dotnet user-secrets set "Notifications:TeamsWebhook" "YOUR_WEBHOOK"

   # Option B: Telegram
   dotnet user-secrets set "Notifications:TelegramBotToken" "YOUR_TOKEN"
   dotnet user-secrets set "Notifications:TelegramChatId" "YOUR_CHAT_ID"
   ```

**Everything else has sensible defaults!** 🎉
