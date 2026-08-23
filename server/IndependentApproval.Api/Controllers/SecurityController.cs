using IndependentApproval.Api.Contracts.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace IndependentApproval.Api.Controllers;

[ApiController]
[Route("api/security")]
[Authorize]
public sealed class SecurityController(
    IAntiforgery antiforgery,
    ILogger<SecurityController> logger) : ControllerBase
{
    private const string InternalServerErrorType =
        "https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1";

    [HttpGet("antiforgery-token")]
    [ProducesResponseType<AntiforgeryTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public ActionResult<AntiforgeryTokenResponse> GetAntiforgeryToken()
    {
        try
        {
            var tokenSet = antiforgery.GetAndStoreTokens(HttpContext);

            if (string.IsNullOrWhiteSpace(tokenSet.RequestToken))
            {
                logger.LogError(
                    "An antiforgery request token could not be generated. Trace identifier: {TraceIdentifier}",
                    HttpContext.TraceIdentifier);

                return CreateTokenGenerationProblem();
            }

            return Ok(new AntiforgeryTokenResponse(tokenSet.RequestToken));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "An antiforgery request token could not be generated. Trace identifier: {TraceIdentifier}",
                HttpContext.TraceIdentifier);

            return CreateTokenGenerationProblem();
        }
        finally
        {
            Response.Headers[HeaderNames.CacheControl] = "no-store";
        }
    }

    private ObjectResult CreateTokenGenerationProblem() =>
        Problem(
            detail: "The antiforgery request token could not be generated.",
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Unable to generate an antiforgery token.",
            type: InternalServerErrorType);
}
