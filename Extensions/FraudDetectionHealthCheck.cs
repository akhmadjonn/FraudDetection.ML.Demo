using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Beepul.Afs.FraudDetection.ML.Api.Services;

namespace Beepul.Afs.FraudDetection.ML.Api.Extensions;

public class FraudDetectionHealthCheck : IHealthCheck
{
    private readonly IFraudAnalysisService _fraudAnalysisService;
    private readonly ILogger<FraudDetectionHealthCheck> _logger;

    public FraudDetectionHealthCheck(
        IFraudAnalysisService fraudAnalysisService,
        ILogger<FraudDetectionHealthCheck> logger)
    {
        _fraudAnalysisService = fraudAnalysisService;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if ML models are loaded
            var isHealthy = _fraudAnalysisService.AreModelsReady();

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
