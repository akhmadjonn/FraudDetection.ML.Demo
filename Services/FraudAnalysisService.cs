using Beepul.Afs.FraudDetection.ML.Host.Models;
using Microsoft.Extensions.Logging;

namespace Beepul.Afs.FraudDetection.ML.Host.Services;

/// <summary>
/// Service for orchestrating fraud detection and analysis operations
/// </summary>
public class FraudAnalysisService : IFraudAnalysisService
{
    private readonly ClickHouseService _clickHouse;
    private readonly FeatureEngineeringService _featureService;
    private readonly IsolationForestService _isolationForest;
    private readonly ClusteringService _clustering;
    private readonly AnomalyAnalysisService _analysisService;
    private readonly NotificationService _notification;
    private readonly HybridAlertService _hybridAlert;
    private readonly ILogger<FraudAnalysisService> _logger;

    public FraudAnalysisService(
        ClickHouseService clickHouse,
        FeatureEngineeringService featureService,
        IsolationForestService isolationForest,
        ClusteringService clustering,
        AnomalyAnalysisService analysisService,
        NotificationService notification,
        HybridAlertService hybridAlert,
        ILogger<FraudAnalysisService> logger)
    {
        _clickHouse = clickHouse;
        _featureService = featureService;
        _isolationForest = isolationForest;
        _clustering = clustering;
        _analysisService = analysisService;
        _notification = notification;
        _hybridAlert = hybridAlert;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool AreModelsReady()
    {
        return _isolationForest.IsModelLoaded && _clustering.IsModelLoaded;
    }

    /// <inheritdoc />
    public async Task<FraudAnalysisResult> AnalyzeSessionAsync(string sessionId)
    {
        _logger.LogInformation("Fraud analysis requested for session: {SessionId}", sessionId);

        // Check if models are loaded
        if (!AreModelsReady())
        {
            _logger.LogWarning("ML models not loaded yet. Session: {SessionId}", sessionId);
            throw new ModelsNotReadyException();
        }

        // Get session data from ClickHouse
        var sessions = await _clickHouse.GetSessionsByIdAsync(sessionId);
        var session = sessions.FirstOrDefault();

        if (session == null)
        {
            throw new SessionNotFoundException(sessionId);
        }

        // Extract features
        var features = await _featureService.ExtractFeaturesAsync(session);
        if (features == null)
        {
            throw new FraudAnalysisException("Failed to extract features from session data");
        }

        // Get predictions from ML models
        var anomalyPrediction = _isolationForest.Predict(features);
        var clusterPrediction = _clustering.Predict(features);

        // Analyze results
        var analysisResult = _analysisService.AnalyzeSession(
            features,
            anomalyPrediction,
            clusterPrediction);

        // Save analysis result to database
        await _clickHouse.SaveAnalysisResultAsync(analysisResult);

        // Check if alert should be sent
        bool alertSent = false;
        List<string> fraudTypesAlerted = new();

        if (analysisResult.RiskLevel is "CRITICAL" or "HIGH")
        {
            var decision = await _hybridAlert.ShouldSendAlertAsync(analysisResult);

            if (decision.ShouldSend)
            {
                await _notification.SendAlertAsync(analysisResult, decision.FraudTypesToAlert);
                alertSent = true;
                fraudTypesAlerted = decision.FraudTypesToAlert;

                _logger.LogWarning(
                    "🚨 {RiskLevel} alert sent! Session: {SessionId}, Score: {Score:F2}, Fraud Types: {Types}",
                    analysisResult.RiskLevel,
                    analysisResult.SessionId,
                    analysisResult.AnomalyScore,
                    string.Join(", ", fraudTypesAlerted));
            }
            else
            {
                _logger.LogDebug(
                    "Alert throttled for session {SessionId}. Reason: {Reason}",
                    analysisResult.SessionId,
                    decision.Reason);
            }
        }

        // Set alert information
        analysisResult.AlertSent = alertSent;
        analysisResult.FraudTypesAlerted = fraudTypesAlerted;

        _logger.LogInformation(
            "Session analyzed: {SessionId}, Risk: {RiskLevel}, Score: {Score:F2}, Alert: {Alert}",
            analysisResult.SessionId,
            analysisResult.RiskLevel,
            analysisResult.AnomalyScore,
            alertSent);

        return analysisResult;
    }

    /// <inheritdoc />
    public async Task<FraudAnalysisResult?> GetSessionAnalysisAsync(string sessionId)
    {
        var results = await _clickHouse.GetAnalysisResultBySessionIdAsync(sessionId);
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<List<FraudAnalysisResult>> GetUserHistoryAsync(string userId, int limit)
    {
        return await _clickHouse.GetUserFraudHistoryAsync(userId, limit);
    }

    /// <inheritdoc />
    public async Task<List<FraudAnalysisResult>> GetDeviceHistoryAsync(string deviceKey, int limit)
    {
        return await _clickHouse.GetDeviceFraudHistoryAsync(deviceKey, limit);
    }
}
