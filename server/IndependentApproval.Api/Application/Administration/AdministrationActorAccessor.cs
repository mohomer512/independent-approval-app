using IndependentApproval.Api.Application.Authorization;

namespace IndependentApproval.Api.Application.Administration;

public sealed class AdministrationActorAccessor(
    IHttpContextAccessor httpContextAccessor,
    IApplicationAccessService applicationAccessService) : IAdministrationActorAccessor
{
    public async Task<AdministrationActor> GetCurrentAsync(
        CancellationToken cancellationToken)
    {
        var principal = httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException(
                "An active authenticated HTTP context is required.");
        var access = await applicationAccessService.ResolveAsync(
            principal,
            cancellationToken);

        if (!access.IsSystemAdministrator || string.IsNullOrWhiteSpace(access.AccountName))
        {
            throw new InvalidOperationException(
                "A system administrator identity is required for this operation.");
        }

        return new AdministrationActor(access.AccountName, access.ApplicationUserId);
    }
}
