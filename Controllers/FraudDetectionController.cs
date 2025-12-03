using Microsoft.AspNetCore.Mvc;
using Beepul.Afs.FraudDetection.ML.Host.Services;
using Beepul.Afs.FraudDetection.ML.Host.Models;

namespace Beepul.Afs.FraudDetection.ML.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FraudDetectionController : ControllerBase
{
    private readonly ClickHouseService _clickHouse;
    private readonly FeatureEngineeringService _featureService;
    private readonly IsolationForestService _isolationForest;
    private readonly ClusteringService _clustering;
    private readonly AnomalyAnalysisService _analysisService;
    private readonly NotificationService _notification;
    private readonly HybridAlertService _hybridAlert;
    private readonly ILogger<FraudDetectionController> _logger;

    public FraudDetectionController(
        ClickHouseService clickHouse,
        FeatureEngineeringService featureService,
        IsolationForestService isolationForest,
        ClusteringService clustering,
        AnomalyAnalysisService analysisService,
        NotificationService notification,
        HybridAlertService hybridAlert,
        ILogger<FraudDetectionController> logger)
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

    /// <summary>
    /// Analyzes a session for fraud in real-time
    /// </summary>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(FraudAnalysisResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 503)]
    public async Task<IActionResult> AnalyzeSession([FromBody] AnalyzeSessionRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "Invalid Request",
                    Message = "SessionId is required"
                });
            }

            _logger.LogInformation("Fraud analysis requested for session: {SessionId}", request.SessionId);

            // Check if models are loaded
            if (!_isolationForest.IsModelLoaded || !_clustering.IsModelLoaded)
            {
                _logger.LogWarning("ML models not loaded yet. Session: {SessionId}", request.SessionId);
                return StatusCode(503, new ErrorResponse
                {
                    Error = "Service Unavailable",
                    Message = "ML models are not yet loaded. Please try again in a few minutes."
                });
            }

            // Get session data from ClickHouse
            var sessions = await _clickHouse.GetSessionsByIdAsync(request.SessionId);
            var session = sessions.FirstOrDefault();

            if (session == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "Not Found",
                    Message = $"Session '{request.SessionId}' not found in database"
                });
            }

            // Extract features
            var features = await _featureService.ExtractFeaturesAsync(session);
            if (features == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "Feature Extraction Failed",
                    Message = "Failed to extract features from session data"
                });
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

            // Build response
            var response = new FraudAnalysisResponse
            {
                SessionId = analysisResult.SessionId,
                UserId = analysisResult.UserId,
                PhoneNumber = analysisResult.PhoneNumber,
                DeviceKey = analysisResult.DeviceKey,
                AnalyzedAt = analysisResult.AnalyzedAt,
                RiskLevel = analysisResult.RiskLevel,
                AnomalyScore = analysisResult.AnomalyScore,
                IsAnomaly = analysisResult.IsAnomaly,
                ClusterId = analysisResult.ClusterId,
                IsMultiAccounting = analysisResult.IsMultiAccounting,
                IsMultiDevicing = analysisResult.IsMultiDevicing,
                IsAccountTakeover = analysisResult.IsAccountTakeover,
                IsImpossibleTravel = analysisResult.IsImpossibleTravel,
                SuspiciousReasons = analysisResult.SuspiciousReasons,
                AlertSent = alertSent,
                FraudTypesAlerted = fraudTypesAlerted
            };

            _logger.LogInformation(
                "Session analyzed: {SessionId}, Risk: {RiskLevel}, Score: {Score:F2}, Alert: {Alert}",
                response.SessionId,
                response.RiskLevel,
                response.AnomalyScore,
                alertSent);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing session {SessionId}", request.SessionId);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Internal Server Error",
                Message = "An error occurred while analyzing the session"
            });
        }
    }

    /// <summary>
    /// Gets existing fraud analysis result for a session
    /// </summary>
    [HttpGet("session/{sessionId}")]
    [ProducesResponseType(typeof(FraudAnalysisResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetSessionAnalysis(string sessionId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "Invalid Request",
                    Message = "SessionId is required"
                });
            }

            var results = await _clickHouse.GetAnalysisResultBySessionIdAsync(sessionId);
            var result = results.FirstOrDefault();

            if (result == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "Not Found",
                    Message = $"No fraud analysis found for session '{sessionId}'"
                });
            }

            var response = new FraudAnalysisResponse
            {
                SessionId = result.SessionId,
                UserId = result.UserId,
                PhoneNumber = result.PhoneNumber,
                DeviceKey = result.DeviceKey,
                AnalyzedAt = result.AnalyzedAt,
                RiskLevel = result.RiskLevel,
                AnomalyScore = result.AnomalyScore,
                IsAnomaly = result.IsAnomaly,
                ClusterId = result.ClusterId,
                IsMultiAccounting = result.IsMultiAccounting,
                IsMultiDevicing = result.IsMultiDevicing,
                IsAccountTakeover = result.IsAccountTakeover,
                IsImpossibleTravel = result.IsImpossibleTravel,
                SuspiciousReasons = result.SuspiciousReasons,
                AlertSent = false, // Historical data, no new alert sent
                FraudTypesAlerted = new()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session analysis {SessionId}", sessionId);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Internal Server Error",
                Message = "An error occurred while retrieving session analysis"
            });
        }
    }

    /// <summary>
    /// Gets fraud history for a user
    /// </summary>
    [HttpGet("user/{userId}/history")]
    [ProducesResponseType(typeof(List<FraudAnalysisResponse>), 200)]
    public async Task<IActionResult> GetUserHistory(string userId, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "Invalid Request",
                    Message = "UserId is required"
                });
            }

            var results = await _clickHouse.GetUserFraudHistoryAsync(userId, limit);

            var response = results.Select(r => new FraudAnalysisResponse
            {
                SessionId = r.SessionId,
                UserId = r.UserId,
                PhoneNumber = r.PhoneNumber,
                DeviceKey = r.DeviceKey,
                AnalyzedAt = r.AnalyzedAt,
                RiskLevel = r.RiskLevel,
                AnomalyScore = r.AnomalyScore,
                IsAnomaly = r.IsAnomaly,
                ClusterId = r.ClusterId,
                IsMultiAccounting = r.IsMultiAccounting,
                IsMultiDevicing = r.IsMultiDevicing,
                IsAccountTakeover = r.IsAccountTakeover,
                IsImpossibleTravel = r.IsImpossibleTravel,
                SuspiciousReasons = r.SuspiciousReasons,
                AlertSent = false,
                FraudTypesAlerted = new()
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user history {UserId}", userId);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Internal Server Error",
                Message = "An error occurred while retrieving user history"
            });
        }
    }

    /// <summary>
    /// Gets fraud history for a device
    /// </summary>
    [HttpGet("device/{deviceKey}/history")]
    [ProducesResponseType(typeof(List<FraudAnalysisResponse>), 200)]
    public async Task<IActionResult> GetDeviceHistory(string deviceKey, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deviceKey))
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "Invalid Request",
                    Message = "DeviceKey is required"
                });
            }

            var results = await _clickHouse.GetDeviceFraudHistoryAsync(deviceKey, limit);

            var response = results.Select(r => new FraudAnalysisResponse
            {
                SessionId = r.SessionId,
                UserId = r.UserId,
                PhoneNumber = r.PhoneNumber,
                DeviceKey = r.DeviceKey,
                AnalyzedAt = r.AnalyzedAt,
                RiskLevel = r.RiskLevel,
                AnomalyScore = r.AnomalyScore,
                IsAnomaly = r.IsAnomaly,
                ClusterId = r.ClusterId,
                IsMultiAccounting = r.IsMultiAccounting,
                IsMultiDevicing = r.IsMultiDevicing,
                IsAccountTakeover = r.IsAccountTakeover,
                IsImpossibleTravel = r.IsImpossibleTravel,
                SuspiciousReasons = r.SuspiciousReasons,
                AlertSent = false,
                FraudTypesAlerted = new()
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device history {DeviceKey}", deviceKey);
            return StatusCode(500, new ErrorResponse
            {
                Error = "Internal Server Error",
                Message = "An error occurred while retrieving device history"
            });
        }
    }
}

// DTOs
public record AnalyzeSessionRequest
{
    public string SessionId { get; init; } = string.Empty;
}

public record FraudAnalysisResponse
{
    public string SessionId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string DeviceKey { get; init; } = string.Empty;
    public DateTime AnalyzedAt { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public float AnomalyScore { get; init; }
    public bool IsAnomaly { get; init; }
    public int ClusterId { get; init; }
    public bool IsMultiAccounting { get; init; }
    public bool IsMultiDevicing { get; init; }
    public bool IsAccountTakeover { get; init; }
    public bool IsImpossibleTravel { get; init; }
    public List<string> SuspiciousReasons { get; init; } = new();
    public bool AlertSent { get; init; }
    public List<string> FraudTypesAlerted { get; init; } = new();
}

public record ErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
