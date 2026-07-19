using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArcPay.Shared.Validation;

public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new Dictionary<string, string[]>();

        foreach (var argument in context.ActionArguments.Values.Where(value => value is not null))
        {
            var argumentType = argument!.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContextType = typeof(ValidationContext<>).MakeGenericType(argumentType);
            var validationContext = (IValidationContext)Activator.CreateInstance(validationContextType, argument)!;
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            foreach (var group in result.Errors.GroupBy(error => error.PropertyName))
            {
                failures[group.Key] = group.Select(error => error.ErrorMessage).Distinct().ToArray();
            }
        }

        if (failures.Count > 0)
        {
            var problem = new ValidationProblemDetails(failures)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed."
            };
            problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }
}
