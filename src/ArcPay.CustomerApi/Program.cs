using ArcPay.CustomerApi.Data;
using ArcPay.CustomerApi.Services;
using ArcPay.CustomerApi.Validators;
using ArcPay.Shared.Errors;
using ArcPay.Shared.Security;
using ArcPay.Shared.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<CustomerDevelopmentSeeder>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddScoped<FluentValidationActionFilter>();

builder.Services.AddControllers(options =>
    options.Filters.Add<FluentValidationActionFilter>());
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
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    await dbContext.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<CustomerDevelopmentSeeder>().SeedAsync();
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/customer/health", () => Results.Ok(new
{
    service = "ArcPay.CustomerApi",
    status = "Healthy"
}));

app.Run();
