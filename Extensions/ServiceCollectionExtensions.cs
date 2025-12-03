using Beepul.Afs.FraudDetection.ML.Host.Services;
using Beepul.Afs.FraudDetection.ML.Host.BackgroundJobs;

namespace Beepul.Afs.FraudDetection.ML.Host.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all core fraud detection services (shared between Web API and Background modes)
    /// </summary>
    public static IServiceCollection AddFraudDetectionServices(this IServiceCollection services)
    {
        // Database Service
        services.AddSingleton<ClickHouseService>();

        // ML Services
        services.AddSingleton<FeatureEngineeringService>();
        services.AddSingleton<IsolationForestService>();
        services.AddSingleton<ClusteringService>();

        // Analysis Services
        services.AddSingleton<AnomalyAnalysisService>();

        // Notification Services
        services.AddSingleton<NotificationService>();
        services.AddSingleton<AlertHistoryService>();
        services.AddSingleton<HybridAlertService>();

        // HTTP Client
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Registers background jobs (only for Background mode)
    /// </summary>
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddHostedService<ModelTrainingJob>();
        services.AddHostedService<RealTimeScoringJob>();
        services.AddHostedService<DailyAnalysisJob>();

        return services;
    }

    /// <summary>
    /// Registers Web API services (only for Web API mode)
    /// </summary>
    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        // Controllers
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null; // PascalCase
                options.JsonSerializerOptions.WriteIndented = true;
            });

        // API Explorer and Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Fraud Detection ML API",
                Version = "v1",
                Description = "Real-time fraud detection API using Machine Learning"
            });

            // Add API Key authentication to Swagger
            c.AddSecurityDefinition("ApiKey", new()
            {
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Name = "X-API-Key",
                Description = "API Key authentication"
            });

            c.AddSecurityRequirement(new()
            {
                {
                    new()
                    {
                        Reference = new()
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Health Checks
        services.AddHealthChecks()
            .AddCheck<FraudDetectionHealthCheck>("fraud_detection");

        // CORS (if needed for cross-origin requests)
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }
}
