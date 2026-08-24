using IndependentApproval.Api.Application.Administration;
using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Contracts.Administration.Directory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/directory/users")]
[Authorize(Policy = AuthorizationPolicyNames.SystemAdministrator)]
public sealed class DirectoryUsersController(
    IDirectoryAdministrationService directoryAdministrationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DirectoryUserSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DirectoryUserSearchResponse>> Search(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await directoryAdministrationService.SearchAsync(
            query,
            page,
            pageSize,
            cancellationToken));
}
