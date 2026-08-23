using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IndependentApproval.Api.Infrastructure;

public sealed class AntiforgeryValidationProblemDetailsFilter :
    IAlwaysRunResultFilter,
    IOrderedFilter
{
    private const string ProblemType =
        "urn:independent-approval:problem:antiforgery-validation";
    private const string ProblemCode = "security.antiforgery_validation_failed";

    public int Order => -3000;

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not IAntiforgeryValidationFailedResult)
        {
            return;
        }

        var httpContext = context.HttpContext;
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Antiforgery validation failed.",
            Type = ProblemType,
            Detail = "A valid antiforgery token is required for this request.",
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["code"] = ProblemCode;

        var result = new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
        result.ContentTypes.Add("application/problem+json");

        context.Result = result;
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
