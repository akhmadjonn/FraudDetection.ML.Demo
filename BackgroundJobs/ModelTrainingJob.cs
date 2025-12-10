using Beepul.Afs.FraudDetection.ML.Host.Models;
using Beepul.Afs.FraudDetection.ML.Host.Services;
using Tensorflow;

namespace Beepul.Afs.FraudDetection.ML.Host.BackgroundJobs;

public class ModelTrainingJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModelTrainingJob> _logger;
    private readonly TimeSpan _trainingInterval;
    private readonly int _minSessionsForTraining;
    private DateTime? _lastTrainingTime;

    public ModelTrainingJob(
        IServiceProvider serviceProvider,
        ILogger<ModelTrainingJob> logger,
        IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _trainingInterval = TimeSpan.FromHours(config.GetValue<int>("ML:TrainingIntervalHours", 6));
        _minSessionsForTraining = config.GetValue<int>("ML:MinSessionsForTraining", 1000);
        _lastTrainingTime = null; // Will use 7 days on first run
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Model Training Job started. Training interval: {Interval}", _trainingInterval);

        // Wait 5 minutes before first training (let data accumulate)
        //await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        // Train immediately on startup
        await TrainModelsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Waiting {Interval} until next training cycle", _trainingInterval);
                await Task.Delay(_trainingInterval, stoppingToken);

                await TrainModelsAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Model training job cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during model training");
            }
        }
    }

    private async Task TrainModelsAsync()
    {
        _logger.LogInformation("═══════════════════════════════════════");
        _logger.LogInformation("Starting model training cycle...");
        _logger.LogInformation("═══════════════════════════════════════");

        var startTime = DateTime.UtcNow;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var clickHouse = scope.ServiceProvider.GetRequiredService<ClickHouseService>();
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureEngineeringService>();
            var isolationForest = scope.ServiceProvider.GetRequiredService<IsolationForestService>();
            var clustering = scope.ServiceProvider.GetRequiredService<ClusteringService>();

            // Determine the time range for this training run
            // First run: use last 7 days
            // Subsequent runs: use NEW data since last training
            var fromDate = _lastTrainingTime ?? DateTime.UtcNow.AddDays(-7);
            var currentTrainingTime = DateTime.UtcNow;

            const int batchSize = 1000; // Process 1000 sessions at a time
            const int maxSessions = 10000; // Maximum sessions to process per training

            if (_lastTrainingTime.HasValue)
            {
                _logger.LogInformation("Fetching NEW sessions since last training at {LastTraining}",
                    _lastTrainingTime.Value);
            }
            else
            {
                _logger.LogInformation("First training run - fetching sessions from last 7 days");
            }

            _logger.LogInformation("Fetching sessions in batches of {BatchSize} (max {MaxSessions} total)",
                batchSize, maxSessions);

            var allSessions = new List<SessionRecord>();
            var offset = 0;

            // Fetch sessions in batches to avoid memory overflow
            while (allSessions.Count < maxSessions)
            {
                var batch = await clickHouse.GetRecentSessionsLightAsync(
                    fromDate,
                    limit: batchSize,
                    offset: offset);

                if (batch.Count == 0) break; // No more sessions

                allSessions.AddRange(batch);
                offset += batchSize;

                _logger.LogInformation("Fetched batch {BatchNum}: {Count} sessions (total: {Total})",
                    offset / batchSize, batch.Count, allSessions.Count);

                if (batch.Count < batchSize) break; // Last batch
            }

            var timeRange = _lastTrainingTime.HasValue
                ? $"since {_lastTrainingTime.Value}"
                : "from last 7 days";

            _logger.LogInformation("Retrieved {Count} sessions for training ({TimeRange})",
                allSessions.Count, timeRange);

            if (allSessions.Count < _minSessionsForTraining)
            {
                _logger.LogWarning(
                    "Not enough data for training. Need at least {Min} sessions, got {Actual}. " +
                    "Skipping training until more data accumulates.",
                    _minSessionsForTraining,
                    allSessions.Count);
                // Don't update _lastTrainingTime so we can include this data in the next run
                return;
            }

            // Extract features in batches to control memory usage
            _logger.LogInformation("Extracting features from sessions in batches...");
            var features = new List<FraudFeatures>();
            var failedCount = 0;
            var processedCount = 0;

            // Process features in smaller batches
            const int featureBatchSize = 500;
            for (int i = 0; i < allSessions.Count; i += featureBatchSize)
            {
                var sessionBatch = allSessions.Skip(i).Take(featureBatchSize).ToList();

                foreach (var session in sessionBatch)
                {
                    try
                    {
                        var feature = await featureService.ExtractFeaturesAsync(session);
                        if (feature != null)
                        {
                            features.Add(feature);
                        }
                        else
                        {
                            failedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract features for session {SessionId}", session.SessionId);
                        failedCount++;
                    }

                    processedCount++;
                }

                _logger.LogInformation("Feature extraction progress: {Processed}/{Total} sessions processed",
                    processedCount, allSessions.Count);
            }

            _logger.LogInformation(
                "Feature extraction complete. Success: {Success}, Failed: {Failed}",
                features.Count,
                failedCount);

            if (features.Count < 100)
            {
                _logger.LogWarning(
                    "Not enough valid features extracted. Need at least 100, got {Count}. " +
                    "Skipping training until more valid data is available.",
                    features.Count);
                // Don't update _lastTrainingTime so we can include this data in the next run
                return;
            }

            // Log feature statistics
            LogFeatureStatistics(features);

            // Train both models in parallel
            _logger.LogInformation("Training ML models in parallel...");
            await Task.WhenAll(
                isolationForest.TrainModelAsync(features),
                clustering.TrainModelAsync(features)
            );

            var duration = DateTime.UtcNow - startTime;

            // Update last training time AFTER successful training
            _lastTrainingTime = currentTrainingTime;

            _logger.LogInformation("═══════════════════════════════════════");
            _logger.LogInformation("Model training completed successfully!");
            _logger.LogInformation("Duration: {Duration}", duration);
            _logger.LogInformation("Sessions processed: {Count}", allSessions.Count);
            _logger.LogInformation("Features extracted: {Count}", features.Count);
            _logger.LogInformation("Next training will use data after: {NextFromDate}", _lastTrainingTime.Value);
            _logger.LogInformation("═══════════════════════════════════════");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during model training");
        }
    }

    private void LogFeatureStatistics(List<FraudFeatures> features)
    {
        _logger.LogInformation("Feature Statistics:");
        _logger.LogInformation("  Multi-Accounting Indicators:");
        _logger.LogInformation("    • Devices with 3+ users (24h): {Count}",
            features.Count(f => f.UniqueUserIdsOnDevice_24h >= 3));
        _logger.LogInformation("    • Devices with 5+ users (7d): {Count}",
            features.Count(f => f.UniqueUserIdsOnDevice_7d >= 5));

        _logger.LogInformation("  Multi-Devicing Indicators:");
        _logger.LogInformation("    • Users on 3+ devices (24h): {Count}",
            features.Count(f => f.DevicesPerUser_24h >= 3));
        _logger.LogInformation("    • Users on 5+ devices (7d): {Count}",
            features.Count(f => f.DevicesPerUser_7d >= 5));

        _logger.LogInformation("  Security Flags:");
        _logger.LogInformation("    • Rooted devices: {Count}",
            features.Count(f => f.IsRooted == 1));
        _logger.LogInformation("    • Emulators: {Count}",
            features.Count(f => f.IsEmulator == 1));
        _logger.LogInformation("    • VPN enabled: {Count}",
            features.Count(f => f.IsVpn == 1));

        _logger.LogInformation("  Behavioral Indicators:");
        _logger.LogInformation("    • Sessions with OTP failures: {Count}",
            features.Count(f => f.OtpFailureCount > 0));
        _logger.LogInformation("    • Sessions with card additions: {Count}",
            features.Count(f => f.CardAdditionCount > 0));
        _logger.LogInformation("    • Sessions with P2P transfers: {Count}",
            features.Count(f => f.P2PTransferCount > 0));
    }
}