using IndependentApproval.Api.Contracts.Administration.Users;
using IndependentApproval.Api.Contracts.Common;

namespace IndependentApproval.Api.Application.Administration;

public interface IApplicationUserAdministrationService
{
    Task<ApplicationUserResponse> AddAsync(
        AddApplicationUserRequest request,
        CancellationToken cancellationToken);

    Task<PagedResponse<ApplicationUserResponse>> ListAsync(
        string? search,
        bool includeRemoved,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationUserResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ApplicationUserResponse> UpdateRolesAsync(
        Guid id,
        UpdateApplicationUserRolesRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationUserResponse> LockAsync(
        Guid id,
        LockApplicationUserRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationUserResponse> UnlockAsync(
        Guid id,
        ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationUserResponse> RemoveAsync(
        Guid id,
        ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationUserResponse> RestoreAsync(
        Guid id,
        ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationUserResponse> RefreshDirectoryProfileAsync(
        Guid id,
        ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken);
}
