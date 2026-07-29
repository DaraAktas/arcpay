using ArcPay.CustomerApi.Data;
using ArcPay.CustomerApi.Dtos;
using ArcPay.CustomerApi.Models;
using ArcPay.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using BC = BCrypt.Net.BCrypt;

namespace ArcPay.CustomerApi.Services;

public sealed class CustomerService(CustomerDbContext dbContext, JwtTokenService jwtTokenService)
{
    private const int BcryptWorkFactor = 12;
    private static readonly string DummyPasswordHash =
        BC.HashPassword("ArcPay.Invalid.Password.Placeholder", BcryptWorkFactor);

    public async Task<Result<CustomerResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (!PhoneNumberNormalizer.TryNormalize(request.PhoneNumber, out var normalizedPhone))
        {
            return Result<CustomerResponse>.Failure(CustomerErrors.InvalidPhone);
        }

        if (await dbContext.Customers.AnyAsync(
                customer => customer.NormalizedEmail == normalizedEmail ||
                            customer.NormalizedPhoneNumber == normalizedPhone,
                cancellationToken))
        {
            var emailExists = await dbContext.Customers.AnyAsync(
                customer => customer.NormalizedEmail == normalizedEmail,
                cancellationToken);
            return Result<CustomerResponse>.Failure(
                emailExists ? CustomerErrors.DuplicateEmail : CustomerErrors.DuplicatePhone);
        }

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            NormalizedEmail = normalizedEmail,
            PhoneNumber = normalizedPhone,
            NormalizedPhoneNumber = normalizedPhone,
            PasswordHash = BC.HashPassword(request.Password, BcryptWorkFactor),
            CreatedBy = "registration",
            UpdatedBy = "registration"
        };

        dbContext.Customers.Add(customer);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Customers_NormalizedEmail"
            })
        {
            return Result<CustomerResponse>.Failure(CustomerErrors.DuplicateEmail);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Customers_NormalizedPhoneNumber"
            })
        {
            return Result<CustomerResponse>.Failure(CustomerErrors.DuplicatePhone);
        }

        return Result<CustomerResponse>.Success(customer.ToResponse());
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var customer = await dbContext.Customers.SingleOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (customer is null)
        {
            BC.Verify(request.Password, DummyPasswordHash);
            return Result<AuthResponse>.Failure(CustomerErrors.InvalidCredentials);
        }

        if (!BC.Verify(request.Password, customer.PasswordHash))
        {
            return Result<AuthResponse>.Failure(CustomerErrors.InvalidCredentials);
        }

        if (BC.PasswordNeedsRehash(customer.PasswordHash, BcryptWorkFactor))
        {
            customer.PasswordHash = BC.HashPassword(request.Password, BcryptWorkFactor);
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedBy = "login-rehash";
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<AuthResponse>.Success(jwtTokenService.Create(customer));
    }

    public async Task<Result<CustomerResponse>> GetByCustomerNumberAsync(
        string customerNumber,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.CustomerNumber == customerNumber,
                cancellationToken);

        return customer is null
            ? Result<CustomerResponse>.Failure(CustomerErrors.NotFound)
            : Result<CustomerResponse>.Success(customer.ToResponse());
    }

    public async Task<Result<RecipientLookupResponse>> ResolveRecipientAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        var value = identifier.Trim();
        Customer? customer;
        if (value.StartsWith("ARC-", StringComparison.OrdinalIgnoreCase))
        {
            var customerNumber = value.ToUpperInvariant();
            customer = await dbContext.Customers.AsNoTracking().SingleOrDefaultAsync(
                candidate => candidate.CustomerNumber == customerNumber && candidate.RecordStatus == "A",
                cancellationToken);
        }
        else if (value.Contains('@'))
        {
            var normalizedEmail = NormalizeEmail(value);
            customer = await dbContext.Customers.AsNoTracking().SingleOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail && candidate.RecordStatus == "A",
                cancellationToken);
        }
        else if (PhoneNumberNormalizer.TryNormalize(value, out var normalizedPhone))
        {
            customer = await dbContext.Customers.AsNoTracking().SingleOrDefaultAsync(
                candidate => candidate.NormalizedPhoneNumber == normalizedPhone && candidate.RecordStatus == "A",
                cancellationToken);
        }
        else
        {
            customer = null;
        }

        return customer is null
            ? Result<RecipientLookupResponse>.Failure(CustomerErrors.NotFound)
            : Result<RecipientLookupResponse>.Success(new(customer.CustomerNumber, customer.FullName));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}
