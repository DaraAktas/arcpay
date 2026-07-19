using ArcPay.WalletApi.Data;
using ArcPay.Shared.Errors;
using ArcPay.Shared.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
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
