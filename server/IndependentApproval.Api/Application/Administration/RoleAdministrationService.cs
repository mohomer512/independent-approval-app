using IndependentApproval.Api.Contracts.Administration.Roles;
using IndependentApproval.Api.Contracts.Common;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IndependentApproval.Api.Application.Administration;

public sealed class RoleAdministrationService(
    IndependentApprovalDbContext dbContext,
    IAdministrationActorAccessor actorAccessor) : IRoleAdministrationService
{
    private const int MaximumCodeLength = 100;
    private const int MaximumNameLength = 200;
    private const int MaximumDescriptionLength = 2000;
    private const int MaximumSearchLength = 200;

    private static readonly IReadOnlyDictionary<string, PermissionDefinition>
        PermissionsByCode = PermissionCatalog.All.ToDictionary(
            permission => permission.Code,
            StringComparer.OrdinalIgnoreCase);

    public async Task<PagedResponse<ApplicationRoleResponse>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        AdministrationEncoding.ValidatePagination(page, pageSize);
        var normalizedSearch = NormalizeSearch(search);
        var query = dbContext.ApplicationRoles
            .AsNoTracking()
            .Where(role => includeArchived || !role.IsArchived);

        if (normalizedSearch is not null)
        {
            query = query.Where(role =>
                role.Code.Contains(normalizedSearch)
                || role.NormalizedCode.Contains(normalizedSearch)
                || role.NameEnglish.Contains(normalizedSearch)
                || role.NameArabic.Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (long)(page - 1) * pageSize;

        if (skip >= totalCount)
        {
            return new PagedResponse<ApplicationRoleResponse>([], page, pageSize, totalCount);
        }

        var roles = await query
            .OrderBy(role => role.Code)
            .ThenBy(role => role.Id)
            .Skip((int)skip)
            .Take(pageSize)
            .Select(role => new RoleProjection(
                role.Id,
                role.Code,
                role.NameEnglish,
                role.NameArabic,
                role.DescriptionEnglish,
                role.DescriptionArabic,
                role.IsActive,
                role.IsArchived,
                role.CreatedAtUtc,
                role.CreatedByAccount,
                role.ModifiedAtUtc,
                role.ModifiedByAccount,
                role.RowVersion))
            .ToListAsync(cancellationToken);
        var details = await GetRoleDetailsAsync(
            roles.Select(role => role.Id).ToArray(),
            cancellationToken);
        var responses = roles
            .Select(role => CreateResponse(
                role,
                details.GetValueOrDefault(role.Id)))
            .ToArray();

        return new PagedResponse<ApplicationRoleResponse>(
            responses,
            page,
            pageSize,
            totalCount);
    }

    public async Task<ApplicationRoleResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var role = await GetProjectionAsync(id, cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The application role does not exist.");
        var details = await GetRoleDetailsAsync([id], cancellationToken);

        return CreateResponse(role, details.GetValueOrDefault(id));
    }

    public IReadOnlyList<PermissionResponse> ListPermissions() =>
        PermissionCatalog.All
            .Select(permission => new PermissionResponse(
                permission.Code,
                permission.NameEnglish,
                permission.NameArabic,
                permission.DescriptionEnglish,
                permission.DescriptionArabic))
            .ToArray();

    public async Task<ApplicationRoleResponse> CreateAsync(
        CreateApplicationRoleRequest request,
        CancellationToken cancellationToken)
    {
        var values = ValidateRoleValues(
            request.Code,
            request.NameEnglish,
            request.NameArabic,
            request.DescriptionEnglish,
            request.DescriptionArabic,
            request.PermissionCodes,
            requireCode: true);
        var normalizedCode = NormalizeCode(values.Code!);

        if (await dbContext.ApplicationRoles.AnyAsync(
                role => role.NormalizedCode == normalizedCode,
                cancellationToken))
        {
            throw DuplicateCodeConflict();
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            NormalizedCode = normalizedCode,
            NameEnglish = values.NameEnglish,
            NameArabic = values.NameArabic,
            DescriptionEnglish = values.DescriptionEnglish,
            DescriptionArabic = values.DescriptionArabic,
            IsActive = true,
            IsArchived = false,
            CreatedAtUtc = now,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        dbContext.ApplicationRoles.Add(role);
        AddPermissionAssignments(role.Id, values.PermissionCodes, actor, now);

        await SaveChangesAsync(cancellationToken, duplicateCodePossible: true);
        return await GetAsync(role.Id, cancellationToken);
    }

    public async Task<ApplicationRoleResponse> UpdateAsync(
        Guid id,
        UpdateApplicationRoleRequest request,
        CancellationToken cancellationToken)
    {
        var values = ValidateRoleValues(
            null,
            request.NameEnglish,
            request.NameArabic,
            request.DescriptionEnglish,
            request.DescriptionArabic,
            request.PermissionCodes,
            requireCode: false);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var role = await GetMutableRoleAsync(id, request.RowVersion, cancellationToken);

        if (role.IsArchived)
        {
            throw new AdministrationConflictException(
                "administration.role_archived",
                "An archived role cannot be changed.");
        }

        var now = DateTimeOffset.UtcNow;
        role.NameEnglish = values.NameEnglish;
        role.NameArabic = values.NameArabic;
        role.DescriptionEnglish = values.DescriptionEnglish;
        role.DescriptionArabic = values.DescriptionArabic;
        role.IsActive = request.IsActive;
        role.ModifiedAtUtc = now;
        role.ModifiedByAccount = actor.AccountName;
        role.ModifiedByUserId = actor.ApplicationUserId;

        await ReconcilePermissionAssignmentsAsync(
            role.Id,
            values.PermissionCodes,
            actor,
            now,
            cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<ApplicationRoleResponse> ArchiveAsync(
        Guid id,
        ApplicationRoleConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var role = await GetMutableRoleAsync(id, request.RowVersion, cancellationToken);

        if (role.IsArchived)
        {
            throw new AdministrationConflictException(
                "administration.role_archived",
                "The role is already archived.");
        }

        var now = DateTimeOffset.UtcNow;
        role.IsActive = false;
        role.IsArchived = true;
        role.ArchivedAtUtc = now;
        role.ArchivedByAccount = actor.AccountName;
        role.ArchivedByUserId = actor.ApplicationUserId;
        role.ModifiedAtUtc = now;
        role.ModifiedByAccount = actor.AccountName;
        role.ModifiedByUserId = actor.ApplicationUserId;

        await SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private async Task<ApplicationRole> GetMutableRoleAsync(
        Guid id,
        string? encodedRowVersion,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = AdministrationEncoding.DecodeRowVersion(encodedRowVersion);
        var role = await dbContext.ApplicationRoles
            .SingleOrDefaultAsync(role => role.Id == id, cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The application role does not exist.");
        dbContext.Entry(role).Property(entity => entity.RowVersion).OriginalValue =
            expectedRowVersion;
        return role;
    }

    private async Task ReconcilePermissionAssignmentsAsync(
        Guid roleId,
        IReadOnlyCollection<string> permissionCodes,
        AdministrationActor actor,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var targetPermissionIds = permissionCodes
            .Select(code => PermissionsByCode[code].Id)
            .ToHashSet();
        var assignments = await dbContext.RolePermissions
            .Where(assignment => assignment.ApplicationRoleId == roleId)
            .ToListAsync(cancellationToken);

        foreach (var assignment in assignments)
        {
            var shouldBeGranted = targetPermissionIds.Contains(assignment.PermissionId);

            if (shouldBeGranted && assignment.RemovedAtUtc is not null)
            {
                assignment.GrantedAtUtc = timestamp;
                assignment.GrantedByAccount = actor.AccountName;
                assignment.GrantedByUserId = actor.ApplicationUserId;
                assignment.RemovedAtUtc = null;
                assignment.RemovedByAccount = null;
                assignment.RemovedByUserId = null;
            }
            else if (!shouldBeGranted && assignment.RemovedAtUtc is null)
            {
                assignment.RemovedAtUtc = timestamp;
                assignment.RemovedByAccount = actor.AccountName;
                assignment.RemovedByUserId = actor.ApplicationUserId;
            }
        }

        var existingPermissionIds = assignments
            .Select(assignment => assignment.PermissionId)
            .ToHashSet();

        foreach (var permissionId in targetPermissionIds
                     .Where(permissionId => !existingPermissionIds.Contains(permissionId)))
        {
            dbContext.RolePermissions.Add(CreatePermissionAssignment(
                roleId,
                permissionId,
                actor,
                timestamp));
        }
    }

    private void AddPermissionAssignments(
        Guid roleId,
        IReadOnlyCollection<string> permissionCodes,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        foreach (var permissionCode in permissionCodes)
        {
            dbContext.RolePermissions.Add(CreatePermissionAssignment(
                roleId,
                PermissionsByCode[permissionCode].Id,
                actor,
                timestamp));
        }
    }

    private static RolePermission CreatePermissionAssignment(
        Guid roleId,
        Guid permissionId,
        AdministrationActor actor,
        DateTimeOffset timestamp) =>
        new()
        {
            Id = Guid.NewGuid(),
            ApplicationRoleId = roleId,
            PermissionId = permissionId,
            GrantedAtUtc = timestamp,
            GrantedByAccount = actor.AccountName,
            GrantedByUserId = actor.ApplicationUserId
        };

    private async Task<RoleProjection?> GetProjectionAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await dbContext.ApplicationRoles
            .AsNoTracking()
            .Where(role => role.Id == id)
            .Select(role => new RoleProjection(
                role.Id,
                role.Code,
                role.NameEnglish,
                role.NameArabic,
                role.DescriptionEnglish,
                role.DescriptionArabic,
                role.IsActive,
                role.IsArchived,
                role.CreatedAtUtc,
                role.CreatedByAccount,
                role.ModifiedAtUtc,
                role.ModifiedByAccount,
                role.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<Dictionary<Guid, RoleDetails>> GetRoleDetailsAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        var permissionRows = await dbContext.RolePermissions
            .AsNoTracking()
            .Where(assignment =>
                roleIds.Contains(assignment.ApplicationRoleId)
                && assignment.RemovedAtUtc == null)
            .Select(assignment => new
            {
                assignment.ApplicationRoleId,
                assignment.Permission.Code
            })
            .ToListAsync(cancellationToken);
        var userCounts = await dbContext.ApplicationUserRoles
            .AsNoTracking()
            .Where(assignment =>
                roleIds.Contains(assignment.ApplicationRoleId)
                && assignment.RemovedAtUtc == null)
            .GroupBy(assignment => assignment.ApplicationRoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.RoleId, row => row.Count, cancellationToken);

        return roleIds.ToDictionary(
            roleId => roleId,
            roleId => new RoleDetails(
                permissionRows
                    .Where(row => row.ApplicationRoleId == roleId)
                    .Select(row => row.Code)
                    .OrderBy(code => code)
                    .ToArray(),
                userCounts.GetValueOrDefault(roleId)));
    }

    private static ApplicationRoleResponse CreateResponse(
        RoleProjection role,
        RoleDetails? details) =>
        new(
            role.Id,
            role.Code,
            role.NameEnglish,
            role.NameArabic,
            role.DescriptionEnglish,
            role.DescriptionArabic,
            role.IsActive,
            role.IsArchived,
            details?.PermissionCodes ?? [],
            details?.UserCount ?? 0,
            role.CreatedAtUtc,
            role.CreatedByAccount,
            role.ModifiedAtUtc,
            role.ModifiedByAccount,
            AdministrationEncoding.EncodeRowVersion(role.RowVersion));

    private static RoleValues ValidateRoleValues(
        string? code,
        string? nameEnglish,
        string? nameArabic,
        string? descriptionEnglish,
        string? descriptionArabic,
        IReadOnlyList<string>? permissionCodes,
        bool requireCode)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var normalizedCode = code?.Trim();
        var normalizedNameEnglish = nameEnglish?.Trim();
        var normalizedNameArabic = nameArabic?.Trim();
        var normalizedDescriptionEnglish = NormalizeOptional(descriptionEnglish);
        var normalizedDescriptionArabic = NormalizeOptional(descriptionArabic);

        if (requireCode && string.IsNullOrWhiteSpace(normalizedCode))
        {
            errors["code"] = ["A role code is required."];
        }
        else if (normalizedCode is not null && !IsValidCode(normalizedCode))
        {
            errors["code"] =
            [
                $"Role code must be 1 to {MaximumCodeLength} ASCII letters, numbers, dots, hyphens or underscores."
            ];
        }

        ValidateRequiredText(
            errors,
            "nameEnglish",
            normalizedNameEnglish,
            "An English role name is required.",
            MaximumNameLength);
        ValidateRequiredText(
            errors,
            "nameArabic",
            normalizedNameArabic,
            "An Arabic role name is required.",
            MaximumNameLength);
        ValidateOptionalText(
            errors,
            "descriptionEnglish",
            normalizedDescriptionEnglish,
            MaximumDescriptionLength);
        ValidateOptionalText(
            errors,
            "descriptionArabic",
            normalizedDescriptionArabic,
            MaximumDescriptionLength);

        if (permissionCodes is null)
        {
            errors["permissionCodes"] = ["Permission codes are required."];
        }

        var normalizedPermissionCodes = permissionCodes?
            .Select(NormalizeCode)
            .ToArray() ?? [];

        if (normalizedPermissionCodes.Distinct(StringComparer.Ordinal).Count()
            != normalizedPermissionCodes.Length)
        {
            errors["permissionCodes"] = ["Permission codes must be unique."];
        }
        else if (normalizedPermissionCodes.Any(codeValue =>
                     !PermissionsByCode.ContainsKey(codeValue)))
        {
            errors["permissionCodes"] =
                ["One or more permission codes are not in the fixed permission catalogue."];
        }

        if (errors.Count > 0)
        {
            throw new AdministrationValidationException(errors);
        }

        return new RoleValues(
            normalizedCode,
            normalizedNameEnglish!,
            normalizedNameArabic!,
            normalizedDescriptionEnglish,
            normalizedDescriptionArabic,
            normalizedPermissionCodes);
    }

    private static void ValidateRequiredText(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        string requiredMessage,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [requiredMessage];
        }
        else if (value.Length > maximumLength)
        {
            errors[field] = [$"The value cannot exceed {maximumLength} characters."];
        }
    }

    private static void ValidateOptionalText(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        int maximumLength)
    {
        if (value?.Length > maximumLength)
        {
            errors[field] = [$"The value cannot exceed {maximumLength} characters."];
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeCode(string code) =>
        code.Trim().ToUpperInvariant();

    private static bool IsValidCode(string code)
    {
        if (code.Length is < 1 or > MaximumCodeLength
            || !IsAsciiLetterOrDigit(code[0]))
        {
            return false;
        }

        return code.All(character =>
            IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_');
    }

    private static bool IsAsciiLetterOrDigit(char character) =>
        character is >= 'A' and <= 'Z'
        or >= 'a' and <= 'z'
        or >= '0' and <= '9';

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

    private async Task SaveChangesAsync(
        CancellationToken cancellationToken,
        bool duplicateCodePossible = false)
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
            when (duplicateCodePossible
                  && exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw DuplicateCodeConflict();
        }
    }

    private static AdministrationConflictException DuplicateCodeConflict() =>
        new(
            "administration.duplicate_role_code",
            "A role with the same normalized code already exists.");

    private sealed record RoleValues(
        string? Code,
        string NameEnglish,
        string NameArabic,
        string? DescriptionEnglish,
        string? DescriptionArabic,
        IReadOnlyList<string> PermissionCodes);

    private sealed record RoleProjection(
        Guid Id,
        string Code,
        string NameEnglish,
        string NameArabic,
        string? DescriptionEnglish,
        string? DescriptionArabic,
        bool IsActive,
        bool IsArchived,
        DateTimeOffset CreatedAtUtc,
        string CreatedByAccount,
        DateTimeOffset? ModifiedAtUtc,
        string? ModifiedByAccount,
        byte[] RowVersion);

    private sealed record RoleDetails(
        IReadOnlyList<string> PermissionCodes,
        int UserCount);
}
