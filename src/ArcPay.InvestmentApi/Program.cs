using ArcPay.InvestmentApi.Api;
using ArcPay.InvestmentApi.Application;
using ArcPay.InvestmentApi.Application.Abstractions;
using ArcPay.InvestmentApi.Infrastructure;
using ArcPay.Shared.Errors;
using ArcPay.Shared.Security;
using ArcPay.Shared.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<InvestmentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<MarketService>();
builder.Services.AddScoped<PurchaseService>();
builder.Services.AddHttpClient<IWalletPaymentGateway, WalletHttpPaymentGateway>(client =>
    client.BaseAddress = new Uri(builder.Configuration["WalletApi:BaseUrl"] ?? "http://localhost:5002/"));

if (builder.Environment.IsDevelopment() &&
    !string.Equals(builder.Configuration["MarketData:Provider"], "AlphaVantage", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IMarketDataProvider, DevelopmentMarketDataProvider>();
}
else
{
    builder.Services.AddHttpClient<IMarketDataProvider, AlphaVantageMarketDataProvider>(client =>
        client.BaseAddress = new Uri(builder.Configuration["MarketData:BaseUrl"] ?? "https://www.alphavantage.co/"));
}

builder.Services.AddControllers(options => options.Filters.Add<FluentValidationActionFilter>());
builder.Services.AddValidatorsFromAssemblyContaining<PurchaseRequestValidator>();
builder.Services.AddScoped<FluentValidationActionFilter>();
builder.Services.AddArcPayJwtAuthentication(builder.Configuration);
builder.Services.AddArcPayProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<InvestmentDbContext>().Database.MigrateAsync();
    app.MapOpenApi();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/investment/health", () => Results.Ok(new { service = "ArcPay.InvestmentApi", status = "Healthy" }))
    .RequireAuthorization();
app.Run();

public partial class Program;
