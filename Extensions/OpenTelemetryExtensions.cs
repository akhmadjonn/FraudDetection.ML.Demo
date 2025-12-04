using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Beepul.Afs.FraudDetection.ML.Host.Extensions;

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Configures OpenTelemetry with tracing, metrics, and Serilog integration for WebApplicationBuilder
    /// </summary>
    public static WebApplicationBuilder AddOpenTelemetryInstrumentation(
        this WebApplicationBuilder builder,
        Action<TracerProviderBuilder>? traceBuilder = null,
        Action<MeterProviderBuilder>? metricsBuilder = null)
    {
        var config = builder.Configuration.GetSection("OpenTelemetry")
            .GetChildren()
            .ToDictionary(x => x.Key, x => x.Value);

        var instanceId = Environment.GetEnvironmentVariable("OTL_INSTANCE_ID");

        // Validate configuration
        if (!config.ContainsKey("ServiceName") ||
            !config.ContainsKey("CollectorUrl") ||
            !config.ContainsKey("ServiceNamespace"))
        {
            Console.WriteLine("!!! OpenTelemetry configuration incomplete (ServiceName, ServiceNamespace, or CollectorUrl missing). Skipping instrumentation...");
            builder.Host.UseSerilog();
            return builder;
        }

        if (string.IsNullOrEmpty(instanceId))
        {
            Console.WriteLine("!!! Environment variable OTL_INSTANCE_ID not set. Skipping OpenTelemetry instrumentation...");
            builder.Host.UseSerilog();
            return builder;
        }

        var serviceName = config["ServiceName"]!;
        var serviceNamespace = config["ServiceNamespace"]!;
        var collectorUrl = config["CollectorUrl"]!;
        var serviceInstanceId = $"{serviceName}_{instanceId}";

        // Configure Serilog with OpenTelemetry sink
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console()
            .WriteTo.OpenTelemetry(cfg =>
            {
                cfg.Endpoint = collectorUrl;
                cfg.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField;
                cfg.ResourceAttributes = new Dictionary<string, object>
                {
                    { "service.name", serviceName },
                    { "service.namespace", serviceNamespace },
                    { "service.instance.id", serviceInstanceId }
                };
            })
            .CreateLogger();

        builder.Host.UseSerilog();

        // Configure OpenTelemetry
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: serviceName,
                        serviceNamespace: serviceNamespace,
                        serviceInstanceId: serviceInstanceId,
                        autoGenerateServiceInstanceId: false));

                tracing.SetSampler(new AlwaysOnSampler());

                tracing.AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                });

                tracing.AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.FilterHttpRequestMessage = (message) =>
                        !message.RequestUri?.PathAndQuery.Contains("opentelemetry.proto.collector") ?? true;
                });

                tracing.AddSource(serviceName);

                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(collectorUrl);
                });

                // Allow custom tracing configuration
                traceBuilder?.Invoke(tracing);
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: serviceName,
                        serviceNamespace: serviceNamespace,
                        serviceInstanceId: serviceInstanceId,
                        autoGenerateServiceInstanceId: false));

                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddProcessInstrumentation();
                metrics.AddRuntimeInstrumentation();

                metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(collectorUrl);
                });

                // Allow custom metrics configuration
                metricsBuilder?.Invoke(metrics);
            });

        Console.WriteLine($"✅ OpenTelemetry configured: Service={serviceName}, Namespace={serviceNamespace}, Instance={serviceInstanceId}, Collector={collectorUrl}");

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry with tracing, metrics, and Serilog integration for HostApplicationBuilder (Background mode)
    /// </summary>
    public static HostApplicationBuilder AddOpenTelemetryInstrumentation(
        this HostApplicationBuilder builder,
        Action<TracerProviderBuilder>? traceBuilder = null,
        Action<MeterProviderBuilder>? metricsBuilder = null)
    {
        var config = builder.Configuration.GetSection("OpenTelemetry")
            .GetChildren()
            .ToDictionary(x => x.Key, x => x.Value);

        var instanceId = Environment.GetEnvironmentVariable("OTL_INSTANCE_ID");

        // Validate configuration
        if (!config.ContainsKey("ServiceName") ||
            !config.ContainsKey("CollectorUrl") ||
            !config.ContainsKey("ServiceNamespace"))
        {
            Console.WriteLine("!!! OpenTelemetry configuration incomplete (ServiceName, ServiceNamespace, or CollectorUrl missing). Skipping instrumentation...");
            builder.Services.AddSerilog();
            return builder;
        }

        if (string.IsNullOrEmpty(instanceId))
        {
            Console.WriteLine("!!! Environment variable OTL_INSTANCE_ID not set. Skipping OpenTelemetry instrumentation...");
            builder.Services.AddSerilog();
            return builder;
        }

        var serviceName = config["ServiceName"]!;
        var serviceNamespace = config["ServiceNamespace"]!;
        var collectorUrl = config["CollectorUrl"]!;
        var serviceInstanceId = $"{serviceName}_{instanceId}";

        // Configure Serilog with OpenTelemetry sink
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console()
            .WriteTo.OpenTelemetry(cfg =>
            {
                cfg.Endpoint = collectorUrl;
                cfg.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField;
                cfg.ResourceAttributes = new Dictionary<string, object>
                {
                    { "service.name", serviceName },
                    { "service.namespace", serviceNamespace },
                    { "service.instance.id", serviceInstanceId }
                };
            })
            .CreateLogger();

        builder.Services.AddSerilog();

        // Configure OpenTelemetry
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: serviceName,
                        serviceNamespace: serviceNamespace,
                        serviceInstanceId: serviceInstanceId,
                        autoGenerateServiceInstanceId: false));

                tracing.SetSampler(new AlwaysOnSampler());

                tracing.AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.FilterHttpRequestMessage = (message) =>
                        !message.RequestUri?.PathAndQuery.Contains("opentelemetry.proto.collector") ?? true;
                });

                tracing.AddSource(serviceName);

                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(collectorUrl);
                });

                // Allow custom tracing configuration
                traceBuilder?.Invoke(tracing);
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: serviceName,
                        serviceNamespace: serviceNamespace,
                        serviceInstanceId: serviceInstanceId,
                        autoGenerateServiceInstanceId: false));

                metrics.AddHttpClientInstrumentation();
                metrics.AddProcessInstrumentation();
                metrics.AddRuntimeInstrumentation();

                metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(collectorUrl);
                });

                // Allow custom metrics configuration
                metricsBuilder?.Invoke(metrics);
            });

        Console.WriteLine($"✅ OpenTelemetry configured: Service={serviceName}, Namespace={serviceNamespace}, Instance={serviceInstanceId}, Collector={collectorUrl}");

        return builder;
    }
}
