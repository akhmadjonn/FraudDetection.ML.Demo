# Fraud Detection Rules - Complete Explanation

## 🎯 Your Question: Do We Check Only These Rules?

**Short Answer:** NO! The system detects **ALL fraud patterns ALL the time** (31+ distinct patterns), categorizes them into **14 fraud types**, and the configuration controls **WHEN and HOW alerts are sent**.

---

## 📊 How Fraud Detection Works

### Two Separate Processes:

```
┌─────────────────────────────────────────────────────────────┐
│  STEP 1: FRAUD DETECTION (Always Running)                  │
│  ✓ Analyzes EVERY session                                   │
│  ✓ Detects ALL 9 fraud types                                │
│  ✓ Saves results to FraudAnalysisResults table              │
│  ✓ NOT controlled by Alerts configuration                   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  STEP 2: ALERT DECISION (Configurable)                     │
│  ✓ Checks which fraud types should trigger alerts          │
│  ✓ Applies throttling windows per fraud type               │
│  ✓ IS controlled by Alerts configuration                    │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔍 All 14 Fraud Types Detected

### **Group A: Main Fraud Types** (Detected from Boolean Flags)

These are detected by analyzing session behavior and flagged as boolean properties in `FraudAnalysisResult`:

| # | Fraud Type | Detection Source | Code Location |
|---|------------|------------------|---------------|
| 1 | **MultiAccounting** | `result.IsMultiAccounting` | Models/Predictions.cs:40 |
| 2 | **MultiDevicing** | `result.IsMultiDevicing` | Models/Predictions.cs:41 |
| 3 | **AccountTakeover** | `result.IsAccountTakeover` | Models/Predictions.cs:42 |
| 4 | **ImpossibleTravel** | `result.IsImpossibleTravel` | Models/Predictions.cs:43 |

**Detection Logic:** These flags are set by the fraud detection engine based on:
- Multi-Accounting: Multiple users using the same device
- Multi-Devicing: Same user across multiple devices
- Account Takeover: Suspicious device/location changes for a user
- Impossible Travel: User appears in geographically distant locations within impossible timeframe

### **Group B: Pattern-Based Fraud Types** (Detected from SuspiciousReasons)

These are detected by pattern matching in the `SuspiciousReasons` text array:

| # | Fraud Type | Detection Pattern | Code Location |
|---|------------|-------------------|---------------|
| 5 | **OtpBruteforce** | Text contains "OTP" + ("failure" OR "attempts" OR "success rate") | HybridAlertService.cs:223-232 |
| 6 | **DeviceSpoofing** | Text contains "Rooted" OR "Emulator" OR "Mock" OR "Cloned app" | HybridAlertService.cs:234-243 |
| 7 | **VpnUsage** | Text contains "VPN" OR "proxy" | HybridAlertService.cs:245-251 |
| 8 | **UnusualTiming** | Text contains "night" OR "2-5 AM" OR "unusual" | HybridAlertService.cs:253-261 |
| 9 | **NewDeviceFraud** | Text contains "Very new device" OR "new device" | HybridAlertService.cs:263-269 |
| 10 | **RapidCardAddition** | Text contains "Card added within 30 minutes" OR "Card added within" | HybridAlertService.cs:271-277 |
| 11 | **CardTestingFraud** | Text contains "Multiple cards added in session" | HybridAlertService.cs:279-286 |
| 12 | **AutomatedBotActivity** | Text contains "Very short session" with sensitive actions | HybridAlertService.cs:288-295 |
| 13 | **SimSwapFraud** | Text contains "carrier changes" OR "Multiple carrier" | HybridAlertService.cs:297-303 |
| 14 | **GeneralSuspicious** | High risk but no specific type detected | HybridAlertService.cs:306-310 |

**Detection Logic:** The system analyzes session data and populates `SuspiciousReasons` with text descriptions. These patterns are then extracted from the text. **ALL 31+ fraud patterns** detected by the ML model are now properly categorized into these 14 types!

---

## ⚙️ Configuration Purpose

The `Alerts` configuration **DOES NOT** control which fraud types are **detected**. It controls:

### 1. **Whether Alerts Are Sent** (`Enabled`)

```json
"MultiAccounting": {
  "Enabled": false  // ← Fraud is STILL detected, but NO alerts sent
}
```

**What happens:**
- ✅ Fraud detection runs normally
- ✅ Results saved to `FraudAnalysisResults` table
- ❌ No alerts sent to Teams/Telegram
- ❌ No throttling tracked (since no alerts)

### 2. **How Often Alerts Are Sent** (`ThrottleWindowMinutes`)

```json
"AccountTakeover": {
  "Enabled": true,
  "ThrottleWindowMinutes": 30  // ← After first alert, wait 30 min before next
}
```

**What happens:**
- First detection → Alert sent immediately
- Same fraud type within 30 minutes → Throttled (no alert)
- After 30 minutes → Alert sent again if detected

### 3. **Priority Labeling** (`Priority`)

```json
"AccountTakeover": {
  "Priority": "CRITICAL"  // ← Used for logging/future prioritization
}
```

**Currently:** This is informational only. Future use: priority queuing, different notification channels, etc.

---

## 🔄 Complete Flow Example

Let's trace a real session:

### **Session Analysis (STEP 1 - Always Runs)**

```
SessionId: sess_12345
User: +998901234567
Device: device_abc123

