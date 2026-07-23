using ArcPay.WalletApi.Domain;
using ArcPay.WalletApi.Domain.ValueObjects;

namespace ArcPay.WalletApi.Tests.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void Currency_NormalizesSupportedCode()
    {
        var result = Currency.Create(" try ");

        Assert.True(result.IsSuccess);
        Assert.Equal("TRY", result.Value.Code);
    }

    [Fact]
    public void Currency_RejectsUnsupportedCode()
    {
        var result = Currency.Create("BTC");

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InvalidCurrency, result.Error);
    }

    [Theory]
    [InlineData("ARC-1000000001")]
    [InlineData(" arc-9999999999 ")]
    public void CustomerNumber_AcceptsAndNormalizesValidValue(string value)
    {
        var result = CustomerNumber.Create(value);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("ARC-", result.Value.Value);
    }

    [Theory]
    [InlineData("1000000001")]
    [InlineData("ARC-123")]
    [InlineData("")]
    public void CustomerNumber_RejectsInvalidValue(string value)
    {
        var result = CustomerNumber.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InvalidCustomerNumber, result.Error);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0.00000001")]
    [InlineData("9999999999.12345678")]
    public void Money_AcceptsPositiveAmountsWithEightDecimalPlaces(string value)
    {
        var currency = Currency.Create("TRY").Value;
        var result = Money.Create(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), currency);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.123456789")]
    public void Money_RejectsInvalidAmount(string value)
    {
        var currency = Currency.Create("TRY").Value;
        var result = Money.Create(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), currency);

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InvalidAmount, result.Error);
    }

    [Fact]
    public void Money_AddsOnlyMatchingCurrencies()
    {
        var currency = Currency.Create("TRY").Value;
        var left = Money.Create(10.25m, currency).Value;
        var right = Money.Create(4.75m, currency).Value;

        Assert.Equal(15m, (left + right).Amount);
    }

    [Fact]
    public void Money_RejectsArithmeticAcrossCurrencies()
    {
        var lira = Money.Create(10m, Currency.Create("TRY").Value).Value;
        var dollar = Money.Create(10m, Currency.Create("USD").Value).Value;

        Assert.Throws<InvalidOperationException>(() => lira + dollar);
    }
}
