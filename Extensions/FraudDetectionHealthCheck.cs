using Microsoft.Extensions.Diagnostics.HealthChecks;
using Beepul.Afs.FraudDetection.ML.Host.Services;

namespace Beepul.Afs.FraudDetection.ML.Host.Extensions;

public class FraudDetectionHealthCheck : IHealthCheck
{
    private readonly IsolationForestService _isolationForest;
    private readonly ClusteringService _clustering;
    private readonly ILogger<FraudDetectionHealthCheck> _logger;

    public FraudDetectionHealthCheck(
        IsolationForestService isolationForest,
        ClusteringService clustering,
        ILogger<FraudDetectionHealthCheck> logger)
    {
        _isolationForest = isolationForest;
        _clustering = clustering;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if ML models are loaded
            var isHealthy = _isolationForest.IsModelLoaded && _clustering.IsModelLoaded;

            if (isHealthy)
            {
                return Task.FromResult(HealthCheckResult.Healthy("ML models loaded and ready"));
            }

            return Task.FromResult(HealthCheckResult.Degraded("ML models not yet loaded"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Health check failed", ex));
        }
    }
}
