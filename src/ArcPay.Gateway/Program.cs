using ArcPay.Shared.Errors;
using ArcPay.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddArcPayJwtAuthentication(builder.Configuration);
builder.Services.AddArcPayProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:5173") //default react port
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    service = "ArcPay.Gateway",
    status = "Healthy"
}));

app.MapReverseProxy();

app.Run();
