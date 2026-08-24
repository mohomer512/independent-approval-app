using System.Security.Claims;
using IndependentApproval.Api.Application.Authorization;

namespace IndependentApproval.Api.Application.Directory;

public sealed class CurrentUserDisplayNameResolver(
    IDirectoryService directoryService,
    ILogger<CurrentUserDisplayNameResolver> logger) :
    ICurrentUserDisplayNameResolver
{
    public async Task<string> ResolveAsync(
        ClaimsPrincipal principal,
        ApplicationAccessSnapshot access,
        CancellationToken cancellationToken)
    {
        if (principal.Identity?.IsAuthenticated != true
            || string.IsNullOrWhiteSpace(access.AccountName))
        {
            return access.DisplayName;
        }

        try
        {
            var directoryUser = await directoryService.FindByAccountNameAsync(
                access.AccountName,
                cancellationToken);

            return string.IsNullOrWhiteSpace(directoryUser?.DisplayName)
                ? access.DisplayName
                : directoryUser.DisplayName.Trim();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "The current user's directory display name could not be resolved; a safe fallback will be used.");
            return access.DisplayName;
        }
    }
}