→ Fraud Detection Engine Analyzes:
  ├─ Device history
  ├─ User behavior
  ├─ OTP attempts
  ├─ Device properties
  └─ Location/timing patterns

→ Detection Results:
  ├─ IsMultiAccounting = true  ✓ (8 users on device)
  ├─ IsMultiDevicing = false
  ├─ IsAccountTakeover = false
  ├─ IsImpossibleTravel = false
  └─ SuspiciousReasons = ["Rooted device detected", "VPN detected", "Activity at 3 AM"]

→ Saved to FraudAnalysisResults table
```

### **Alert Decision (STEP 2 - Configuration Applied)**

```
→ HybridAlertService.DetectAllFraudTypes(result):

  FROM Boolean Flags:
  ✓ MultiAccounting (IsMultiAccounting = true)

  FROM SuspiciousReasons Text:
  ✓ DeviceSpoofing ("Rooted device detected")
  ✓ VpnUsage ("VPN detected")
  ✓ UnusualTiming ("Activity at 3 AM")

  Total Detected: 4 fraud types

→ Check Configuration:

  MultiAccounting:
    ├─ Enabled: true ✓
    ├─ ThrottleWindow: 60 min
    ├─ Last Alert: None
    └─ DECISION: SEND ALERT ✓

  DeviceSpoofing:
    ├─ Enabled: true ✓
    ├─ ThrottleWindow: 120 min
    ├─ Last Alert: None
    └─ DECISION: SEND ALERT ✓

  VpnUsage:
    ├─ Enabled: true ✓
    ├─ ThrottleWindow: 120 min
    ├─ Last Alert: None
    └─ DECISION: SEND ALERT ✓

  UnusualTiming:
    ├─ Enabled: true ✓
    ├─ ThrottleWindow: 180 min
    ├─ Last Alert: None
    └─ DECISION: SEND ALERT ✓

→ Final Decision:
  ShouldSend: true
  FraudTypesToAlert: [MultiAccounting, DeviceSpoofing, VpnUsage, UnusualTiming]

→ Alert Sent to Teams/Telegram:
  "🆕 NEW FRAUD PATTERNS: Multi-Accounting, Device Spoofing, VPN Usage, Unusual Timing"
