using ArcPay.Shared.Results;

namespace ArcPay.CustomerApi.Services;

internal static class CustomerErrors
{
    public static readonly Error DuplicateEmail = new(
        "Customer.DuplicateEmail",
        "A customer with this email address already exists.",
        StatusCodes.Status409Conflict);

    public static readonly Error DuplicatePhone = new(
        "Customer.DuplicatePhone",
        "A customer with this phone number already exists.",
        StatusCodes.Status409Conflict);

    public static readonly Error InvalidPhone = new(
        "Customer.InvalidPhone",
        "Phone number format is invalid.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidCredentials = new(
        "Customer.InvalidCredentials",
        "Email or password is incorrect.",
        StatusCodes.Status401Unauthorized);

    public static readonly Error NotFound = new(
        "Customer.NotFound",
        "Customer was not found.",
        StatusCodes.Status404NotFound);
}
