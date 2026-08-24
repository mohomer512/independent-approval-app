using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Application.Directory;
using IndependentApproval.Api.Contracts.Administration.Users;
using IndependentApproval.Api.Contracts.Common;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IndependentApproval.Api.Application.Administration;

public sealed class ApplicationUserAdministrationService(
    IndependentApprovalDbContext dbContext,
    IAdministrationActorAccessor actorAccessor,
    IApplicationAccessService applicationAccessService,
    IDirectoryService directoryService,
    IDirectorySelectionTokenService selectionTokenService) :
    IApplicationUserAdministrationService
{
    private const int MaximumSearchLength = 200;
    private const int MaximumLockReasonLength = 1000;

    public async Task<ApplicationUserResponse> AddAsync(
        AddApplicationUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!selectionTokenService.TryRead(
                request.SelectionToken,
                out var selectionReference)
            || selectionReference is null)
        {
            throw new AdministrationBadRequestException(
                "directory.invalid_selection_token",
                "The directory selection token is invalid or has expired. Search again and reselect the user.");
        }

        var roleIds = ValidateRoleIds(request.RoleIds);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var directoryUser = await FindBySidAsync(
            selectionReference.Sid,
            cancellationToken);

        if (directoryUser is null)
        {
            throw new AdministrationConflictException(
                "directory.user_not_found",
                "The selected directory user no longer exists or is no longer available.");
        }

        if (!directoryUser.Sid.SequenceEqual(selectionReference.Sid)
            || selectionReference.ObjectGuid is Guid selectedObjectGuid
            && directoryUser.ObjectGuid != selectedObjectGuid)
        {
            throw new AdministrationConflictException(
                "directory.identity_mismatch",
                "The selected directory identity changed. Search again and reselect the user.");
        }

        var profile = ValidateDirectoryProfile(directoryUser);
        await EnsureNoDuplicateUserAsync(
            profile,
            excludedUserId: null,
            cancellationToken);
        var roles = await GetAssignableRolesAsync(roleIds, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            AdSid = [.. profile.Sid],
            AdObjectGuid = profile.ObjectGuid,
            AccountName = profile.AccountName,
            Domain = profile.Domain,
            SamAccountName = profile.SamAccountName,
            UserPrincipalName = profile.UserPrincipalName,
            NormalizedAccountName = profile.NormalizedAccountName,
            DisplayName = profile.DisplayName,
            Email = profile.Email,
            IsActive = true,
            IsLocked = false,
            IsRemoved = false,
            CreatedAtUtc = now,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        dbContext.ApplicationUsers.Add(user);

        foreach (var role in roles)
        {
            dbContext.ApplicationUserRoles.Add(new ApplicationUserRole
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                ApplicationRoleId = role.Id,
                AssignedAtUtc = now,
                AssignedByAccount = actor.AccountName,
                AssignedByUserId = actor.ApplicationUserId
            });
        }

        await SaveChangesAsync(cancellationToken, duplicateUserPossible: true);
        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task<PagedResponse<ApplicationUserResponse>> ListAsync(
        string? search,
        bool includeRemoved,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        AdministrationEncoding.ValidatePagination(page, pageSize);
        var normalizedSearch = NormalizeSearch(search);
        var query = dbContext.ApplicationUsers
            .AsNoTracking()
            .Where(user => includeRemoved || !user.IsRemoved);

        if (normalizedSearch is not null)
        {
            query = query.Where(user =>
                user.AccountName.Contains(normalizedSearch)
                || user.NormalizedAccountName.Contains(normalizedSearch)
                || (user.DisplayName != null && user.DisplayName.Contains(normalizedSearch))
                || (user.Email != null && user.Email.Contains(normalizedSearch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (long)(page - 1) * pageSize;

        if (skip >= totalCount)
        {
            return new PagedResponse<ApplicationUserResponse>([], page, pageSize, totalCount);
        }

        var users = await query
            .OrderBy(user => user.DisplayName ?? user.AccountName)
            .ThenBy(user => user.AccountName)
            .ThenBy(user => user.Id)
            .Skip((int)skip)
            .Take(pageSize)
            .Select(user => new UserProjection(
                user.Id,
                user.AccountName,
                user.Domain,
                user.SamAccountName,
                user.UserPrincipalName,
                user.Email,
                user.DisplayName,
                user.IsActive,
                user.IsLocked,
                user.LockReason,
                user.IsRemoved,
                user.LastSuccessfulAccessAtUtc,
                user.CreatedAtUtc,
                user.CreatedByAccount,
                user.ModifiedAtUtc,
                user.ModifiedByAccount,
                user.LockedAtUtc,
                user.LockedByAccount,
                user.RemovedAtUtc,
                user.RemovedByAccount,
                user.RowVersion))
            .ToListAsync(cancellationToken);

        var rolesByUserId = await GetRolesByUserIdAsync(
            users.Select(user => user.Id).ToArray(),
            cancellationToken);
        var responses = users
            .Select(user => CreateResponse(
                user,
                rolesByUserId.GetValueOrDefault(user.Id) ?? []))
            .ToArray();

        return new PagedResponse<ApplicationUserResponse>(
            responses,
            page,
            pageSize,
            totalCount);
    }

    public async Task<ApplicationUserResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await GetProjectionAsync(id, cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The application user does not exist.");
        var rolesByUserId = await GetRolesByUserIdAsync([id], cancellationToken);

        return CreateResponse(user, rolesByUserId.GetValueOrDefault(id) ?? []);
    }

    public async Task<ApplicationUserResponse> RefreshDirectoryProfileAsync(
        Guid id,
        ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var user = await GetMutableUserAsync(id, request.RowVersion, cancellationToken);
        var directoryUser = await FindBySidAsync(user.AdSid, cancellationToken);

        if (directoryUser is null)
        {
            throw new AdministrationConflictException(
                "directory.user_not_found",
                "The application user's immutable SID could not be found in the configured directory.");
        }

        if (!directoryUser.Sid.SequenceEqual(user.AdSid)
            || user.AdObjectGuid is Guid storedObjectGuid
            && directoryUser.ObjectGuid != storedObjectGuid)
        {
            throw new AdministrationConflictException(
                "directory.identity_mismatch",
                "The directory identity does not match the stored immutable identity.");
        }

        var profile = ValidateDirectoryProfile(directoryUser);
        await EnsureNoDuplicateUserAsync(profile, user.Id, cancellationToken);
        user.AdObjectGuid ??= profile.ObjectGuid;
        user.AccountName = profile.AccountName;
        user.Domain = profile.Domain;
        user.SamAccountName = profile.SamAccountName;
        user.UserPrincipalName = profile.UserPrincipalName;
        user.NormalizedAccountName = profile.NormalizedAccountName;
        user.DisplayName = profile.DisplayName;
        user.Email = profile.Email;
        SetModified(user, actor, DateTimeOffset.UtcNow);

        await SaveChangesAsync(cancellationToken, duplicateUserPossible: true);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<ApplicationUserResponse> UpdateRolesAsync(
        Guid id,
        UpdateApplicationUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RoleIds is null)
        {
            throw AdministrationValidationException.For(
                "roleIds",
                "Role identifiers are required.");
        }

        var roleIds = request.RoleIds.Distinct().ToArray();

        if (roleIds.Length != request.RoleIds.Count)
        {
            throw AdministrationValidationException.For(
                "roleIds",
                "Role identifiers must be unique.");
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var user = await GetMutableUserAsync(id, request.RowVersion, cancellationToken);

        if (user.IsRemoved)
        {
            throw new AdministrationConflictException(
                "administration.user_removed",
                "Restore the application user before changing role assignments.");
        }

        var roles = await dbContext.ApplicationRoles
            .Where(role => roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);

        if (roles.Count != roleIds.Length)
        {
            throw AdministrationValidationException.For(
                "roleIds",
                "One or more roles do not exist.");
        }

        if (roles.Any(role => role.IsArchived || !role.IsActive))
        {
            throw new AdministrationConflictException(
                "administration.role_unavailable",
                "Archived or inactive roles cannot be assigned.");
        }

        var assignments = await dbContext.ApplicationUserRoles
            .Where(assignment => assignment.ApplicationUserId == id)
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var assignment in assignments)
        {
            var shouldBeAssigned = roleIds.Contains(assignment.ApplicationRoleId);

            if (shouldBeAssigned && assignment.RemovedAtUtc is not null)
            {
                assignment.AssignedAtUtc = now;
                assignment.AssignedByAccount = actor.AccountName;
                assignment.AssignedByUserId = actor.ApplicationUserId;
                assignment.RemovedAtUtc = null;
                assignment.RemovedByAccount = null;
                assignment.RemovedByUserId = null;
            }
            else if (!shouldBeAssigned && assignment.RemovedAtUtc is null)
            {
                assignment.RemovedAtUtc = now;
                assignment.RemovedByAccount = actor.AccountName;
                assignment.RemovedByUserId = actor.ApplicationUserId;
            }
        }

        var existingRoleIds = assignments
            .Select(assignment => assignment.ApplicationRoleId)
            .ToHashSet();

        foreach (var roleId in roleIds.Where(roleId => !existingRoleIds.Contains(roleId)))
        {
            dbContext.ApplicationUserRoles.Add(new ApplicationUserRole
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = id,
                ApplicationRoleId = roleId,
                AssignedAtUtc = now,
                AssignedByAccount = actor.AccountName,
                AssignedByUserId = actor.ApplicationUserId
            });
        }

        SetModified(user, actor, now);
        await SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<ApplicationUserResponse> LockAsync(
        Guid id,
        LockApplicationUserRequest request,
        CancellationToken cancellationToken)
    {
        var reason = request.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw AdministrationValidationException.For(
                "reason",
                "A lock reason is required.");
        }

        if (reason.Length > MaximumLockReasonLength)
        {
            throw AdministrationValidationException.For(
                "reason",
                $"The lock reason cannot exceed {MaximumLockReasonLength} characters.");
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var user = await GetMutableUserAsync(id, request.RowVersion, cancellationToken);

        if (user.IsRemoved)
        {
            throw new AdministrationConflictException(
                "administration.user_removed",
                "A removed application user cannot be locked.");
        }

        if (user.IsLocked)
        {
            throw new AdministrationConflictException(
                "administration.user_already_locked",
                "The application user is already locked.");
        }

        var now = DateTimeOffset.UtcNow;
        user.IsLocked = true;
        user.LockReason = reason;
        user.LockedAtUtc = now;
        user.LockedByAccount = actor.AccountName;
        user.LockedByUserId = actor.ApplicationUserId;
        SetModified(user, actor, now);

        await SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public Task<ApplicationUserResponse> UnlockAsync(
        Guid id,
        ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        ChangeStateAsync(id, request.RowVersion, UserStateChange.Unlock, cancellationToken);

    public Task<ApplicationUserResponse> RemoveAsync(
        Guid id,
        ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        ChangeStateAsync(id, request.RowVersion, UserStateChange.Remove, cancellationToken);

    public Task<ApplicationUserResponse> RestoreAsync(
        Guid id,
        ApplicationUserConcurrencyRequest request,
        CancellationToken cancellationToken) =>
        ChangeStateAsync(id, request.RowVersion, UserStateChange.Restore, cancellationToken);

    private async Task<ApplicationUserResponse> ChangeStateAsync(
        Guid id,
        string? encodedRowVersion,
        UserStateChange stateChange,
        CancellationToken cancellationToken)
    {
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var user = await GetMutableUserAsync(id, encodedRowVersion, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        switch (stateChange)
        {
            case UserStateChange.Unlock when !user.IsLocked:
                throw new AdministrationConflictException(
                    "administration.user_not_locked",
                    "The application user is not locked.");
            case UserStateChange.Unlock:
                user.IsLocked = false;
                user.LockReason = null;
                break;
            case UserStateChange.Remove when user.IsRemoved:
                throw new AdministrationConflictException(
                    "administration.user_already_removed",
                    "The application user has already been removed.");
            case UserStateChange.Remove:
                user.IsRemoved = true;
                user.RemovedAtUtc = now;
                user.RemovedByAccount = actor.AccountName;
                user.RemovedByUserId = actor.ApplicationUserId;
                break;
            case UserStateChange.Restore when !user.IsRemoved:
                throw new AdministrationConflictException(
                    "administration.user_not_removed",
                    "The application user is not removed.");
            case UserStateChange.Restore:
                user.IsRemoved = false;
                break;
        }

        SetModified(user, actor, now);
        await SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private async Task<ApplicationUser> GetMutableUserAsync(
        Guid id,
        string? encodedRowVersion,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = AdministrationEncoding.DecodeRowVersion(encodedRowVersion);
        var user = await dbContext.ApplicationUsers
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The application user does not exist.");

        if (applicationAccessService.IsSystemAdministrator(user.NormalizedAccountName))
        {
            throw new AdministrationConflictException(
                "administration.protected_bootstrap_administrator",
                "A bootstrap system administrator cannot be changed through the administration API.");
        }

        dbContext.Entry(user).Property(entity => entity.RowVersion).OriginalValue =
            expectedRowVersion;
        return user;
    }

    private async Task<DirectoryUser?> FindBySidAsync(
        byte[] sid,
        CancellationToken cancellationToken)
    {
        try
        {
            return await directoryService.FindBySidAsync(
                [.. sid],
                cancellationToken);
        }
        catch (DirectoryServiceUnavailableException)
        {
            throw new AdministrationServiceUnavailableException(
                "directory.unavailable",
                "The configured directory could not be queried. Try again later.");
        }
    }

    private async Task EnsureNoDuplicateUserAsync(
        DirectoryProfile profile,
        Guid? excludedUserId,
        CancellationToken cancellationToken)
    {
        var users = dbContext.ApplicationUsers.AsNoTracking();

        if (excludedUserId is Guid userId)
        {
            users = users.Where(user => user.Id != userId);
        }

        var duplicateSid = await users
            .AnyAsync(
                user => user.AdSid == profile.Sid,
                cancellationToken);
        var duplicateObjectGuid = profile.ObjectGuid is Guid objectGuid
            && await users
                .AnyAsync(
                    user => user.AdObjectGuid == objectGuid,
                    cancellationToken);
        var duplicateAccount = await users
            .AnyAsync(
                user => user.NormalizedAccountName == profile.NormalizedAccountName,
                cancellationToken);

        if (duplicateSid || duplicateObjectGuid || duplicateAccount)
        {
            throw DuplicateApplicationUserConflict();
        }
    }

    private async Task<IReadOnlyList<ApplicationRole>> GetAssignableRolesAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        var roles = await dbContext.ApplicationRoles
            .Where(role => roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);

        if (roles.Count != roleIds.Count)
        {
            throw AdministrationValidationException.For(
                "roleIds",
                "One or more roles do not exist.");
        }

        if (roles.Any(role => role.IsArchived || !role.IsActive))
        {
            throw new AdministrationConflictException(
                "administration.role_unavailable",
                "Archived or inactive roles cannot be assigned.");
        }

        return roles;
    }

    private static Guid[] ValidateRoleIds(IReadOnlyList<Guid>? requestedRoleIds)
    {
        if (requestedRoleIds is null)
        {
            throw AdministrationValidationException.For(
                "roleIds",
                "Role identifiers are required.");
        }

        var roleIds = requestedRoleIds.Distinct().ToArray();

        if (roleIds.Length == 0)
        {
            throw AdministrationValidationException.For(
                "roleIds",
                "At least one role is required.");
        }

        if (roleIds.Length != requestedRoleIds.Count)
        {
            throw AdministrationValidationException.For(
                "roleIds",
                "Role identifiers must be unique.");
        }

        return roleIds;
    }

    private static DirectoryProfile ValidateDirectoryProfile(DirectoryUser user)
    {
        var domain = user.Domain.Trim();
        var samAccountName = user.SamAccountName.Trim();

        if (user.Sid.Length is < 1 or > 68
            || string.IsNullOrWhiteSpace(domain)
            || domain.Length > 255
            || string.IsNullOrWhiteSpace(samAccountName)
            || samAccountName.Length > 256
            || samAccountName.Contains('\\')
            || user.UserPrincipalName?.Trim().Length > 320
            || user.DisplayName?.Trim().Length > 256
            || user.Email?.Trim().Length > 320)
        {
            throw new AdministrationServiceUnavailableException(
                "directory.invalid_result",
                "The configured directory returned an invalid user profile.");
        }

        var accountName = $"{domain}\\{samAccountName}";

        if (accountName.Length > 256)
        {
            throw new AdministrationServiceUnavailableException(
                "directory.invalid_result",
                "The configured directory returned an invalid user profile.");
        }

        return new DirectoryProfile(
            [.. user.Sid],
            user.ObjectGuid,
            accountName,
            domain,
            samAccountName,
            NormalizeOptional(user.UserPrincipalName),
            AccountNameNormalizer.Normalize(accountName),
            NormalizeOptional(user.DisplayName),
            NormalizeOptional(user.Email));
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private async Task<UserProjection?> GetProjectionAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await dbContext.ApplicationUsers
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new UserProjection(
                user.Id,
                user.AccountName,
                user.Domain,
                user.SamAccountName,
                user.UserPrincipalName,
                user.Email,
                user.DisplayName,
                user.IsActive,
                user.IsLocked,
                user.LockReason,
                user.IsRemoved,
                user.LastSuccessfulAccessAtUtc,
                user.CreatedAtUtc,
                user.CreatedByAccount,
                user.ModifiedAtUtc,
                user.ModifiedByAccount,
                user.LockedAtUtc,
                user.LockedByAccount,
                user.RemovedAtUtc,
                user.RemovedByAccount,
                user.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<Dictionary<Guid, IReadOnlyList<RoleReferenceResponse>>>
        GetRolesByUserIdAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        var assignments = await dbContext.ApplicationUserRoles
            .AsNoTracking()
            .Where(assignment =>
                userIds.Contains(assignment.ApplicationUserId)
                && assignment.RemovedAtUtc == null)
            .OrderBy(assignment => assignment.ApplicationRole.Code)
            .Select(assignment => new
            {
                assignment.ApplicationUserId,
                Role = new RoleReferenceResponse(
                    assignment.ApplicationRole.Id,
                    assignment.ApplicationRole.Code,
                    assignment.ApplicationRole.NameEnglish,
                    assignment.ApplicationRole.NameArabic,
                    assignment.ApplicationRole.IsArchived)
            })
            .ToListAsync(cancellationToken);

        return assignments
            .GroupBy(assignment => assignment.ApplicationUserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<RoleReferenceResponse>)group
                    .Select(assignment => assignment.Role)
                    .ToArray());
    }

    private ApplicationUserResponse CreateResponse(
        UserProjection user,
        IReadOnlyList<RoleReferenceResponse> roles) =>
        new(
            user.Id,
            user.AccountName,
            user.Domain,
            user.SamAccountName,
            user.UserPrincipalName,
            user.Email,
            string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.SamAccountName
                : user.DisplayName,
            user.IsActive,
            user.IsLocked,
            user.IsLocked ? user.LockReason : null,
            user.IsRemoved,
            applicationAccessService.IsSystemAdministrator(user.AccountName),
            roles,
            user.LastSuccessfulAccessAtUtc,
            user.CreatedAtUtc,
            user.CreatedByAccount,
            user.ModifiedAtUtc,
            user.ModifiedByAccount,
            user.LockedAtUtc,
            user.LockedByAccount,
            user.RemovedAtUtc,
            user.RemovedByAccount,
            AdministrationEncoding.EncodeRowVersion(user.RowVersion));

    private static string? NormalizeSearch(string? search)
    {
        var normalizedSearch = search?.Trim();

        if (normalizedSearch?.Length > MaximumSearchLength)
        {
            throw AdministrationValidationException.For(
                "search",
                $"Search text cannot exceed {MaximumSearchLength} characters.");
        }

        return string.IsNullOrWhiteSpace(normalizedSearch)
            ? null
            : normalizedSearch;
    }

    private static void SetModified(
        ApplicationUser user,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        user.ModifiedAtUtc = timestamp;
        user.ModifiedByAccount = actor.AccountName;
        user.ModifiedByUserId = actor.ApplicationUserId;
    }

    private async Task SaveChangesAsync(
        CancellationToken cancellationToken,
        bool duplicateUserPossible = false)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AdministrationConflictException(
                "administration.concurrency_conflict",
                "The resource was changed by another administrator. Reload it and try again.");
        }
        catch (DbUpdateException exception)
            when (duplicateUserPossible
                  && exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw DuplicateApplicationUserConflict();
        }
    }

    private static AdministrationConflictException DuplicateApplicationUserConflict() =>
        new(
            "administration.duplicate_application_user",
            "This directory identity already has an application user record. Restore the existing record if it was removed.");

    private enum UserStateChange
    {
        Unlock,
        Remove,
        Restore
    }

    private sealed record UserProjection(
        Guid Id,
        string AccountName,
        string Domain,
        string SamAccountName,
        string? UserPrincipalName,
        string? Email,
        string? DisplayName,
        bool IsActive,
        bool IsLocked,
        string? LockReason,
        bool IsRemoved,
        DateTimeOffset? LastSuccessfulAccessAtUtc,
        DateTimeOffset CreatedAtUtc,
        string CreatedByAccount,
        DateTimeOffset? ModifiedAtUtc,
        string? ModifiedByAccount,
        DateTimeOffset? LockedAtUtc,
        string? LockedByAccount,
        DateTimeOffset? RemovedAtUtc,
        string? RemovedByAccount,
        byte[] RowVersion);

    private sealed record DirectoryProfile(
        byte[] Sid,
        Guid? ObjectGuid,
        string AccountName,
        string Domain,
        string SamAccountName,
        string? UserPrincipalName,
        string NormalizedAccountName,
        string? DisplayName,
        string? Email);
}
