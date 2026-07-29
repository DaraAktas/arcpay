using System.Net.Http.Headers;
using System.Net.Http.Json;
using ArcPay.InvestmentApi.Application.Abstractions;
using ArcPay.InvestmentApi.Domain;
using ArcPay.Shared.Results;

namespace ArcPay.InvestmentApi.Infrastructure;

public sealed class WalletHttpPaymentGateway(HttpClient httpClient, IHttpContextAccessor contextAccessor) : IWalletPaymentGateway
{
    public Task<Result<WalletPayment>> ChargeAsync(
        decimal amount, string currency, Guid reference, string description, CancellationToken cancellationToken) =>
        SendAsync("api/transaction/investment/charge", new { amount, currency, transactionRef = reference, description }, cancellationToken);

    public Task<Result<WalletPayment>> RefundAsync(
        Guid originalReference, Guid refundReference, CancellationToken cancellationToken) =>
        SendAsync("api/transaction/investment/refund", new { originalTransactionRef = originalReference, refundTransactionRef = refundReference }, cancellationToken);

    private async Task<Result<WalletPayment>> SendAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload) };
        var context = contextAccessor.HttpContext;
        if (context?.Request.Headers.Authorization is { Count: > 0 } authorization &&
            AuthenticationHeaderValue.TryParse(authorization.ToString(), out var parsed))
            request.Headers.Authorization = parsed;
        if (context?.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) == true)
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId.ToString());

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var payment = await response.Content.ReadFromJsonAsync<WalletPaymentResponse>(cancellationToken);
            return payment is null
                ? Result<WalletPayment>.Failure(InvestmentErrors.QuoteUnavailable)
                : Result<WalletPayment>.Success(new(payment.TransactionRef, payment.Amount, payment.Currency, payment.IsReplay));
        }

        var problem = await response.Content.ReadFromJsonAsync<WalletProblem>(cancellationToken);
        return Result<WalletPayment>.Failure(new Error(
            problem?.Code ?? "wallet.payment_failed",
            problem?.Title ?? "Wallet payment could not be completed.",
            (int)response.StatusCode));
    }

    private sealed record WalletPaymentResponse(Guid TransactionRef, decimal Amount, string Currency, bool IsReplay);
    private sealed record WalletProblem(string? Title, string? Code);
}
