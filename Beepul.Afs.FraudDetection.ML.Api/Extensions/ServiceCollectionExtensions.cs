using Beepul.Afs.FraudDetection.ML.Api.Services;
using Beepul.Afs.FraudDetection.ML.Api.BackgroundJobs;
using Beepul.Afs.FraudDetection.ML.Api.Configuration;

namespace Beepul.Afs.FraudDetection.ML.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all core fraud detection services (shared between Web API and Background modes)
    /// </summary>
    public static IServiceCollection AddFraudDetectionServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure strongly-typed settings
        services.Configure<MlSettings>(configuration.GetSection("ML"));
        services.Configure<ConnectionStringsSettings>(configuration.GetSection("ConnectionStrings"));
        services.Configure<AlertsSettings>(configuration.GetSection("Alerts"));
        services.Configure<PartnersSettings>(configuration.GetSection("Partners"));

        // Database Service
        services.AddSingleton<ClickHouseService>();

        // ML Services
        services.AddSingleton<FeatureEngineeringService>();
        services.AddSingleton<IsolationForestService>();
        services.AddSingleton<ClusteringService>();

        // Analysis Services
        services.AddSingleton<AnomalyAnalysisService>();
        services.AddSingleton<IFraudAnalysisService, FraudAnalysisService>();

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
