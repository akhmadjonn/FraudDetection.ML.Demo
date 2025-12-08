namespace Beepul.Afs.FraudDetection.ML.Api.Configuration;

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
