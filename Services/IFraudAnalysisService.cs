using Beepul.Afs.FraudDetection.ML.Api.Models;

namespace Beepul.Afs.FraudDetection.ML.Api.Services;

/// <summary>
/// Service interface for fraud detection and analysis operations
/// </summary>
public interface IFraudAnalysisService
{
    /// <summary>
    /// Analyzes a session for fraud in real-time
    /// </summary>
    /// <param name="sessionId">The session ID to analyze</param>
    /// <returns>Fraud analysis result with risk assessment and alert information</returns>
    Task<FraudAnalysisResult> AnalyzeSessionAsync(string sessionId);

    /// <summary>
    /// Gets existing fraud analysis result for a session
    /// </summary>
    /// <param name="sessionId">The session ID to retrieve</param>
    /// <returns>Fraud analysis result or null if not found</returns>
    Task<FraudAnalysisResult?> GetSessionAnalysisAsync(string sessionId);

    /// <summary>
    /// Gets fraud history for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>List of fraud analysis results for the user</returns>
    Task<List<FraudAnalysisResult>> GetUserHistoryAsync(string userId, int limit);

    /// <summary>
    /// Gets fraud history for a device
    /// </summary>
    /// <param name="deviceKey">The device key</param>
    /// <param name="limit">Maximum number of records to return</param>
    /// <returns>List of fraud analysis results for the device</returns>
    Task<List<FraudAnalysisResult>> GetDeviceHistoryAsync(string deviceKey, int limit);

    /// <summary>
    /// Checks if the fraud detection models are ready
    /// </summary>
    /// <returns>True if models are loaded and ready, false otherwise</returns>
    bool AreModelsReady();
}

/// <summary>
/// Exception thrown when fraud analysis fails
/// </summary>
public class FraudAnalysisException : Exception
{
    public FraudAnalysisException(string message) : base(message) { }
    public FraudAnalysisException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when session is not found
/// </summary>
public class SessionNotFoundException : Exception
{
    public SessionNotFoundException(string sessionId)
        : base($"Session '{sessionId}' not found in database")
    {
        SessionId = sessionId;
    }

    public string SessionId { get; }
}

/// <summary>
/// Exception thrown when ML models are not ready
/// </summary>
public class ModelsNotReadyException : Exception
{
    public ModelsNotReadyException()
        : base("ML models are not yet loaded. Please try again in a few minutes.")
    { }
}
