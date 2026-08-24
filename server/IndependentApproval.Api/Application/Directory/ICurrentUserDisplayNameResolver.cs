using System.Security.Claims;
using IndependentApproval.Api.Application.Authorization;

namespace IndependentApproval.Api.Application.Directory;

public interface ICurrentUserDisplayNameResolver
{
    Task<string> ResolveAsync(
        ClaimsPrincipal principal,
        ApplicationAccessSnapshot access,
        CancellationToken cancellationToken);
}
