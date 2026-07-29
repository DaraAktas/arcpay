using ArcPay.InvestmentApi.Application.Abstractions;
using ArcPay.InvestmentApi.Domain;
using ArcPay.InvestmentApi.Infrastructure;
using ArcPay.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace ArcPay.InvestmentApi.Application;

public sealed class PurchaseService(
    InvestmentDbContext dbContext,
    MarketService marketService,
    IWalletPaymentGateway walletGateway,
    IHostEnvironment environment,
    ILogger<PurchaseService> logger)
{
    public async Task<PortfolioView> GetPortfolioAsync(string customerNumber, CancellationToken cancellationToken)
    {
        var portfolio = await dbContext.Portfolios.AsNoTracking().Include(item => item.Holdings)
            .SingleOrDefaultAsync(item => item.CustomerNumber == customerNumber, cancellationToken);
        return portfolio is null
            ? new PortfolioView(customerNumber, [])
            : ToView(portfolio);
    }

    public async Task<Result<PurchaseView>> PurchaseAsync(
        string customerNumber,
        string symbol,
        decimal quantity,
        Guid purchaseReference,
        bool simulatePortfolioFailure,
        CancellationToken cancellationToken)
    {
        if (purchaseReference == Guid.Empty)
            return Result<PurchaseView>.Failure(InvestmentErrors.PurchaseReferenceConflict);
        if (quantity <= 0 || decimal.Round(quantity, 8) != quantity)
            return Result<PurchaseView>.Failure(InvestmentErrors.InvalidQuantity);

        var existing = await dbContext.PurchaseOrders.AsNoTracking()
            .SingleOrDefaultAsync(order => order.PurchaseRef == purchaseReference, cancellationToken);
        if (existing is not null)
        {
            if (existing.CustomerNumber != customerNumber || existing.Symbol != symbol.Trim().ToUpperInvariant() || existing.Quantity != quantity)
                return Result<PurchaseView>.Failure(InvestmentErrors.PurchaseReferenceConflict);
            if (existing.Status == "Completed") return Result<PurchaseView>.Success(ToView(existing, true));
            if (existing.Status is "Compensated" or "CompensationFailed")
                return Result<PurchaseView>.Failure(existing.Status == "Compensated"
                    ? InvestmentErrors.PurchaseCompensated
                    : InvestmentErrors.CompensationFailed);
        }

        PurchaseOrder order;
        if (existing is null)
        {
            var quoteResult = await marketService.GetQuoteAsync(symbol, cancellationToken);
            if (quoteResult.IsFailure) return Result<PurchaseView>.Failure(quoteResult.Error);
            order = PurchaseOrder.Start(purchaseReference, customerNumber, quoteResult.Value, quantity);
            dbContext.PurchaseOrders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            order = await dbContext.PurchaseOrders.SingleAsync(item => item.PurchaseRef == purchaseReference, cancellationToken);
        }

        var charge = await walletGateway.ChargeAsync(
            order.TotalAmount, order.Currency, order.PurchaseRef, $"{order.Symbol} investment purchase", cancellationToken);
        if (charge.IsFailure) return Result<PurchaseView>.Failure(charge.Error);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            if (simulatePortfolioFailure && environment.IsDevelopment())
                throw new InvalidOperationException("Development compensation scenario requested.");

            var portfolio = await dbContext.Portfolios.Include(item => item.Holdings)
                .SingleOrDefaultAsync(item => item.CustomerNumber == customerNumber, cancellationToken);
            if (portfolio is null)
            {
                portfolio = Portfolio.Open(customerNumber);
                dbContext.Portfolios.Add(portfolio);
            }

            var addResult = portfolio.AddPurchase(order.Symbol, order.Quantity, order.UnitPrice, order.Currency);
            if (addResult.IsFailure) return Result<PurchaseView>.Failure(addResult.Error);
            order.Complete();
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<PurchaseView>.Success(ToView(order, charge.Value.IsReplay));
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Portfolio write failed for {PurchaseRef}; compensation started.", order.PurchaseRef);
            dbContext.ChangeTracker.Clear();
            return await CompensateAsync(order.PurchaseRef, exception.Message, cancellationToken);
        }
    }

    private async Task<Result<PurchaseView>> CompensateAsync(Guid purchaseReference, string reason, CancellationToken cancellationToken)
    {
        var order = await dbContext.PurchaseOrders.SingleAsync(item => item.PurchaseRef == purchaseReference, cancellationToken);
        var refund = await walletGateway.RefundAsync(order.PurchaseRef, order.RefundRef, cancellationToken);
        if (refund.IsSuccess)
        {
            order.Compensate(reason);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<PurchaseView>.Failure(InvestmentErrors.PurchaseCompensated);
        }

        var failureReason = $"{reason} Refund: {refund.Error.Description}";
        order.MarkCompensationFailed(failureReason);
        dbContext.FailedCompensations.Add(FailedCompensation.Record(order, failureReason));
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogError("Investment compensation failed for {PurchaseRef}; manual review required.", order.PurchaseRef);
        return Result<PurchaseView>.Failure(InvestmentErrors.CompensationFailed);
    }

    private static PortfolioView ToView(Portfolio portfolio) => new(
        portfolio.CustomerNumber,
        portfolio.Holdings.OrderBy(item => item.Symbol)
            .Select(item => new HoldingView(item.Symbol, item.Quantity, item.AverageCost, item.Currency)).ToArray());

    private static PurchaseView ToView(PurchaseOrder order, bool replay) => new(
        order.PurchaseRef, order.Symbol, order.Quantity, order.UnitPrice, order.TotalAmount, order.Currency, order.Status, replay);
}
