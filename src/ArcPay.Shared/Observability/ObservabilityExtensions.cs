using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace ArcPay.Shared.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddArcPayObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, configuration) => configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .WriteTo.Console(new RenderedCompactJsonFormatter()));

        return builder;
    }

    public static WebApplication UseArcPayObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, context) =>
            {
                diagnosticContext.Set("CorrelationId", context.TraceIdentifier);
                diagnosticContext.Set("RequestHost", context.Request.Host.Value);
            };
        });
        return app;
    }
}
