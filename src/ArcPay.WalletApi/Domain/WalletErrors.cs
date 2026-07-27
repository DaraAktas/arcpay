using ArcPay.Shared.Results;

namespace ArcPay.WalletApi.Domain;

public static class WalletErrors
{
    public static readonly Error InvalidCustomerNumber =
        new("wallet.invalid_customer_number", "Customer number format is invalid.", 400);

    public static readonly Error InvalidCurrency =
        new("wallet.invalid_currency", "Currency is not supported.", 400);

    public static readonly Error InvalidAmount =
        new("wallet.invalid_amount", "Amount must be positive and have at most 8 decimal places.", 400);

    public static readonly Error InvalidTransactionReference =
        new("wallet.invalid_transaction_reference", "Transaction reference is required.", 400);

    public static readonly Error CurrencyMismatch =
        new("wallet.currency_mismatch", "Money currency must match wallet currency.", 409);

    public static readonly Error InsufficientFunds =
        new("wallet.insufficient_funds", "Wallet balance is insufficient.", 409);

    public static readonly Error NotFound =
        new("wallet.not_found", "Wallet was not found.", 404);

    public static readonly Error AlreadyExists =
        new("wallet.already_exists", "A wallet already exists for this currency.", 409);

    public static readonly Error TransactionReferenceConflict =
        new("wallet.transaction_reference_conflict", "Transaction reference was already used.", 409);

    public static readonly Error SelfTransfer =
        new("wallet.self_transfer", "Sender and receiver must be different customers.", 409);
}
