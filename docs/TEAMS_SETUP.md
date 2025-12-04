# 📧 Microsoft Teams Webhook Setup Guide

## Step 1: Create an Incoming Webhook in Teams

### Method 1: Teams Desktop/Web App (Recommended)

1. **Open Microsoft Teams**
2. **Navigate to the channel** where you want to receive fraud alerts
3. **Click the three dots (•••)** next to the channel name
4. **Select "Connectors"** or "Workflows" (depending on Teams version)

### For Classic Teams (Before 2024):
5. **Search for "Incoming Webhook"**
6. **Click "Configure"**
7. **Enter a name:** `Fraud Detection Alerts`
8. **Upload an icon** (optional)
9. **Click "Create"**
10. **Copy the webhook URL** - it looks like:
    ```
    https://outlook.office.com/webhook/abc123-def456-ghi789@xyz/IncomingWebhook/abc/def123
    ```
    **This is your `Notifications:TeamsWebhook`**

### For New Teams (2024+):
5. **Select "Workflows"**
6. **Search for "Post to a channel when a webhook request is received"**
7. **Click "Add workflow"**
8. **Select the channel** and **name** it `Fraud Detection Alerts`
9. **Copy the webhook URL**

## Step 2: Configure Your Application

### For Development (User Secrets):
```bash
dotnet user-secrets set "Notifications:TeamsWebhook" "https://outlook.office.com/webhook/YOUR-WEBHOOK-URL"
```

### For Production (Environment Variables):
```bash
export Notifications__TeamsWebhook="https://outlook.office.com/webhook/YOUR-WEBHOOK-URL"
```

## Step 3: Test Your Configuration

### Quick Test (PowerShell):
```powershell
$webhook = "YOUR-WEBHOOK-URL"
$body = @{
    text = "Test message from Fraud Detection system"
} | ConvertTo-Json

Invoke-RestMethod -Uri $webhook -Method Post -Body $body -ContentType "application/json"
```

### Quick Test (curl):
```bash
curl -H 'Content-Type: application/json' \
  -d '{"text": "Test message from Fraud Detection system"}' \
  YOUR-WEBHOOK-URL
```

## Teams Features in Fraud Detection

### When Alerts are Sent:
- ✅ **ALL risk levels** (CRITICAL, HIGH, MEDIUM, LOW) are sent to Teams
- ✅ **Daily analysis reports** are sent to Teams
- ✅ **Fraud pattern summaries** are sent to Teams

### Message Format:
Teams messages use Adaptive Cards with:
- **Color coding** based on severity (Red=Critical, Orange=High, Yellow=Medium)
- **Formatted text** with markdown
- **Session details** and metrics
- **Fraud type indicators**
- **Suspicious behavior summaries**

Example:
```
⚠️ **HIGH RISK ALERT**

**Session Details:**
• Session: `abc123...`
• User: +1234567890
• Device: `device_abc123`
• Anomaly Score: **0.85**
• Cluster: 3

**🎯 Fraud Types Detected:**
• Multi-Accounting
• Multi-Devicing

**🔍 Suspicious Indicators:**
• 3 different users on device (7 days)
• 5 devices used by user (7 days)
• High card addition rate
• Multiple P2P transfers

**📊 Key Metrics:**
• Device Age: 15.3 days
• OTP Failures: 2
• Card Additions: 3
• P2P Transfers: 8
```

## Creating Multiple Webhooks

You can create different webhooks for different alert levels:

### In appsettings.json:
```json
{
  "Notifications": {
    "TeamsWebhook": "https://outlook.office.com/webhook/MAIN-WEBHOOK",
    "TeamsWebhookCritical": "https://outlook.office.com/webhook/CRITICAL-ONLY-WEBHOOK",
    "TeamsWebhookDaily": "https://outlook.office.com/webhook/REPORTS-WEBHOOK"
  }
}
```

## Troubleshooting

### Webhook not working:
- ✅ Verify the webhook URL is complete and correct
- ✅ Check if webhook was removed from Teams channel
- ✅ Ensure the channel still exists
- ✅ Check application logs for HTTP errors

### Messages not formatted correctly:
- ✅ Verify JSON payload is valid
- ✅ Check Adaptive Card schema version
- ✅ Test with simple text message first

### Webhook expired or deleted:
- ✅ Recreate the webhook in Teams
- ✅ Update the configuration with the new URL
- ✅ Restart the application

## Security Considerations

⚠️ **Never commit your webhook URL to git!**
⚠️ **Anyone with the webhook URL can post to your channel**
⚠️ **Use environment variables or user secrets for all webhook URLs**
⚠️ **Delete and recreate webhook if it's ever exposed**

## Advanced: Rate Limiting

Teams webhooks have rate limits:
- **4 requests per second** per webhook
- **Throttling** if exceeded

The application handles this with:
- Alert throttling (configured per fraud type)
- In-memory deduplication
- Database persistence to prevent duplicate alerts

## Webhook Permissions

⚠️ **Teams Channel Permissions:**
- Anyone in the channel can create/delete webhooks
- Only channel owners can manage connectors
- Webhook URLs should be treated as secrets

## Alternative: Power Automate

For more advanced scenarios, consider using Power Automate:
1. **Create a Power Automate flow**
2. **Trigger:** "When a HTTP request is received"
3. **Action:** Post to Teams with custom logic
4. **Benefits:** Conditional formatting, routing, additional processing
