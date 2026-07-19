using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ArcPay.Shared.Security;

public static class JwtServiceCollectionExtensions
{
    public static IServiceCollection AddArcPayJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? throw new InvalidOperationException("JWT configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
            string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
            string.IsNullOrWhiteSpace(jwtOptions.Key))
        {
            throw new InvalidOperationException(
                "Jwt:Issuer, Jwt:Audience and Jwt:Key must be configured. Store Jwt:Key in user-secrets or an environment variable.");
        }

        if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 256 bits (32 UTF-8 bytes).");
        }

        services.AddSingleton(jwtOptions);
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub"
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/problem+json";
                        context.Response.Headers.WWWAuthenticate = "Bearer";

                        var problem = new ProblemDetails
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Title = "Authentication is required.",
                            Type = "urn:arcpay:error:Authentication.Required"
                        };
                        problem.Extensions["code"] = "Authentication.Required";
                        problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
                        await context.Response.WriteAsJsonAsync(problem);
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/problem+json";

                        var problem = new ProblemDetails
                        {
                            Status = StatusCodes.Status403Forbidden,
                            Title = "You are not allowed to access this resource.",
                            Type = "urn:arcpay:error:Authorization.Forbidden"
                        };
                        problem.Extensions["code"] = "Authorization.Forbidden";
                        problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
                        await context.Response.WriteAsJsonAsync(problem);
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
