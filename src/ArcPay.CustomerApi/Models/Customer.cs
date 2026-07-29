using ArcPay.Shared;

namespace ArcPay.CustomerApi.Models;

public class Customer : BaseEntity
{
    public string CustomerNumber { get; private set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? NormalizedPhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    public static Customer CreateDevelopment(
        string customerNumber,
        string fullName,
        string email,
        string phoneNumber,
        string passwordHash) => new()
        {
            CustomerNumber = customerNumber,
            FullName = fullName,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PhoneNumber = phoneNumber,
            NormalizedPhoneNumber = phoneNumber,
            PasswordHash = passwordHash,
            CreatedBy = "development-seed",
            UpdatedBy = "development-seed"
        };
}
