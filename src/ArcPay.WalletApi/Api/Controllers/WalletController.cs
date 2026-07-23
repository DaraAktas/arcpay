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
[Route("api/wallet")]
public sealed class WalletController(WalletService walletService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WalletResponse>>> List(CancellationToken cancellationToken)
    {
        var ownerResult = GetOwner();
        if (ownerResult.IsFailure)
        {
            return Unauthorized();
        }

        var wallets = await walletService.ListAsync(ownerResult.Value, cancellationToken);
        return Ok(wallets.Select(WalletResponse.From));
    }

    [HttpPost]
    public async Task<ActionResult<WalletResponse>> Open(
        OpenWalletRequest request,
        CancellationToken cancellationToken)
    {
        var ownerResult = GetOwner();
        if (ownerResult.IsFailure)
        {
            return Unauthorized();
        }

        var result = await walletService.OpenAsync(ownerResult.Value, request.Currency, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, WalletResponse.From(result.Value))
            : ToProblem(result.Error);
    }

    [HttpPost("{currency}/deposit")]
    public async Task<ActionResult<DepositResponse>> Deposit(
        string currency,
        DepositRequest request,
        CancellationToken cancellationToken)
    {
        var ownerResult = GetOwner();
        if (ownerResult.IsFailure)
        {
            return Unauthorized();
        }

        var result = await walletService.DepositAsync(
            ownerResult.Value,
            currency,
            request.Amount,
            request.TransactionRef,
            cancellationToken);
        return result.IsSuccess ? Ok(DepositResponse.From(result.Value)) : ToProblem(result.Error);
    }

    private Result<CustomerNumber> GetOwner() =>
        CustomerNumber.Create(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

    private ObjectResult ToProblem(Error error)
    {
        var problem = new ProblemDetails
        {
            Status = error.StatusCode,
            Title = error.Description,
            Type = $"urn:arcpay:error:{error.Code}"
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        return StatusCode(error.StatusCode, problem);
    }
}
