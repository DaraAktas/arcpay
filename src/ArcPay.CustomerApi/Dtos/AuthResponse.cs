namespace ArcPay.CustomerApi.Dtos;

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    CustomerResponse Customer);
