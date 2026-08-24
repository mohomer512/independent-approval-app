using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IndependentApproval.Api.Tests.Infrastructure;

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(
        options,
        logger,
        encoder)
{
    public const string AuthenticationScheme = "CheckpointOneTest";
    public const string AccountHeaderName = "X-Checkpoint-One-Test-Account";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var accountName = Request.Headers[AccountHeaderName].FirstOrDefault()?.Trim();

        if (string.IsNullOrWhiteSpace(accountName))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, accountName),
            new Claim(ClaimTypes.NameIdentifier, accountName)
        ],
        AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
