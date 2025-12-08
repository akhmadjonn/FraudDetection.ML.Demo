using Beepul.Afs.FraudDetection.ML.Api.Services;
using Beepul.Afs.FraudDetection.ML.Api.BackgroundJobs;
using Beepul.Afs.FraudDetection.ML.Api.Extensions;
using Beepul.Afs.FraudDetection.ML.Api.Middleware;
using Serilog;

// Detect run mode from command-line arguments
var runMode = DetectRunMode(args);

var a = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
// Build unified configuration (used by both Serilog and application)
// This configuration is built ONCE and NOT reloaded during runtime
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
    .AddJsonFile("/settings.json", optional: true, reloadOnChange: false)  // Docker secrets/config
    .AddJsonFile("/run/secrets/secrets.json", optional: true, reloadOnChange: false)  // Docker secrets
    .AddEnvironmentVariables()
    .Build();

// Configure Serilog using unified configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)  // Use unified configuration
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Beepul.Afs.FraudDetection.ML.Api")
    .Enrich.WithProperty("RunMode", runMode.ToString())
    .CreateLogger();

try
{
    if (runMode == RunMode.WebApi)
    {
        await RunWebApiAsync(args, configuration);
    }
    else if (runMode == RunMode.Background)
    {
        await RunBackgroundAsync(args, configuration);
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Fraud Detection ML Service stopped");
    await Log.CloseAndFlushAsync();
}

// ==================== WEB API MODE ====================
async Task RunWebApiAsync(string[] arguments, IConfiguration config)
{
    Log.Information("═══════════════════════════════════════");
    Log.Information("Starting in WEB API Mode");
    Log.Information("═══════════════════════════════════════");

    var builder = WebApplication.CreateBuilder(arguments);

    // Use unified configuration (instead of default configuration)
    builder.Configuration.Sources.Clear();
    builder.Configuration.AddConfiguration(config);

    // Configure OpenTelemetry (includes Serilog configuration)
    builder.AddOpenTelemetryInstrumentation();

    // Register fraud detection services (shared)
    builder.Services.AddFraudDetectionServices(builder.Configuration);

    // Register Web API services
    builder.Services.AddWebApiServices();

    var app = builder.Build();

    // Create Models directory
    var modelsDir = Path.Combine(AppContext.BaseDirectory, "Models");
    Directory.CreateDirectory(modelsDir);

    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("Models Directory: {ModelsDirectory}", modelsDir);

    // Initialize services on startup
    Log.Information("Initializing services...");
    var clickHouseService = app.Services.GetRequiredService<ClickHouseService>();
    await clickHouseService.InitializeAsync();

    var alertHistoryService = app.Services.GetRequiredService<AlertHistoryService>();
    await alertHistoryService.InitializeAsync();

    var hybridAlertService = app.Services.GetRequiredService<HybridAlertService>();
    await hybridAlertService.InitializeAsync();
    Log.Information("Services initialized successfully");

    // Configure middleware pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();

    // API Key Authentication Middleware
    app.UseApiKeyAuth();

    app.UseRouting();
    app.UseCors("AllowAll");

    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("Web API is ready and listening...");
    Log.Information("Swagger UI: {SwaggerUrl}", app.Environment.IsDevelopment() ? "http://localhost:5000" : "Disabled in Production");

    await app.RunAsync();
}

// ==================== BACKGROUND MODE ====================
async Task RunBackgroundAsync(string[] arguments, IConfiguration config)
{
    Log.Information("═══════════════════════════════════════");
    Log.Information("Starting in BACKGROUND Mode");
    Log.Information("═══════════════════════════════════════");

    var builder = Host.CreateApplicationBuilder(arguments);

    // Use unified configuration (instead of default configuration)
    builder.Configuration.Sources.Clear();
    builder.Configuration.AddConfiguration(config);

    // Configure OpenTelemetry (includes Serilog configuration)
    builder.AddOpenTelemetryInstrumentation();

    // Register fraud detection services (shared)
    builder.Services.AddFraudDetectionServices(builder.Configuration);

    // Register background jobs
    builder.Services.AddBackgroundJobs();

    var host = builder.Build();

    // Get logger from DI container
    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    // Create Models directory
    var modelsDir = Path.Combine(AppContext.BaseDirectory, "Models");
    Directory.CreateDirectory(modelsDir);

    logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
    logger.LogInformation("Models Directory: {ModelsDirectory}", modelsDir);

    // Initialize ClickHouse tables
    logger.LogInformation("Initializing ClickHouse tables...");
    var clickHouseService = host.Services.GetRequiredService<ClickHouseService>();
    await clickHouseService.InitializeAsync();
    logger.LogInformation("FraudAnalysisResults table initialized successfully");

    // Initialize AlertHistory table
    logger.LogInformation("Initializing AlertHistory table...");
    var alertHistoryService = host.Services.GetRequiredService<AlertHistoryService>();
    await alertHistoryService.InitializeAsync();
    logger.LogInformation("AlertHistory table initialized successfully");

    // Initialize HybridAlertService (warm up cache from database)
    logger.LogInformation("Initializing HybridAlertService...");
    var hybridAlertService = host.Services.GetRequiredService<HybridAlertService>();
    await hybridAlertService.InitializeAsync();
    logger.LogInformation("HybridAlertService initialized successfully");

    logger.LogInformation("Background jobs are starting...");
    await host.RunAsync();
}

// ==================== RUN MODE DETECTION ====================
RunMode DetectRunMode(string[] arguments)
{
     // Check for --hangfire or --background flag
    if (arguments.Any(arg => arg.Equals("--hangfire", StringComparison.OrdinalIgnoreCase) ||
                             arg.Equals("--background", StringComparison.OrdinalIgnoreCase)))
    {
        return RunMode.Background;
    }

    // Default to Web API mode
    return RunMode.WebApi;
}

enum RunMode
{
    WebApi,
    Background
}
