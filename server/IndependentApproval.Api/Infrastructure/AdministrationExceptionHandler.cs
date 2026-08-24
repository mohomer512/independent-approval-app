using IndependentApproval.Api.Application.Administration;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Infrastructure;

public sealed class AdministrationExceptionHandler(
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not AdministrationException administrationException)
        {
            return false;
        }

        httpContext.Response.StatusCode = administrationException.StatusCode;

        ProblemDetails problemDetails = administrationException switch
        {
            AdministrationValidationException validationException =>
                new HttpValidationProblemDetails(
                    validationException.Errors.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.OrdinalIgnoreCase)),
            _ => new ProblemDetails()
        };

        problemDetails.Status = administrationException.StatusCode;
        problemDetails.Title = administrationException.Title;
        problemDetails.Detail = administrationException.Message;
        problemDetails.Type = GetProblemType(administrationException.StatusCode);
        problemDetails.Instance = httpContext.Request.Path;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["code"] = administrationException.Code;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });

        return true;
    }

    private static string GetProblemType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest =>
            "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1",
        StatusCodes.Status404NotFound =>
            "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5",
        StatusCodes.Status409Conflict =>
            "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10",
        StatusCodes.Status503ServiceUnavailable =>
            "https://www.rfc-editor.org/rfc/rfc9110#section-15.6.4",
        _ => "about:blank"
    };
}
