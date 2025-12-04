# 📱 Telegram Bot Setup Guide

## Step 1: Create a Telegram Bot

1. **Open Telegram** and search for `@BotFather`
2. **Start a chat** with BotFather
3. **Send command:** `/newbot`
4. **Follow the prompts:**
   - Choose a name: `Fraud Detection Alert Bot`
   - Choose a username: `fraud_detection_alerts_bot` (must end with `bot`)

5. **Save the Token** - BotFather will give you a token like:
   ```
   1234567890:ABCdefGHIjklMNOpqrsTUVwxyz
   ```
   **This is your `Notifications:TelegramBotToken`**

## Step 2: Get Your Chat ID

### Option A: Personal Chat
1. **Start a chat** with your newly created bot
2. **Send any message** to the bot (e.g., "Hello")
3. **Open this URL** in your browser (replace `<TOKEN>` with your bot token):
   ```
   https://api.telegram.org/bot<TOKEN>/getUpdates
   ```
4. **Look for the `chat` object** in the JSON response:
   ```json
   {
     "update_id": 123456789,
     "message": {
       "chat": {
         "id": 987654321,
         "first_name": "John",
         "type": "private"
       }
     }
   }
   ```
   **The `chat.id` value is your `Notifications:TelegramChatId`**

### Option B: Group Chat (Recommended for Team)
1. **Create a Telegram group**
2. **Add your bot** to the group
3. **Send a message** in the group
4. **Open this URL** in your browser:
   ```
   https://api.telegram.org/bot<TOKEN>/getUpdates
   ```
5. **Look for the group chat ID** (it will be negative, like `-1001234567890`):
   ```json
   {
     "update_id": 123456789,
     "message": {
       "chat": {
         "id": -1001234567890,
         "title": "Fraud Detection Alerts",
         "type": "group"
       }
     }
   }
   ```
   **The `chat.id` value (with the minus sign) is your `Notifications:TelegramChatId`**

## Step 3: Configure Your Application

### For Development (User Secrets):
```bash
dotnet user-secrets set "Notifications:TelegramBotToken" "1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
dotnet user-secrets set "Notifications:TelegramChatId" "-1001234567890"
```

### For Production (Environment Variables):
```bash
export Notifications__TelegramBotToken="1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"
export Notifications__TelegramChatId="-1001234567890"
```

## Step 4: Test Your Configuration

1. **Run your application**
2. **Trigger a CRITICAL alert** (or wait for one)
3. **Check your Telegram chat** - you should receive an alert message

## Telegram Features in Fraud Detection

### When Alerts are Sent:
- ✅ **CRITICAL risk alerts** are sent to Telegram
- ✅ **Daily analysis reports** are sent to Telegram
- ❌ HIGH, MEDIUM, LOW alerts are NOT sent (only Teams)

### Message Format:
```
🚨 **CRITICAL RISK ALERT**

🆕 **NEW FRAUD PATTERN DETECTED:** Multi-Accounting

**Session Details:**
• Session: `abc123...`
• User: +1234567890
• Device: `device_abc123`
• Anomaly Score: **0.95**

**🎯 Fraud Types Detected:**
• Multi-Accounting
• Account Takeover

**🔍 Suspicious Indicators:**
• High user switching rate (5 users in 24h)
• Multiple card additions
...
```

## Troubleshooting

### Bot doesn't send messages:
- ✅ Verify bot token is correct
- ✅ Verify chat ID is correct (including minus sign for groups)
- ✅ Make sure the bot is added to the group
- ✅ Check application logs for errors

### Can't get chat ID:
- ✅ Make sure you sent at least one message to the bot/group
- ✅ Wait a few seconds and try `/getUpdates` again
- ✅ Check if there are any messages in the response

### Bot not in group:
- ✅ Add the bot by searching for its username in group settings
- ✅ Make sure the bot has permission to read messages

## Security Considerations

⚠️ **Never commit your bot token or chat ID to git!**
⚠️ **Use environment variables or user secrets for all credentials**
⚠️ **Rotate your bot token if it's ever exposed**

To regenerate a bot token:
1. Message `@BotFather`
2. Send `/token`
3. Select your bot
4. Follow instructions to generate a new token
