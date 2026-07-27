using System.IdentityModel.Tokens.Jwt;
using ArcPay.Shared.Results;
using ArcPay.WalletApi.Api.Dtos;
using ArcPay.WalletApi.Application.Transactions;
using ArcPay.WalletApi.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcPay.WalletApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transaction")]
public sealed class TransactionController(TransferService transferService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionHistoryResponse>>> List(
        CancellationToken cancellationToken)
    {
        var ownerResult = GetOwner();
        if (ownerResult.IsFailure)
        {
            return Unauthorized();
        }

        var transactions = await transferService.ListAsync(ownerResult.Value, cancellationToken);
        return Ok(transactions.Select(TransactionHistoryResponse.From));
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<TransferResponse>> Transfer(
        TransferRequest request,
        CancellationToken cancellationToken)
    {
        var ownerResult = GetOwner();
        if (ownerResult.IsFailure)
        {
            return Unauthorized();
        }

        var result = await transferService.TransferAsync(
            ownerResult.Value,
            request.ToCustomerNumber,
            request.Currency,
            request.Amount,
            request.TransactionRef,
            request.Description,
            cancellationToken);
        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        var response = TransferResponse.From(result.Value);
        return result.Value.IsReplay
            ? Ok(response)
            : StatusCode(StatusCodes.Status201Created, response);
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
