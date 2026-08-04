using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace ArcPay.Shared.Observability;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaximumLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName].FirstOrDefault());

        context.TraceIdentifier = correlationId;
        context.Request.Headers[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(string? candidate) =>
        !string.IsNullOrWhiteSpace(candidate) && candidate.Length <= MaximumLength
            ? candidate.Trim()
            : Guid.NewGuid().ToString("N");
}
