using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IApplicationAccessService applicationAccessService) : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        var identity = HttpContext.User.Identity;
        var access = await applicationAccessService.ResolveAsync(
            HttpContext.User,
            cancellationToken);

        if (access.HasApplicationAccess && access.ApplicationUserId is Guid applicationUserId)
        {
            await applicationAccessService.RecordSuccessfulAccessAsync(
                applicationUserId,
                cancellationToken);
        }

        var response = new CurrentUserResponse(
            identity?.IsAuthenticated == true,
            access.AccountName,
            access.Domain,
            access.UserName,
            identity?.AuthenticationType ?? string.Empty,
            access.DisplayName,
            access.ApplicationUserId,
            access.AccessState,
            access.HasApplicationAccess,
            access.IsSystemAdministrator,
            access.RoleCodes,
            access.Permissions,
            "en",
            "light");

        return Ok(response);
    }
}
