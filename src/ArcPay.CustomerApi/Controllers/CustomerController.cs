using System.IdentityModel.Tokens.Jwt;
using ArcPay.CustomerApi.Dtos;
using ArcPay.CustomerApi.Services;
using ArcPay.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArcPay.CustomerApi.Controllers;

[ApiController]
[Route("api/customer")]
public sealed class CustomerController(CustomerService customerService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<CustomerResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customerService.RegisterAsync(request, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToProblem(result.Error);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customerService.LoginAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CustomerResponse>> Me(CancellationToken cancellationToken)
    {
        var customerNumber = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrWhiteSpace(customerNumber))
        {
            return Unauthorized();
        }

        var result = await customerService.GetByCustomerNumberAsync(customerNumber, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

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
