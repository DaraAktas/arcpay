using ArcPay.InvestmentApi.Domain;
using ArcPay.Shared.Results;

namespace ArcPay.InvestmentApi.Application.Abstractions;

public interface IMarketDataProvider
{
    Task<Result<Quote>> GetQuoteAsync(string symbol, CancellationToken cancellationToken);
}
