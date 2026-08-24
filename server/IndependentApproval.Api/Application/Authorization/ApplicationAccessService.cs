using System.Security.Claims;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IndependentApproval.Api.Application.Authorization;

public sealed class ApplicationAccessService(
    IndependentApprovalDbContext dbContext,
    IOptions<ApplicationAuthorizationOptions> authorizationOptions,
    IHttpContextAccessor httpContextAccessor) : IApplicationAccessService
{
    private const string HttpContextCacheKey =
        "IndependentApproval.ApplicationAccessSnapshot";

    private readonly HashSet<string> _systemAdministratorAccounts =
        authorizationOptions.Value.SystemAdministratorAccounts
            .Select(AccountNameNormalizer.Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<ApplicationAccessSnapshot> ResolveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var identity = principal.Identity;
        var accountName = identity?.Name?.Trim() ?? string.Empty;
        var normalizedAccountName = AccountNameNormalizer.Normalize(accountName);
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext?.Items[HttpContextCacheKey] is ApplicationAccessSnapshot cached
            && string.Equals(
                AccountNameNormalizer.Normalize(cached.AccountName),
                normalizedAccountName,
                StringComparison.Ordinal))
        {
            return cached;
        }

        var (domain, userName) = AccountNameNormalizer.Parse(accountName);

        if (identity?.IsAuthenticated != true || normalizedAccountName.Length == 0)
        {
            return Cache(new ApplicationAccessSnapshot(
                null,
                accountName,
                domain,
                userName,
                GetDisplayName(null, userName, accountName),
                ApplicationAccessStates.Unknown,
                false,
                false,
                [],
                []));
        }

        if (IsSystemAdministrator(normalizedAccountName))
        {
            var bootstrapUser = await dbContext.ApplicationUsers
                .AsNoTracking()
                .Where(user => user.NormalizedAccountName == normalizedAccountName)
                .Select(user => new UserProjection(
                    user.Id,
                    user.DisplayName,
                    user.SamAccountName))
                .SingleOrDefaultAsync(cancellationToken);

            return Cache(new ApplicationAccessSnapshot(
                bootstrapUser?.Id,
                accountName,
                domain,
                userName,
                GetDisplayName(
                    bootstrapUser?.DisplayName,
                    bootstrapUser?.SamAccountName ?? userName,
                    accountName),
                ApplicationAccessStates.Granted,
                true,
                true,
                [],
                PermissionCatalog.All.Select(permission => permission.Code).ToArray()));
        }

        var applicationUser = await dbContext.ApplicationUsers
            .AsNoTracking()
            .Where(user => user.NormalizedAccountName == normalizedAccountName)
            .Select(user => new ApplicationUserProjection(
                user.Id,
                user.DisplayName,
                user.SamAccountName,
                user.IsActive,
                user.IsLocked,
                user.IsRemoved))
            .SingleOrDefaultAsync(cancellationToken);

        if (applicationUser is null)
        {
            return Cache(new ApplicationAccessSnapshot(
                null,
                accountName,
                domain,
                userName,
                GetDisplayName(null, userName, accountName),
                ApplicationAccessStates.Unknown,
                false,
                false,
                [],
                []));
        }

        var roleCodes = await dbContext.ApplicationUserRoles
            .AsNoTracking()
            .Where(assignment =>
                assignment.ApplicationUserId == applicationUser.Id
                && assignment.RemovedAtUtc == null
                && assignment.ApplicationRole.IsActive
                && !assignment.ApplicationRole.IsArchived)
            .Select(assignment => assignment.ApplicationRole.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);

        var permissionCodes = await dbContext.ApplicationUserRoles
            .AsNoTracking()
            .Where(assignment =>
                assignment.ApplicationUserId == applicationUser.Id
                && assignment.RemovedAtUtc == null
                && assignment.ApplicationRole.IsActive
                && !assignment.ApplicationRole.IsArchived)
            .SelectMany(assignment => assignment.ApplicationRole.PermissionAssignments)
            .Where(assignment => assignment.RemovedAtUtc == null)
            .Select(assignment => assignment.Permission.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);

        var accessState = GetAccessState(applicationUser, permissionCodes);
        var hasApplicationAccess = accessState == ApplicationAccessStates.Granted;

        return Cache(new ApplicationAccessSnapshot(
            applicationUser.Id,
            accountName,
            domain,
            userName,
            GetDisplayName(applicationUser.DisplayName, applicationUser.SamAccountName, accountName),
            accessState,
            hasApplicationAccess,
            false,
            roleCodes,
            permissionCodes));
    }

    public bool IsSystemAdministrator(string? accountName) =>
        _systemAdministratorAccounts.Contains(AccountNameNormalizer.Normalize(accountName));

    public async Task RecordSuccessfulAccessAsync(
        Guid applicationUserId,
        CancellationToken cancellationToken)
    {
        await dbContext.ApplicationUsers
            .Where(user => user.Id == applicationUserId)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(
                    user => user.LastSuccessfulAccessAtUtc,
                    DateTimeOffset.UtcNow),
                cancellationToken);
    }

    private ApplicationAccessSnapshot Cache(ApplicationAccessSnapshot snapshot)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            httpContext.Items[HttpContextCacheKey] = snapshot;
        }

        return snapshot;
    }

    private static string GetAccessState(
        ApplicationUserProjection user,
        IReadOnlyCollection<string> permissionCodes)
    {
        if (user.IsRemoved)
        {
            return ApplicationAccessStates.Removed;
        }

        if (user.IsLocked)
        {
            return ApplicationAccessStates.Locked;
        }

        if (!user.IsActive)
        {
            return ApplicationAccessStates.Inactive;
        }

        return permissionCodes.Contains(PermissionCodes.AccessApplication, StringComparer.Ordinal)
            ? ApplicationAccessStates.Granted
            : ApplicationAccessStates.MissingAccessPermission;
    }

    private static string GetDisplayName(
        string? applicationDisplayName,
        string? userName,
        string accountName)
    {
        if (!string.IsNullOrWhiteSpace(applicationDisplayName))
        {
            return applicationDisplayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            return userName.Trim();
        }

        return accountName;
    }

    private sealed record UserProjection(
        Guid Id,
        string? DisplayName,
        string SamAccountName);

    private sealed record ApplicationUserProjection(
        Guid Id,
        string? DisplayName,
        string SamAccountName,
        bool IsActive,
        bool IsLocked,
        bool IsRemoved);
}
