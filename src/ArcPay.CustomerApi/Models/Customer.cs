using ArcPay.Shared;

namespace ArcPay.CustomerApi.Models;

public class Customer : BaseEntity
{
    public string CustomerNumber { get; private set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
