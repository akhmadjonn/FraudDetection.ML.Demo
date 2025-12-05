using Beepul.Afs.FraudDetection.ML.Host.Configuration;
using Microsoft.Extensions.Options;

namespace Beepul.Afs.FraudDetection.ML.Host.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly List<PartnerConfig> _partners;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private const string API_KEY_HEADER = "X-API-Key";
    public const string PARTNER_NAME_KEY = "PartnerName";

    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        IOptions<PartnersSettings> partnersSettings,
        ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _partners = partnersSettings.Value.Partners;
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

        // Check if partners are configured
        if (_partners == null || !_partners.Any())
        {
            _logger.LogCritical("No partners configured in appsettings. All requests will be rejected.");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Internal Server Error",
                Message = "API authentication not properly configured"
            });
            return;
        }

        // Find partner by API key
        var providedKey = extractedApiKey.ToString();
        var partner = _partners.FirstOrDefault(p => p.ApiKey == providedKey);

        if (partner == null)
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

        // Store partner name in HttpContext for use in controllers/logging
        context.Items[PARTNER_NAME_KEY] = partner.Name;

        // API key is valid, log partner name and continue
        _logger.LogInformation("✅ Request from partner: {PartnerName}, Path: {Path}",
            partner.Name, context.Request.Path);

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