```

---

## 🤔 Common Questions

### Q1: "If I set `Enabled: false` for a fraud type, is it still detected?"

**A:** YES! It's always detected and saved to the database. You just won't receive alerts for it.

**Example:**
```json
"VpnUsage": {
  "Enabled": false
}
```

**Result:**
- ✅ VPN usage is detected
- ✅ Saved in `FraudAnalysisResults.SuspiciousReasons`
- ✅ Visible in database queries
- ❌ NO alert sent

**Use Case:** You might disable `VpnUsage` alerts if many legitimate users use VPNs, but you still want to track it in the database for analytics.

---

### Q2: "The system now covers ALL patterns!"

**A:** Great news! We've expanded from 9 to **14 fraud types** to ensure ALL 31+ patterns are covered:

**New fraud types added:**
- **NewDeviceFraud** - Detects very new devices (< 1 day old)
- **RapidCardAddition** - Detects cards added within 30 minutes of installation
- **CardTestingFraud** - Detects multiple cards being added in a single session
- **AutomatedBotActivity** - Detects very short sessions with sensitive actions
- **SimSwapFraud** - Detects multiple carrier changes (SIM swap attacks)

**Enhanced existing types:**
- **DeviceSpoofing** - Now includes cloned app detection
- **OtpBruteforce** - Now includes low OTP success rate detection

### Q2b: "How do I add a NEW fraud type?" (For future needs)

**A:** If you need to add additional fraud types, modify the detection logic in TWO places:

#### Option 1: Add a Boolean Flag (Recommended for major fraud types)

1. **Add flag to model** (Models/Predictions.cs):
```csharp
public class FraudAnalysisResult
{
    // Existing flags...
    public bool IsNewFraudType { get; set; }  // ← Add new flag
}
```

2. **Add detection logic** (wherever fraud detection happens):
```csharp
result.IsNewFraudType = /* your detection logic */;
```

3. **Add to HybridAlertService** (Services/HybridAlertService.cs:201-265):
```csharp
private List<string> DetectAllFraudTypes(FraudAnalysisResult result)
{
    var fraudTypes = new List<string>();

    // Add detection
    if (result.IsNewFraudType)
        fraudTypes.Add("NewFraudType");

    // ... rest of code
}
```

4. **Add configuration** (appsettings.json):
```json
"Alerts": {
  "FraudTypeSettings": {
    "NewFraudType": {
      "Enabled": true,
      "ThrottleWindowMinutes": 60,
      "Priority": "HIGH"
    }
  }
}
```

5. **Update hardcoded list** (Services/HybridAlertService.cs:301-305):
```csharp
foreach (var fraudType in new[]
{
    "MultiAccounting", "MultiDevicing", "AccountTakeover", "ImpossibleTravel",
    "OtpBruteforce", "DeviceSpoofing", "VpnUsage", "UnusualTiming", "GeneralSuspicious",
    "NewFraudType"  // ← Add here
})
```

#### Option 2: Add a Pattern-Based Type (Simpler for text-based detection)

1. **Ensure your detection adds text to SuspiciousReasons**:
```csharp
result.SuspiciousReasons.Add("Suspicious pattern detected: reason here");
```

2. **Add pattern matching** (Services/HybridAlertService.cs:218-256):
```csharp
// Inside DetectAllFraudTypes method
if (reason.Contains("YourPattern", StringComparison.OrdinalIgnoreCase))
{
    if (!fraudTypes.Contains("YourNewFraudType"))
        fraudTypes.Add("YourNewFraudType");
}
```

3. **Add configuration** (same as above).

4. **Update hardcoded list** (same as above).

---

### Q3: "Why do some fraud types have different throttle windows?"

**A:** Different fraud types have different severity and frequency expectations:

| Severity | Window | Reason |
|----------|--------|--------|
| **CRITICAL** (30 min) | Short | Account takeover is urgent - need immediate alerts |
| **HIGH** (60 min) | Medium | Multi-accounting is serious but not immediately critical |
| **MEDIUM** (45-120 min) | Medium-Long | OTP bruteforce, device spoofing - balance alert fatigue |
| **LOW** (120-180 min) | Long | VPN usage, unusual timing - reduce noise |

**Customization:** Adjust based on your fraud patterns:
- High fraud volume → Increase windows to reduce alert fatigue
- Critical fraud type → Decrease window to ensure rapid response

---

### Q4: "Can I have different throttle windows for the same fraud type on different devices?"

**A:** Not directly with current configuration. Throttle windows are per fraud type globally.

**Workaround:** Create multiple fraud types:
```json
"AccountTakeover": {
  "ThrottleWindowMinutes": 30  // Standard
},
"AccountTakeoverHighRisk": {
  "ThrottleWindowMinutes": 15  // Shorter for high-risk users
}
```

Then modify detection logic to choose which type to use based on device/user risk profile.

---

### Q5: "How do I know which fraud types were detected but throttled?"

**A:** Check the `AlertDecision` object:

```csharp
var decision = await _hybridAlertService.ShouldSendAlertAsync(result);

Console.WriteLine($"All detected: {string.Join(", ", decision.AllDetectedFraudTypes)}");
Console.WriteLine($"Alerted: {string.Join(", ", decision.FraudTypesToAlert)}");
Console.WriteLine($"Throttled: {string.Join(", ", decision.ThrottledFraudTypes)}");
```

**In Logs:**
```
DEBUG: ⏸️ Throttling MultiAccounting for device abc123. Last alert: 25.3 min ago (window: 60 min)
```

**In Database:**
```sql
-- All detections (including throttled)
SELECT * FROM FraudAnalysisResults WHERE RiskLevel IN ('HIGH', 'CRITICAL');

-- Only sent alerts
SELECT * FROM AlertHistory;
```

---

## 📊 Database Tables

### **FraudAnalysisResults** (ALL Detections)

```sql
-- Contains EVERY session analysis, regardless of alerts
SELECT
    SessionId,
    RiskLevel,
    IsMultiAccounting,
    IsAccountTakeover,
    SuspiciousReasons,
    AnalyzedAt
FROM FraudAnalysisResults
WHERE RiskLevel IN ('HIGH', 'CRITICAL')
ORDER BY AnalyzedAt DESC;
```

**Purpose:** Complete audit trail of all fraud detections.

### **AlertHistory** (Only Sent Alerts)

```sql
-- Contains only alerts that were actually sent
SELECT
    AlertId,
    SessionId,
    DeviceKey,
    FraudTypes,  -- Array: which types triggered this alert
    SentAt
