using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArcPay.CustomerApi.Dtos;
using ArcPay.CustomerApi.Models;
using ArcPay.Shared.Security;
using Microsoft.IdentityModel.Tokens;

namespace ArcPay.CustomerApi.Services;

public sealed class JwtTokenService(JwtOptions options)
{
    public AuthResponse Create(Customer customer)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(options.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, customer.CustomerNumber),
            new Claim(JwtRegisteredClaimNames.Email, customer.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64)
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            expiresAt,
            customer.ToResponse());
    }
}
