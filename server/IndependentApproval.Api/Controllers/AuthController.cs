using IndependentApproval.Api.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndependentApproval.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> GetCurrentUser()
    {
        var identity = HttpContext.User.Identity;
        var accountName = identity?.Name ?? string.Empty;
        var (domain, userName) = ParseAccountName(accountName);

        var response = new CurrentUserResponse(
            identity?.IsAuthenticated == true,
            accountName,
            domain,
            userName,
            identity?.AuthenticationType ?? string.Empty);

        return Ok(response);
    }

    private static (string Domain, string UserName) ParseAccountName(string accountName)
    {
        var separatorIndex = accountName.IndexOf('\\');

        if (separatorIndex > 0 && separatorIndex < accountName.Length - 1)
        {
            return (accountName[..separatorIndex], accountName[(separatorIndex + 1)..]);
        }

        separatorIndex = accountName.LastIndexOf('@');

        if (separatorIndex > 0 && separatorIndex < accountName.Length - 1)
        {
            return (accountName[(separatorIndex + 1)..], accountName[..separatorIndex]);
        }

        return (string.Empty, accountName);
    }
}
