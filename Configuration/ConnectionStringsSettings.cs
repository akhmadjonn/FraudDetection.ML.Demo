namespace Beepul.Afs.FraudDetection.ML.Host.Configuration;

/// <summary>
/// Connection strings configuration
/// </summary>
public class ConnectionStringsSettings
{
    /// <summary>
    /// ClickHouse database connection string
    /// </summary>
    public string ClickHouse { get; set; } = string.Empty;
}
