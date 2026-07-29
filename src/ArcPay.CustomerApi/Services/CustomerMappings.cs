using ArcPay.CustomerApi.Dtos;
using ArcPay.CustomerApi.Models;

namespace ArcPay.CustomerApi.Services;

internal static class CustomerMappings
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.CustomerNumber, customer.FullName, customer.Email, customer.PhoneNumber);
}
