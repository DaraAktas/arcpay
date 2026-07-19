namespace ArcPay.CustomerApi.Dtos;

public sealed record RegisterRequest(string FullName, string Email, string Password);
