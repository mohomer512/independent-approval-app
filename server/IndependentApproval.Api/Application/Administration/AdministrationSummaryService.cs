using IndependentApproval.Api.Contracts.Administration;
using IndependentApproval.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IndependentApproval.Api.Application.Administration;

public sealed class AdministrationSummaryService(
    IndependentApprovalDbContext dbContext) : IAdministrationSummaryService
{
    public async Task<AdministrationSummaryResponse> GetAsync(
        CancellationToken cancellationToken)
    {
        var activeUserCount = await dbContext.ApplicationUsers
            .AsNoTracking()
            .CountAsync(
                user => user.IsActive && !user.IsLocked && !user.IsRemoved,
                cancellationToken);
        var activeRoleCount = await dbContext.ApplicationRoles
            .AsNoTracking()
            .CountAsync(
                role => role.IsActive && !role.IsArchived,
                cancellationToken);
        var activeRequestTypeCount = await dbContext.RequestTypes
            .AsNoTracking()
            .CountAsync(
                requestType => !requestType.IsArchived,
                cancellationToken);

        return new AdministrationSummaryResponse(
            activeUserCount,
            activeRoleCount,
            PublishedWorkflowCount: 0,
            activeRequestTypeCount);
    }
}
