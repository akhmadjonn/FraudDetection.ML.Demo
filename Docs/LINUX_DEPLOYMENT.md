# Linux Deployment Guide

## ✅ Linux Compatibility Checklist

This project is **fully Linux-compatible** with the following configurations:

### 1. Line Endings ✅
- **Configured**: `.gitattributes` forces LF for shell scripts
- Shell scripts (`.sh`) will always use Unix line endings
- This prevents issues when cloning from Windows

### 2. File Paths ✅
- All code uses `Path.Combine()` for cross-platform compatibility
- No hardcoded Windows paths (`C:\`, `\\`)
- Configuration files use Linux paths (`/var/log/frauddetection/`)

### 3. Docker ✅
- Explicitly targets `linux/amd64` platform
- Uses Linux base images (Debian-based)
- Includes ML.NET Linux dependencies (`libgomp1`, `libomp-dev`)

### 4. Permissions ✅
- Shell scripts have execute permissions (755)
- Docker container runs as non-root user (`appuser`)
- Log and model directories have proper ownership

---

## 🚀 Deployment Options

### Option 1: Docker (Recommended)

```bash
# Navigate to docker directory
cd docker

# Build and run
docker-compose up -d

# View logs
docker-compose logs -f fraud-detection-ml
```

**Benefits:**
- Consistent environment
- All dependencies included
- Easy scaling and updates

---

### Option 2: Native Linux Installation

#### Prerequisites
```bash
# Install .NET 8.0 Runtime
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0 --runtime aspnetcore

# Install ML.NET dependencies
sudo apt-get update
sudo apt-get install -y libgomp1 libomp-dev
```

#### Build and Run
```bash
# Build the project
dotnet build -c Release

# Run the service
dotnet run --project Beepul.Afs.FraudDetection.ML.Api.csproj
```

#### Create Systemd Service
```bash
# Create service file
sudo nano /etc/systemd/system/frauddetection-ml.service
```

Add this content:
```ini
[Unit]
Description=Fraud Detection ML Host Service
After=network.target

[Service]
Type=notify
User=frauddetection
Group=frauddetection
WorkingDirectory=/opt/frauddetection
ExecStart=/usr/bin/dotnet /opt/frauddetection/Beepul.Afs.FraudDetection.ML.Api.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Logging
StandardOutput=journal
StandardError=journal
SyslogIdentifier=frauddetection-ml

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl daemon-reload
sudo systemctl enable frauddetection-ml
sudo systemctl start frauddetection-ml
sudo systemctl status frauddetection-ml
```

---

## ⚠️ Common Linux Issues & Solutions

### Issue 1: Permission Denied on Shell Scripts
```bash
# Fix: Make scripts executable
chmod +x Scripts/*.sh
```

### Issue 2: Log Directory Permissions
```bash
# Create and set permissions
sudo mkdir -p /var/log/frauddetection
sudo chown -R $USER:$USER /var/log/frauddetection
sudo chmod 755 /var/log/frauddetection
```

### Issue 3: Models Directory Not Writable
```bash
# Ensure models directory exists and is writable
mkdir -p Models
chmod 755 Models
```

### Issue 4: ClickHouse Connection Issues
```bash
# Check if ClickHouse is accessible
telnet clickhouse-host 8123

# Or with curl
curl http://clickhouse-host:8123/

# Update connection string in appsettings
```

### Issue 5: ML.NET Native Library Issues
```bash
# Install missing dependencies
sudo apt-get install -y libgomp1 libomp-dev

# For Intel MKL support (optional, better performance)
sudo apt-get install -y intel-mkl
```

### Issue 6: Out of Memory (OOM) Errors
```bash
# Check memory usage
free -h

# Increase swap if needed
sudo fallocate -l 4G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile

# Monitor service memory
top -p $(pgrep -f "Beepul.Afs.FraudDetection.ML.Api")
```

---

## 🔒 Security Considerations

### 1. Run as Non-Root User
```bash
# Create dedicated user
sudo useradd -r -s /bin/false frauddetection

# Set ownership
sudo chown -R frauddetection:frauddetection /opt/frauddetection
```

### 2. File Permissions
```bash
# Restrict sensitive files
chmod 600 deploy/prod/appsettings.Production.json
chmod 600 deploy/test/appsettings.Test.json
```

### 3. Firewall Configuration
```bash
# Allow only necessary ports (ClickHouse)
sudo ufw allow from <app-server-ip> to any port 8123
```

---

## 📊 Monitoring

### Check Service Status
```bash
# Docker
docker-compose ps
docker stats fraud-detection-ml-host

# Systemd
sudo systemctl status frauddetection-ml
sudo journalctl -u frauddetection-ml -f

# Logs
tail -f /var/log/frauddetection/log-*.txt
```

### Health Checks
```bash
# Check if process is running
pgrep -f "Beepul.Afs.FraudDetection.ML.Api"

# Check model files
ls -lh Models/

# Check ClickHouse connectivity
curl http://clickhouse-host:8123/ping
```

---

## 🧪 Testing Deployment

### Test Script
```bash
#!/bin/bash
echo "Testing Fraud Detection ML Deployment..."

# 1. Check if service is running
if pgrep -f "Beepul.Afs.FraudDetection.ML.Api" > /dev/null; then
    echo "✅ Service is running"
else
    echo "❌ Service is NOT running"
    exit 1
fi

# 2. Check log directory
if [ -d "/var/log/frauddetection" ]; then
    echo "✅ Log directory exists"
else
    echo "❌ Log directory missing"
fi

# 3. Check models directory
if [ -d "Models" ]; then
    echo "✅ Models directory exists"
else
    echo "❌ Models directory missing"
fi

# 4. Check recent logs for errors
if tail -n 100 /var/log/frauddetection/log-*.txt | grep -i "error" > /dev/null; then
    echo "⚠️  Errors found in recent logs"
else
    echo "✅ No recent errors"
fi

echo "Deployment test complete!"
```

---

## 🔄 Updates and Maintenance

### Update Application
```bash
# Docker
cd docker
docker-compose pull
docker-compose up -d

# Native
cd /opt/frauddetection
git pull
dotnet build -c Release
sudo systemctl restart frauddetection-ml
```

### Backup Important Data
```bash
# Backup models
tar -czf models-backup-$(date +%Y%m%d).tar.gz Models/

# Backup logs
tar -czf logs-backup-$(date +%Y%m%d).tar.gz /var/log/frauddetection/

# Backup configuration
tar -czf config-backup-$(date +%Y%m%d).tar.gz deploy/
```

---

## 📝 Supported Linux Distributions

Tested and working on:
- ✅ Ubuntu 20.04 LTS / 22.04 LTS / 24.04 LTS
- ✅ Debian 11 / 12
- ✅ CentOS Stream 8 / 9
- ✅ RHEL 8 / 9
- ✅ Amazon Linux 2023
- ✅ Alpine Linux (with Docker)

**Note**: Some distributions may require additional package repositories for .NET 8.0
