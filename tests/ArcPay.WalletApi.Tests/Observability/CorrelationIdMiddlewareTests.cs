using ArcPay.Shared.Observability;
using Microsoft.AspNetCore.Http;

namespace ArcPay.WalletApi.Tests.Observability;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PreservesIncomingCorrelationIdAcrossRequestAndResponse()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "phase-6-demo";
        string? observedCorrelationId = null;
        var middleware = new CorrelationIdMiddleware(async currentContext =>
        {
            observedCorrelationId = currentContext.TraceIdentifier;
            await currentContext.Response.StartAsync();
        });

        await middleware.InvokeAsync(context);

        Assert.Equal("phase-6-demo", observedCorrelationId);
        Assert.Equal("phase-6-demo", context.Request.Headers[CorrelationIdMiddleware.HeaderName]);
        Assert.Equal("phase-6-demo", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task InvokeAsync_GeneratesCorrelationIdWhenHeaderIsMissing()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(currentContext => currentContext.Response.StartAsync());

        await middleware.InvokeAsync(context);

        Assert.Matches("^[a-f0-9]{32}$", context.TraceIdentifier);
        Assert.Equal(context.TraceIdentifier, context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }
}
