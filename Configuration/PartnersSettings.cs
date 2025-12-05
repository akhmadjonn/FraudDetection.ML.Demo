namespace Beepul.Afs.FraudDetection.ML.Host.Configuration;

/// <summary>
/// Partner API key configuration
/// </summary>
public class PartnersSettings
{
    /// <summary>
    /// List of registered partners with their API keys
    /// </summary>
    public List<PartnerConfig> Partners { get; set; } = new();
}

/// <summary>
/// Individual partner configuration
/// </summary>
public class PartnerConfig
{
    /// <summary>
    /// Partner's API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Partner name/identifier
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
