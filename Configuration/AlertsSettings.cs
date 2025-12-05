namespace Beepul.Afs.FraudDetection.ML.Api.Configuration;

/// <summary>
/// Alerts configuration settings
/// </summary>
public class AlertsSettings
{
    /// <summary>
    /// Enable database persistence for alerts
    /// </summary>
    public bool EnableDatabasePersistence { get; set; } = true;

    /// <summary>
    /// Default throttle window in minutes
    /// </summary>
    public int DefaultThrottleWindowMinutes { get; set; } = 60;

    /// <summary>
    /// Throttle window in hours (for alert history)
    /// </summary>
    public int ThrottleWindowHours { get; set; } = 24;

    /// <summary>
    /// Fraud type specific settings
    /// </summary>
    public Dictionary<string, FraudTypeConfig> FraudTypeSettings { get; set; } = new();
}

/// <summary>
/// Configuration for specific fraud types
/// </summary>
public class FraudTypeConfig
{
    /// <summary>
    /// Whether this fraud type is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Throttle window in minutes for this fraud type
    /// </summary>
    public int ThrottleWindowMinutes { get; set; }

    /// <summary>
    /// Priority level (CRITICAL, HIGH, MEDIUM, LOW)
    /// </summary>
    public string Priority { get; set; } = string.Empty;
}
