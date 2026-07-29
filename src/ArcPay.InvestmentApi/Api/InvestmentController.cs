using System.IdentityModel.Tokens.Jwt;
using ArcPay.InvestmentApi.Application;
using ArcPay.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcPay.InvestmentApi.Api;

[ApiController]
[Authorize]
[Route("api/investment")]
public sealed class InvestmentController(MarketService marketService, PurchaseService purchaseService) : ControllerBase
{
    [HttpGet("market")]
    public async Task<IActionResult> Market(CancellationToken cancellationToken) =>
        Ok(await marketService.ListAsync(cancellationToken));

    [HttpGet("portfolio")]
    public async Task<IActionResult> Portfolio(CancellationToken cancellationToken)
    {
        var customerNumber = GetCustomerNumber();
        return customerNumber is null
            ? Unauthorized()
            : Ok(await purchaseService.GetPortfolioAsync(customerNumber, cancellationToken));
    }

    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase(PurchaseRequest request, CancellationToken cancellationToken)
    {
        var customerNumber = GetCustomerNumber();
        if (customerNumber is null) return Unauthorized();
        var result = await purchaseService.PurchaseAsync(
            customerNumber, request.Symbol, request.Quantity, request.PurchaseRef,
            request.SimulatePortfolioFailure, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    private string? GetCustomerNumber() => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    private ObjectResult ToProblem(Error error)
    {
        var problem = new ProblemDetails { Status = error.StatusCode, Title = error.Description };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        return StatusCode(error.StatusCode, problem);
    }
}
