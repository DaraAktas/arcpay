using ArcPay.Shared.Errors;
using ArcPay.Shared.Security;
using ArcPay.Shared.Validation;
using ArcPay.WalletApi.Api.Validators;
using ArcPay.WalletApi.Application.Abstractions;
using ArcPay.WalletApi.Application.Transactions;
using ArcPay.WalletApi.Application.Wallets;
using ArcPay.WalletApi.Domain.Wallets;
using ArcPay.WalletApi.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers(options => options.Filters.Add<FluentValidationActionFilter>());
builder.Services.AddValidatorsFromAssemblyContaining<OpenWalletRequestValidator>();
builder.Services.AddScoped<WalletRepository>();
builder.Services.AddScoped<IWalletRepository>(provider => provider.GetRequiredService<WalletRepository>());
builder.Services.AddScoped<IWalletUnitOfWork>(provider => provider.GetRequiredService<WalletRepository>());
builder.Services.AddScoped<ITransactionHistoryReader>(provider => provider.GetRequiredService<WalletRepository>());
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<TransferService>();
builder.Services.AddArcPayJwtAuthentication(builder.Configuration);
builder.Services.AddArcPayProblemDetails();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/wallet/health", () => Results.Ok(new
{
    service = "ArcPay.WalletApi",
    status = "Healthy"
})).RequireAuthorization();

app.Run();
