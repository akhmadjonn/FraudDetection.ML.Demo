namespace Beepul.Afs.FraudDetection.ML.Host.Configuration;

/// <summary>
/// API Keys configuration settings
/// </summary>
public class ApiKeysSettings
{
    /// <summary>
    /// List of valid API keys
    /// </summary>
    public string[] ValidKeys { get; set; } = Array.Empty<string>();
}
