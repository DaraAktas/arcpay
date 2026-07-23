namespace ArcPay.WalletApi.Api.Dtos;

public sealed record DepositRequest(decimal Amount, Guid TransactionRef);
