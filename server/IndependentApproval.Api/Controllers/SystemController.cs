using IndependentApproval.Api.Contracts.System;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController(IHostEnvironment hostEnvironment) : ControllerBase
{
    private const string ApplicationName = "Independent Approval API";

    private static readonly string ApplicationVersion =
        typeof(SystemController).Assembly.GetName().Version?.ToString() ?? "Unknown";

    [HttpGet("info")]
    [ProducesResponseType<SystemInfoResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemInfoResponse> GetInfo()
    {
        var response = new SystemInfoResponse(
            ApplicationName,
            ApplicationVersion,
            hostEnvironment.EnvironmentName,
            DateTimeOffset.UtcNow);

        return Ok(response);
    }
}
