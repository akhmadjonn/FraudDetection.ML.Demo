using Microsoft.AspNetCore.Mvc;
using Beepul.Afs.FraudDetection.ML.Api.Services;

namespace Beepul.Afs.FraudDetection.ML.Api.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly IsolationForestService _isolationForest;
    private readonly ClusteringService _clustering;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IsolationForestService isolationForest,
        ClusteringService clustering,
        ILogger<HealthController> logger)
    {
        _isolationForest = isolationForest;
        _clustering = clustering;
        _logger = logger;
    }

    /// <summary>
    /// Simple ping endpoint for Docker health checks
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { Status = "OK", Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Detailed health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        var isolationForestLoaded = _isolationForest.IsModelLoaded;
        var clusteringModelLoaded = _clustering.IsModelLoaded;
        var allModelsLoaded = isolationForestLoaded && clusteringModelLoaded;

        var health = new
        {
            Status = allModelsLoaded ? "Healthy" : "Degraded",
            Timestamp = DateTime.UtcNow,
            Models = new
            {
                IsolationForest = new
                {
                    Loaded = isolationForestLoaded,
                    Status = isolationForestLoaded ? "Ready" : "Not Loaded"
                },
                Clustering = new
                {
                    Loaded = clusteringModelLoaded,
                    Status = clusteringModelLoaded ? "Ready" : "Not Loaded"
                }
            },
            Message = allModelsLoaded
                ? "All ML models loaded and ready"
                : "ML models not yet loaded. Please wait for model training to complete."
        };

        if (!allModelsLoaded)
        {
            _logger.LogWarning("Health check: ML models not loaded yet");
            return StatusCode(503, health); // Service Unavailable
        }

        return Ok(health);
    }

    /// <summary>
    /// Get service information
    /// </summary>
    [HttpGet("info")]
    public IActionResult Info()
    {
        return Ok(new
        {
            Service = "Fraud Detection ML API",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Timestamp = DateTime.UtcNow
        });
    }
}
