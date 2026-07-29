using ArcPay.CustomerApi.Models;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace ArcPay.CustomerApi.Data;

public sealed class CustomerDevelopmentSeeder(CustomerDbContext dbContext)
{
    private const string DemoPassword = "Demo123!";

    private static readonly (string Number, string Name, string Email, string Phone)[] DemoCustomers =
    [
        ("ARC-9000000001", "Demo Gönderen", "demo.sender@arcpay.test", "+905551000001"),
        ("ARC-9000000002", "Demo Alıcı", "demo.receiver@arcpay.test", "+905551000002"),
        ("ARC-9000000003", "Demo Boş Hesap", "demo.empty@arcpay.test", "+905551000003")
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var demo in DemoCustomers)
        {
            if (await dbContext.Customers.AnyAsync(customer => customer.CustomerNumber == demo.Number, cancellationToken))
                continue;

            dbContext.Customers.Add(Customer.CreateDevelopment(
                demo.Number,
                demo.Name,
                demo.Email,
                demo.Phone,
                BC.HashPassword(DemoPassword, 12)));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
