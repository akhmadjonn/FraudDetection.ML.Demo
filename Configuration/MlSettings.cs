namespace Beepul.Afs.FraudDetection.ML.Api.Configuration;

/// <summary>
/// Machine Learning configuration settings
/// </summary>
public class MlSettings
{
    /// <summary>
    /// Training interval in hours
    /// </summary>
    public int TrainingIntervalHours { get; set; } = 6;

    /// <summary>
    /// Minimum sessions required for training
    /// </summary>
    public int MinSessionsForTraining { get; set; } = 1000;

    /// <summary>
    /// Scoring interval in seconds
    /// </summary>
    public int ScoringIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Batch size for scoring
    /// </summary>
    public int ScoringBatchSize { get; set; } = 100;

    /// <summary>
    /// Daily report hour (0-23)
    /// </summary>
    public int DailyReportHour { get; set; } = 8;
}
