namespace Beepul.Afs.FraudDetection.ML.Host.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private const string API_KEY_HEADER = "X-API-Key";

    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health check and ping endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.Contains("/health") || path.Contains("/ping") || path.Contains("/swagger"))
        {
            await _next(context);
            return;
        }

        // Check if API key is present in header
        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
        {
            _logger.LogWarning("API Key missing from request. Path: {Path}, IP: {IP}",
                context.Request.Path, context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Unauthorized",
                Message = "API Key is missing. Please provide X-API-Key header."
            });
            return;
        }

        // Get valid API keys from configuration
        var validApiKeys = _configuration.GetSection("ApiKeys:ValidKeys").Get<string[]>() ?? Array.Empty<string>();

        if (validApiKeys.Length == 0)
        {
            _logger.LogCritical("No API keys configured in appsettings. All requests will be rejected.");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Internal Server Error",
                Message = "API authentication not properly configured"
            });
            return;
        }

        // Validate API key
        var providedKey = extractedApiKey.ToString();
        if (!validApiKeys.Contains(providedKey))
        {
            _logger.LogWarning("Invalid API Key attempted. Path: {Path}, IP: {IP}, Key: {Key}",
                context.Request.Path, context.Connection.RemoteIpAddress, providedKey);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Forbidden",
                Message = "Invalid API Key"
            });
            return;
        }

        // API key is valid, continue to next middleware
        _logger.LogDebug("API Key validated successfully for {Path}", context.Request.Path);
        await _next(context);
    }
}

public static class ApiKeyAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthMiddleware>();
    }
}
