using Microsoft.AspNetCore.Mvc;
using Beepul.Afs.FraudDetection.ML.Api.Services;
using Beepul.Afs.FraudDetection.ML.Api.Models;

namespace Beepul.Afs.FraudDetection.ML.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FraudDetectionController : ControllerBase
{
    private readonly IFraudAnalysisService _fraudAnalysisService;
    private readonly ILogger<FraudDetectionController> _logger;

    public FraudDetectionController(
        IFraudAnalysisService fraudAnalysisService,
        ILogger<FraudDetectionController> logger)
    {
        _fraudAnalysisService = fraudAnalysisService;
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

            // Delegate to service
            var result = await _fraudAnalysisService.AnalyzeSessionAsync(request.SessionId);

            // Map to response DTO
            var response = MapToResponse(result);

            return Ok(response);
        }
        catch (ModelsNotReadyException ex)
        {
            _logger.LogWarning(ex, "ML models not loaded yet. Session: {SessionId}", request.SessionId);
            return StatusCode(503, new ErrorResponse
            {
                Error = "Service Unavailable",
                Message = ex.Message
            });
        }
        catch (SessionNotFoundException ex)
        {
            return NotFound(new ErrorResponse
            {
                Error = "Not Found",
                Message = ex.Message
            });
        }
        catch (FraudAnalysisException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Feature Extraction Failed",
                Message = ex.Message
            });
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

            var result = await _fraudAnalysisService.GetSessionAnalysisAsync(sessionId);

            if (result == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = "Not Found",
                    Message = $"No fraud analysis found for session '{sessionId}'"
                });
            }

            var response = MapToResponse(result);
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

            var results = await _fraudAnalysisService.GetUserHistoryAsync(userId, limit);
            var response = results.Select(MapToResponse).ToList();

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

            var results = await _fraudAnalysisService.GetDeviceHistoryAsync(deviceKey, limit);
            var response = results.Select(MapToResponse).ToList();

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

    /// <summary>
    /// Maps FraudAnalysisResult to FraudAnalysisResponse DTO
    /// </summary>
    private static FraudAnalysisResponse MapToResponse(FraudAnalysisResult result)
    {
        return new FraudAnalysisResponse
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
            AlertSent = result.AlertSent,
            FraudTypesAlerted = result.FraudTypesAlerted
        };
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
    public uint ClusterId { get; init; }
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
