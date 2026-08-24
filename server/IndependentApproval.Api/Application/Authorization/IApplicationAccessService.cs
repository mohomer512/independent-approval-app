using System.Security.Claims;

namespace IndependentApproval.Api.Application.Authorization;

public interface IApplicationAccessService
{
    Task<ApplicationAccessSnapshot> ResolveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task RecordSuccessfulAccessAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken);

    bool IsSystemAdministrator(string? accountName);
}
