using System.IdentityModel.Tokens.Jwt;
using ArcPay.Shared.Results;
using ArcPay.WalletApi.Api.Dtos;
using ArcPay.WalletApi.Application.Wallets;
using ArcPay.WalletApi.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcPay.WalletApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transaction/investment")]
public sealed class InvestmentPaymentController(InvestmentPaymentService paymentService) : ControllerBase
{
    [HttpPost("charge")]
    public async Task<ActionResult<InvestmentPaymentResponse>> Charge(
        InvestmentChargeRequest request,
        CancellationToken cancellationToken)
    {
        var owner = GetOwner();
        if (owner.IsFailure) return Unauthorized();
        var result = await paymentService.ChargeAsync(
            owner.Value, request.Amount, request.Currency, request.TransactionRef, request.Description, cancellationToken);
        return result.IsSuccess ? Ok(ToResponse(result.Value)) : ToProblem(result.Error);
    }

    [HttpPost("refund")]
    public async Task<ActionResult<InvestmentPaymentResponse>> Refund(
        InvestmentRefundRequest request,
        CancellationToken cancellationToken)
    {
        var owner = GetOwner();
        if (owner.IsFailure) return Unauthorized();
        var result = await paymentService.RefundAsync(
            owner.Value, request.OriginalTransactionRef, request.RefundTransactionRef, cancellationToken);
        return result.IsSuccess ? Ok(ToResponse(result.Value)) : ToProblem(result.Error);
    }

    private Result<CustomerNumber> GetOwner() =>
        CustomerNumber.Create(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

    private static InvestmentPaymentResponse ToResponse(InvestmentPaymentView view) =>
        new(view.TransactionReference, view.Amount, view.Currency, view.IsReplay);

    private ObjectResult ToProblem(Error error)
    {
        var problem = new ProblemDetails { Status = error.StatusCode, Title = error.Description };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        return StatusCode(error.StatusCode, problem);
    }
}
