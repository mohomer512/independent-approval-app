using IndependentApproval.Api.Contracts.Administration.Roles;
using IndependentApproval.Api.Contracts.Common;

namespace IndependentApproval.Api.Application.Administration;

public interface IRoleAdministrationService
{
    Task<PagedResponse<ApplicationRoleResponse>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ApplicationRoleResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken);

    IReadOnlyList<PermissionResponse> ListPermissions();

    Task<ApplicationRoleResponse> CreateAsync(
        CreateApplicationRoleRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationRoleResponse> UpdateAsync(
        Guid id,
        UpdateApplicationRoleRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationRoleResponse> ArchiveAsync(
        Guid id,
        ApplicationRoleConcurrencyRequest request,
        CancellationToken cancellationToken);
}