FROM AlertHistory
ORDER BY SentAt DESC;
```

**Purpose:** Track alert notifications and throttling.

**Relationship:**
```
FraudAnalysisResults (Many) → AlertHistory (One or None)
    One analysis may generate 0 or 1 alert (due to throttling)
    One alert references 1+ fraud types
```

---

## 🎯 Best Practices

### 1. **Start Conservative**
```json
// Initial deployment - catch everything
"Alerts": {
  "FraudTypeSettings": {
    "MultiAccounting": {
      "Enabled": true,
      "ThrottleWindowMinutes": 60  // Not too short, not too long
    }
  }
}
```

### 2. **Monitor First Week**
```sql
-- Check alert volume per fraud type
SELECT
    arrayJoin(FraudTypes) as FraudType,
    count() as AlertCount
FROM AlertHistory
WHERE SentAt > now() - INTERVAL 7 DAY
GROUP BY FraudType
ORDER BY AlertCount DESC;
```

### 3. **Adjust Based on Data**
```
If MultiAccounting alerts = 500/week:
  → Too noisy? Increase ThrottleWindowMinutes to 120
  → Just right? Keep at 60
  → Too few? Decrease to 30

If AccountTakeover alerts = 2/week:
  → Maybe decrease ThrottleWindowMinutes to 15 for faster response
```

### 4. **Disable Noisy Patterns**
```json
// If VPN usage generates 1000s of false positives
"VpnUsage": {
  "Enabled": false  // Stop alerts, but still track in database
}
```

### 5. **Enable Database Persistence in Production**
```json
"Alerts": {
  "EnableDatabasePersistence": true  // Survives restarts, provides audit trail
}
```

---

## 🔧 Debugging

### Check What's Being Detected:
```csharp
// Add logging in RealTimeScoringJob.cs
_logger.LogInformation(
    "Session {SessionId}: Detected fraud types: {Types}, Risk: {Risk}",
    result.SessionId,
    string.Join(", ", DetectAllFraudTypes(result)),
    result.RiskLevel
);
```

### Check Configuration Loading:
```csharp
// Add logging in HybridAlertService constructor
foreach (var (type, config) in _fraudTypeConfigs)
{
    _logger.LogInformation(
        "Loaded config for {Type}: Enabled={Enabled}, Window={Window}min, Priority={Priority}",
        type, config.Enabled, config.ThrottleWindow.TotalMinutes, config.Priority
    );
}
```

### Test Throttling:
```csharp
// Clear throttle for testing
_hybridAlertService.ClearThrottle("device-abc123");

// Check throttle state
var stats = _hybridAlertService.GetStatistics();
_logger.LogInformation("Throttle entries: {Count}, By type: {@Types}",
    stats.TotalEntries, stats.EntriesByFraudType);
```

---

## 📚 Summary

| Aspect | How It Works |
|--------|--------------|
| **Detection** | ALL 31+ fraud patterns detected ALWAYS for EVERY session |
| **Fraud Types** | 14 fraud types (expanded from 9 to cover all patterns) |
| **Configuration** | Controls which types trigger alerts and how often |
| **Storage** | All detections → FraudAnalysisResults, Alerts → AlertHistory |
| **Main Types** | 4 types from boolean flags (Multi-Accounting, etc.) |
| **Pattern Types** | 10 types from text analysis (OTP Bruteforce, Device Spoofing, etc.) |
| **Coverage** | 100% - No fraud patterns are lost anymore! |
| **Enabled: false** | Still detects, just doesn't alert |
| **ThrottleWindow** | Time between repeated alerts for same fraud type |
| **New Types Added** | NewDeviceFraud, RapidCardAddition, CardTestingFraud, AutomatedBotActivity, SimSwapFraud |

---

## 🎓 Key Takeaway

**Your Alerts configuration is NOT a "which rules to check" setting.**

It's a **"which detections should page me and how often"** setting.

Think of it like:
- **Fraud Detection = Security Camera** (always recording everything - 31+ patterns)
- **Fraud Types = Video Categories** (14 types organizing the patterns)
- **Alerts Configuration = Alarm System** (when to notify you)

You can turn off the alarm for certain categories, but the camera keeps recording everything!

**NEW:** We've expanded from 9 to 14 fraud types to ensure **100% coverage** of all detected patterns. No fraud is lost anymore!

---

## 📞 Questions?

If you need to:
- Add a new fraud type → See Q2 above
- Adjust alert frequency → Modify `ThrottleWindowMinutes`
- Stop alerts for a type → Set `Enabled: false`
- See all detections → Query `FraudAnalysisResults` table
- See sent alerts → Query `AlertHistory` table
- Debug throttling → Use `GetStatistics()` and `GetActiveThrottles()`
