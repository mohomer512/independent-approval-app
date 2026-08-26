using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Contracts.Common;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Domain.Workflows;
using IndependentApproval.Api.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IndependentApproval.Api.Application.Administration;

public sealed partial class WorkflowAdministrationService : IWorkflowAdministrationService
{
    private const int MaximumSearchLength = 200;

    private readonly IndependentApprovalDbContext dbContext;
    private readonly IAdministrationActorAccessor actorAccessor;

    public WorkflowAdministrationService(
        IndependentApprovalDbContext dbContext,
        IAdministrationActorAccessor actorAccessor)
    {
        this.dbContext = dbContext;
        this.actorAccessor = actorAccessor;
    }

    public async Task<PagedResponse<WorkflowListItemResponse>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        AdministrationEncoding.ValidatePagination(page, pageSize);
        var normalizedSearch = NormalizeSearch(search);
        var query = dbContext.WorkflowDefinitions
            .AsNoTracking()
            .Where(workflow => includeArchived || !workflow.IsArchived);

        if (normalizedSearch is not null)
        {
            query = query.Where(workflow =>
                workflow.Code.Contains(normalizedSearch)
                || workflow.NormalizedCode.Contains(normalizedSearch)
                || workflow.Versions.Any(version =>
                    version.NameEnglish.Contains(normalizedSearch)
                    || version.NameArabic.Contains(normalizedSearch)
                    || version.NavigationSlug.Contains(normalizedSearch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (long)(page - 1) * pageSize;

        if (skip >= totalCount)
        {
            return new PagedResponse<WorkflowListItemResponse>(
                [],
                page,
                pageSize,
                totalCount);
        }

        var roots = await query
            .OrderBy(workflow => workflow.Code)
            .ThenBy(workflow => workflow.Id)
            .Skip((int)skip)
            .Take(pageSize)
            .Select(workflow => new WorkflowRootProjection(
                workflow.Id,
                workflow.Code,
                workflow.IsArchived,
                workflow.CreatedAtUtc,
                workflow.CreatedByAccount,
                workflow.ModifiedAtUtc,
                workflow.ModifiedByAccount,
                workflow.ArchivedAtUtc,
                workflow.ArchivedByAccount,
                workflow.RowVersion))
            .ToArrayAsync(cancellationToken);
        var summaries = await GetVersionSummariesAsync(
            roots.Select(root => root.Id).ToArray(),
            cancellationToken);
        var items = roots.Select(root =>
        {
            var versions = summaries.GetValueOrDefault(root.Id) ?? [];
            var displayVersion = versions
                .OrderBy(version => DisplayLifecycleOrder(version.Lifecycle))
                .ThenByDescending(version => version.VersionNumber)
                .FirstOrDefault();

            return new WorkflowListItemResponse(
                root.Id,
                root.Code,
                root.IsArchived,
                displayVersion,
                versions.FirstOrDefault(version =>
                    version.Lifecycle == "draft")?.Id,
                versions
                    .Where(version => version.Lifecycle == "published")
                    .OrderByDescending(version => version.VersionNumber)
                    .Select(version => (Guid?)version.Id)
                    .FirstOrDefault(),
                versions.Count,
                root.CreatedAtUtc,
                root.CreatedByAccount,
                root.ModifiedAtUtc,
                root.ModifiedByAccount,
                root.ArchivedAtUtc,
                root.ArchivedByAccount,
                AdministrationEncoding.EncodeRowVersion(root.RowVersion));
        }).ToArray();

        return new PagedResponse<WorkflowListItemResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<WorkflowDetailResponse> GetAsync(
        Guid workflowId,
        CancellationToken cancellationToken)
    {
        var root = await dbContext.WorkflowDefinitions
            .AsNoTracking()
            .Where(workflow => workflow.Id == workflowId)
            .Select(workflow => new WorkflowRootProjection(
                workflow.Id,
                workflow.Code,
                workflow.IsArchived,
                workflow.CreatedAtUtc,
                workflow.CreatedByAccount,
                workflow.ModifiedAtUtc,
                workflow.ModifiedByAccount,
                workflow.ArchivedAtUtc,
                workflow.ArchivedByAccount,
                workflow.RowVersion))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The workflow does not exist.");
        var versions = await VersionGraphQuery(tracking: false)
            .Where(version => version.WorkflowDefinitionId == workflowId)
            .OrderByDescending(version => version.VersionNumber)
            .ToArrayAsync(cancellationToken);
        var requestCounts = await GetRequestCountsAsync(
            versions.Select(version => version.Id),
            cancellationToken);

        return CreateDetailResponse(root, versions, requestCounts);
    }

    public async Task<WorkflowVersionResponse> GetVersionAsync(
        Guid workflowId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var version = await VersionGraphQuery(tracking: false)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == versionId
                             && candidate.WorkflowDefinitionId == workflowId,
                cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The workflow version does not exist.");
        var requestCount = await dbContext.ApprovalRequests
            .AsNoTracking()
            .CountAsync(
                request => request.WorkflowVersionId == versionId,
                cancellationToken);
        return CreateVersionResponse(version, requestCount);
    }

    public async Task<WorkflowOptionsResponse> GetOptionsAsync(
        CancellationToken cancellationToken) =>
        await CreateOptionsResponseAsync(cancellationToken);

    public async Task<WorkflowDetailResponse> CreateAsync(
        CreateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var values = WorkflowDefinitionValidator.ValidateCreate(request);
        await EnsureUniqueCodeAsync(values.Code, cancellationToken);
        await EnsureEligibleRequestTypeVersionAsync(
            values.Version.RequestTypeVersionId,
            cancellationToken);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var root = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Code = values.Code,
            NormalizedCode = values.Code,
            IsArchived = false,
            CreatedAtUtc = now,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        dbContext.WorkflowDefinitions.Add(root);
        await ReserveSlugAsync(
            root.Id,
            values.Version.NavigationSlug,
            actor,
            now,
            cancellationToken);
        dbContext.WorkflowVersions.Add(CreateVersion(
            root.Id,
            versionNumber: 1,
            values.Version,
            actor,
            now));
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetAsync(root.Id, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> UpdateVersionAsync(
        Guid workflowId,
        Guid versionId,
        UpdateWorkflowVersionRequest request,
        CancellationToken cancellationToken)
    {
        var values = WorkflowDefinitionValidator.ValidateVersion(request);
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.RowVersion,
            cancellationToken);
        await EnsureEligibleRequestTypeVersionAsync(
            values.RequestTypeVersionId,
            cancellationToken);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (mutable.Version.RequestTypeVersionId != values.RequestTypeVersionId)
        {
            await RebindAndUpdateVersionAsync(
                mutable,
                values,
                actor,
                now,
                cancellationToken);
        }
        else
        {
            await ReserveSlugAsync(
                workflowId,
                values.NavigationSlug,
                actor,
                now,
                cancellationToken);
            ApplyVersionValues(mutable.Version, values);
            SetModified(mutable.Version, actor, now);
            SetModified(mutable.Root, actor, now);
            await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        }

        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> ReplaceStarterRolesAsync(
        Guid workflowId,
        Guid versionId,
        ReplaceWorkflowStarterRolesRequest request,
        CancellationToken cancellationToken)
    {
        var roleIds = WorkflowDefinitionValidator.ValidateRoleIds(request.RoleIds);
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        await EnsureAssignableRolesAsync(roleIds, cancellationToken);
        var requestedRoleIds = roleIds.ToHashSet();
        var existingRoleIds = mutable.Version.StarterRoles
            .Select(assignment => assignment.ApplicationRoleId)
            .ToHashSet();
        dbContext.WorkflowVersionStarterRoles.RemoveRange(
            mutable.Version.StarterRoles.Where(assignment =>
                !requestedRoleIds.Contains(assignment.ApplicationRoleId)));

        foreach (var roleId in roleIds.Where(roleId => !existingRoleIds.Contains(roleId)))
        {
            dbContext.WorkflowVersionStarterRoles.Add(
                new WorkflowVersionStarterRole
                {
                    WorkflowVersionId = versionId,
                    ApplicationRoleId = roleId
                });
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.Root, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> AddStepAsync(
        Guid workflowId,
        Guid versionId,
        CreateWorkflowStepRequest request,
        CancellationToken cancellationToken)
    {
        var values = WorkflowDefinitionValidator.ValidateStep(request);
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        await EnsureAssignableRolesAsync(values.RoleIds, cancellationToken);
        EnsureUniqueStep(mutable.Version, values, excludedStepId: null);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var step = new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowVersionId = versionId,
            Key = values.Key,
            NormalizedKey = values.NormalizedKey,
            CreatedAtUtc = now,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        ApplyStepValues(step, values);
        dbContext.WorkflowSteps.Add(step);

        foreach (var roleId in values.RoleIds)
        {
            dbContext.WorkflowStepRoles.Add(new WorkflowStepRole
            {
                WorkflowStepId = step.Id,
                WorkflowVersionId = versionId,
                ApplicationRoleId = roleId
            });
        }

        SetModified(mutable.Version, actor, now);
        SetModified(mutable.Root, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> UpdateStepAsync(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        UpdateWorkflowStepRequest request,
        CancellationToken cancellationToken)
    {
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        var step = GetStep(mutable.Version, stepId, request.StepRowVersion);
        var values = WorkflowDefinitionValidator.ValidateStep(step.Key, request);
        EnsureUniqueStep(mutable.Version, values, stepId);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        ApplyStepValues(step, values);
        SetModified(step, actor, now);
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.Root, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> DeleteStepAsync(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        DeleteWorkflowStepRequest request,
        CancellationToken cancellationToken)
    {
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        var step = GetStep(mutable.Version, stepId, request.StepRowVersion);
        dbContext.WorkflowTransitions.RemoveRange(
            mutable.Version.Transitions.Where(transition =>
                transition.SourceStepId == stepId
                || transition.TargetStepId == stepId));
        dbContext.WorkflowStepFieldPermissions.RemoveRange(step.FieldPermissions);
        dbContext.WorkflowStepRoles.RemoveRange(step.AssignedRoles);
        dbContext.WorkflowSteps.Remove(step);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.Root, actor, now);
        await SaveChangesAsync(cancellationToken);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> ReplaceStepRolesAsync(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        ReplaceWorkflowStepRolesRequest request,
        CancellationToken cancellationToken)
    {
        var roleIds = WorkflowDefinitionValidator.ValidateRoleIds(request.RoleIds);
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        var step = GetStep(mutable.Version, stepId, request.StepRowVersion);
        await EnsureAssignableRolesAsync(roleIds, cancellationToken);
        var requestedRoleIds = roleIds.ToHashSet();
        var existingRoleIds = step.AssignedRoles
            .Select(assignment => assignment.ApplicationRoleId)
            .ToHashSet();
        dbContext.WorkflowStepRoles.RemoveRange(
            step.AssignedRoles.Where(assignment =>
                !requestedRoleIds.Contains(assignment.ApplicationRoleId)));

        foreach (var roleId in roleIds.Where(roleId => !existingRoleIds.Contains(roleId)))
        {
            dbContext.WorkflowStepRoles.Add(new WorkflowStepRole
            {
                WorkflowStepId = stepId,
                WorkflowVersionId = versionId,
                ApplicationRoleId = roleId
            });
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        SetModified(step, actor, now);
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.Root, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> ReplaceStepFieldPermissionsAsync(
        Guid workflowId,
        Guid versionId,
        Guid stepId,
        ReplaceWorkflowStepFieldPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var values = WorkflowDefinitionValidator.ValidatePermissions(request.Permissions);
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        var step = GetStep(mutable.Version, stepId, request.StepRowVersion);
        ValidatePermissionFields(mutable.Version, values);
        var existingByField = step.FieldPermissions.ToDictionary(
            permission => permission.RequestFieldDefinitionId);
        var requestedFieldIds = values
            .Select(value => value.RequestFieldDefinitionId)
            .ToHashSet();
        dbContext.WorkflowStepFieldPermissions.RemoveRange(
            step.FieldPermissions.Where(permission =>
                !requestedFieldIds.Contains(permission.RequestFieldDefinitionId)));
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var value in values)
        {
            if (existingByField.TryGetValue(
                    value.RequestFieldDefinitionId,
                    out var permission))
            {
                EnsureRowVersion(
                    permission.RowVersion,
                    AdministrationEncoding.DecodeRowVersion(value.RowVersion));
                permission.Access = value.Access;
                permission.DocumentAccess = value.DocumentAccess;
                permission.CanSelectForForwarding = value.CanSelectForForwarding;
                SetModified(permission, actor, now);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(value.RowVersion))
                {
                    throw AdministrationValidationException.For(
                        "permissions.rowVersion",
                        "A new field permission cannot have a row version.");
                }

                dbContext.WorkflowStepFieldPermissions.Add(
                    new WorkflowStepFieldPermission
                    {
                        Id = Guid.NewGuid(),
                        WorkflowVersionId = versionId,
                        RequestTypeVersionId = mutable.Version.RequestTypeVersionId,
                        WorkflowStepId = stepId,
                        RequestFieldDefinitionId = value.RequestFieldDefinitionId,
                        Access = value.Access,
                        DocumentAccess = value.DocumentAccess,
                        CanSelectForForwarding = value.CanSelectForForwarding,
                        CreatedAtUtc = now,
                        CreatedByAccount = actor.AccountName,
                        CreatedByUserId = actor.ApplicationUserId
                    });
            }
        }

        SetModified(step, actor, now);
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.Root, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> AddTransitionAsync(
        Guid workflowId,
        Guid versionId,
        CreateWorkflowTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var values = WorkflowDefinitionValidator.ValidateTransition(request);
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        EnsureTransitionSteps(mutable.Version, values);
        EnsureUniqueTransition(mutable.Version, values, excludedTransitionId: null);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var transition = new WorkflowTransition
        {
            Id = Guid.NewGuid(),
            WorkflowVersionId = versionId,
            Key = values.Key,
            NormalizedKey = values.NormalizedKey,
            CreatedAtUtc = now,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        ApplyTransitionValues(transition, values);
        dbContext.WorkflowTransitions.Add(transition);
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.Root, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> UpdateTransitionAsync(
        Guid workflowId,
        Guid versionId,
        Guid transitionId,
        UpdateWorkflowTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        var transition = GetTransition(
            mutable.Version,
            transitionId,
            request.TransitionRowVersion);
        var values = WorkflowDefinitionValidator.ValidateTransition(
            transition.Key,
            request);
        EnsureTransitionSteps(mutable.Version, values);
        EnsureUniqueTransition(mutable.Version, values, transitionId);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        ApplyTransitionValues(transition, values);
        SetModified(transition, actor, now);
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.Root, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> DeleteTransitionAsync(
        Guid workflowId,
        Guid versionId,
        Guid transitionId,
        DeleteWorkflowTransitionRequest request,
        CancellationToken cancellationToken)
    {
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        var transition = GetTransition(
            mutable.Version,
            transitionId,
            request.TransitionRowVersion);
        dbContext.WorkflowTransitions.Remove(transition);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.Root, actor, now);
        await SaveChangesAsync(cancellationToken);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowValidationResponse> ValidateAsync(
        Guid workflowId,
        Guid versionId,
        ValidateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var version = await VersionGraphQuery(tracking: false)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == versionId
                             && candidate.WorkflowDefinitionId == workflowId,
                cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The workflow version does not exist.");
        EnsureRowVersion(
            version.RowVersion,
            AdministrationEncoding.DecodeRowVersion(request.VersionRowVersion));
        var issues = WorkflowPublishingValidator.Validate(version);
        return new WorkflowValidationResponse(issues.Count == 0, issues);
    }

    public async Task<WorkflowVersionResponse> PublishAsync(
        Guid workflowId,
        Guid versionId,
        WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var mutable = await GetMutableVersionAsync(
            workflowId,
            versionId,
            request.RowVersion,
            cancellationToken);
        var issues = WorkflowPublishingValidator.Validate(mutable.Version);

        if (issues.Count > 0)
        {
            throw WorkflowValidationException(issues);
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await ReserveSlugAsync(
            workflowId,
            mutable.Version.NavigationSlug,
            actor,
            now,
            cancellationToken);
        mutable.Version.Lifecycle = WorkflowVersionLifecycle.Published;
        mutable.Version.PublishedAtUtc = now;
        mutable.Version.PublishedByAccount = actor.AccountName;
        mutable.Version.PublishedByUserId = actor.ApplicationUserId;
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.Root, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> CloneAsync(
        Guid workflowId,
        Guid versionId,
        WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = AdministrationEncoding.DecodeRowVersion(request.RowVersion);
        var source = await VersionGraphQuery(tracking: true)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == versionId
                             && candidate.WorkflowDefinitionId == workflowId,
                cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The workflow version does not exist.");
        EnsureRowVersion(source.RowVersion, expectedRowVersion);

        if (source.WorkflowDefinition.IsArchived)
        {
            throw WorkflowArchivedConflict();
        }

        if (source.Lifecycle == WorkflowVersionLifecycle.Draft)
        {
            throw new AdministrationConflictException(
                "administration.workflow_clone_source_invalid",
                "Only published or archived workflow versions can be cloned.");
        }

        if (await dbContext.WorkflowVersions.AnyAsync(
                version => version.WorkflowDefinitionId == workflowId
                           && version.Lifecycle == WorkflowVersionLifecycle.Draft,
                cancellationToken))
        {
            throw DraftExistsConflict();
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await ReserveSlugAsync(
            workflowId,
            source.NavigationSlug,
            actor,
            now,
            cancellationToken);
        var nextVersionNumber = await dbContext.WorkflowVersions
            .Where(version => version.WorkflowDefinitionId == workflowId)
            .MaxAsync(version => version.VersionNumber, cancellationToken) + 1;
        var clone = new WorkflowVersion
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            RequestTypeVersionId = source.RequestTypeVersionId,
            VersionNumber = nextVersionNumber,
            NameEnglish = source.NameEnglish,
            NameArabic = source.NameArabic,
            DescriptionEnglish = source.DescriptionEnglish,
            DescriptionArabic = source.DescriptionArabic,
            NavigationLabelEnglish = source.NavigationLabelEnglish,
            NavigationLabelArabic = source.NavigationLabelArabic,
            NavigationSlug = source.NavigationSlug,
            NavigationOrder = source.NavigationOrder,
            Lifecycle = WorkflowVersionLifecycle.Draft,
            CreatedAtUtc = now,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        dbContext.WorkflowVersions.Add(clone);
        CloneGraph(source, clone, actor, now);
        SetModified(source.WorkflowDefinition, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(workflowId, clone.Id, cancellationToken);
    }

    public async Task<WorkflowVersionResponse> ArchiveVersionAsync(
        Guid workflowId,
        Guid versionId,
        WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = AdministrationEncoding.DecodeRowVersion(request.RowVersion);
        var version = await dbContext.WorkflowVersions
            .Include(candidate => candidate.WorkflowDefinition)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == versionId
                             && candidate.WorkflowDefinitionId == workflowId,
                cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The workflow version does not exist.");
        EnsureRowVersion(version.RowVersion, expectedRowVersion);

        if (version.WorkflowDefinition.IsArchived)
        {
            throw WorkflowArchivedConflict();
        }

        if (version.Lifecycle == WorkflowVersionLifecycle.Archived)
        {
            throw new AdministrationConflictException(
                "administration.workflow_version_archived",
                "The workflow version is already archived.");
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        version.Lifecycle = WorkflowVersionLifecycle.Archived;
        version.ArchivedAtUtc = now;
        version.ArchivedByAccount = actor.AccountName;
        version.ArchivedByUserId = actor.ApplicationUserId;
        SetModified(version, actor, now);
        SetModified(version.WorkflowDefinition, actor, now);
        await SaveChangesAsync(cancellationToken);
        return await GetVersionAsync(workflowId, versionId, cancellationToken);
    }

    public async Task<WorkflowDetailResponse> ArchiveAsync(
        Guid workflowId,
        WorkflowConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = AdministrationEncoding.DecodeRowVersion(request.RowVersion);
        var root = await dbContext.WorkflowDefinitions
            .SingleOrDefaultAsync(
                workflow => workflow.Id == workflowId,
                cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The workflow does not exist.");
        EnsureRowVersion(root.RowVersion, expectedRowVersion);

        if (root.IsArchived)
        {
            throw WorkflowArchivedConflict();
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        root.IsArchived = true;
        root.ArchivedAtUtc = now;
        root.ArchivedByAccount = actor.AccountName;
        root.ArchivedByUserId = actor.ApplicationUserId;
        SetModified(root, actor, now);
        await SaveChangesAsync(cancellationToken);
        return await GetAsync(workflowId, cancellationToken);
    }

    private async Task RebindAndUpdateVersionAsync(
        MutableWorkflowVersion mutable,
        WorkflowVersionValues values,
        AdministrationActor actor,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var workflowId = mutable.Root.Id;
        var versionId = mutable.Version.Id;
        var expectedRowVersion = AdministrationEncoding.EncodeRowVersion(
            mutable.Version.RowVersion);
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        dbContext.ChangeTracker.Clear();

        await executionStrategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken);
            var current = await GetMutableVersionAsync(
                workflowId,
                versionId,
                expectedRowVersion,
                cancellationToken);
            await ReserveSlugAsync(
                workflowId,
                values.NavigationSlug,
                actor,
                timestamp,
                cancellationToken);
            dbContext.WorkflowStepFieldPermissions.RemoveRange(
                current.Version.FieldPermissions);
            ApplyVersionValues(
                current.Version,
                values,
                includeRequestTypeBinding: false);
            SetModified(current.Version, actor, timestamp);
            SetModified(current.Root, actor, timestamp);
            await SaveChangesAsync(cancellationToken, duplicatePossible: true);
            var rowVersionAfterPermissionClear = current.Version.RowVersion;
            var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE [app].[WorkflowVersions]
                 SET [RequestTypeVersionId] = {values.RequestTypeVersionId}
                 WHERE [Id] = {versionId}
                   AND [RowVersion] = {rowVersionAfterPermissionClear}
                 """,
                cancellationToken);

            if (affected != 1)
            {
                throw ConcurrencyConflict();
            }

            await transaction.CommitAsync(cancellationToken);
        });

        dbContext.ChangeTracker.Clear();
    }

    private async Task<MutableWorkflowVersion> GetMutableVersionAsync(
        Guid workflowId,
        Guid versionId,
        string? encodedRowVersion,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = AdministrationEncoding.DecodeRowVersion(encodedRowVersion);
        var version = await VersionGraphQuery(tracking: true)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == versionId
                             && candidate.WorkflowDefinitionId == workflowId,
                cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The workflow version does not exist.");
        EnsureRowVersion(version.RowVersion, expectedRowVersion);

        if (await dbContext.ApprovalRequests
                .AsNoTracking()
                .AnyAsync(
                    request => request.WorkflowVersionId == versionId,
                    cancellationToken))
        {
            throw WorkflowInUseConflict();
        }

        if (version.WorkflowDefinition.IsArchived)
        {
            throw WorkflowArchivedConflict();
        }

        if (version.Lifecycle != WorkflowVersionLifecycle.Draft)
        {
            throw WorkflowVersionImmutableConflict();
        }

        return new MutableWorkflowVersion(version.WorkflowDefinition, version);
    }

    private async Task EnsureUniqueCodeAsync(
        string normalizedCode,
        CancellationToken cancellationToken)
    {
        if (await dbContext.WorkflowDefinitions
                .AsNoTracking()
                .AnyAsync(
                    workflow => workflow.NormalizedCode == normalizedCode,
                    cancellationToken))
        {
            throw DuplicateCodeConflict();
        }
    }

    private async Task EnsureEligibleRequestTypeVersionAsync(
        Guid requestTypeVersionId,
        CancellationToken cancellationToken)
    {
        var eligible = await dbContext.RequestTypeVersions
            .AsNoTracking()
            .AnyAsync(
                version => version.Id == requestTypeVersionId
                           && version.Lifecycle == RequestTypeVersionLifecycle.Published
                           && !version.RequestType.IsArchived,
                cancellationToken);

        if (!eligible)
        {
            throw AdministrationValidationException.For(
                "requestTypeVersionId",
                "Select a published request-type version whose request type is not archived.");
        }
    }

    private async Task EnsureAssignableRolesAsync(
        IReadOnlyList<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return;
        }

        var activeRoleCount = await dbContext.ApplicationRoles
            .AsNoTracking()
            .CountAsync(
                role => roleIds.Contains(role.Id)
                        && role.IsActive
                        && !role.IsArchived,
                cancellationToken);

        if (activeRoleCount != roleIds.Count)
        {
            throw AdministrationValidationException.For(
                "roleIds",
                "Every assigned role must be active and not archived.");
        }
    }

    private async Task ReserveSlugAsync(
        Guid workflowId,
        string navigationSlug,
        AdministrationActor actor,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var reservation = await dbContext.WorkflowSlugReservations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.NormalizedSlug == navigationSlug,
                cancellationToken);

        if (reservation is not null
            && reservation.WorkflowDefinitionId != workflowId)
        {
            throw DuplicateSlugConflict();
        }

        if (reservation is null)
        {
            dbContext.WorkflowSlugReservations.Add(
                new WorkflowSlugReservation
                {
                    NormalizedSlug = navigationSlug,
                    WorkflowDefinitionId = workflowId,
                    ReservedAtUtc = timestamp,
                    ReservedByAccount = actor.AccountName,
                    ReservedByUserId = actor.ApplicationUserId
                });
        }
    }

    private async Task SaveChangesAsync(
        CancellationToken cancellationToken,
        bool duplicatePossible = false)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ConcurrencyConflict();
        }
        catch (DbUpdateException exception)
            when (duplicatePossible
                  && exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            var message = exception.InnerException.Message;

            if (message.Contains(
                    "WorkflowSlugReservations",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw DuplicateSlugConflict();
            }

            if (message.Contains(
                    "WorkflowDefinitions",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw DuplicateCodeConflict();
            }

            if (message.Contains(
                    "WorkflowDefinitionId_Draft",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw DraftExistsConflict();
            }

            throw new AdministrationConflictException(
                "administration.workflow_definition_conflict",
                "The workflow definition conflicts with an existing draft, key, sort order or assignment. Reload and try again.");
        }
    }

    private static AdministrationValidationException WorkflowValidationException(
        IEnumerable<WorkflowValidationIssueResponse> issues) =>
        new(issues
            .GroupBy(issue => issue.Path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(issue => $"[{issue.Code}] {issue.Message}")
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase));

    private static AdministrationConflictException ConcurrencyConflict() =>
        new(
            "administration.concurrency_conflict",
            "The resource was changed by another administrator. Reload it and try again.");

    private static AdministrationConflictException DuplicateCodeConflict() =>
        new(
            "administration.duplicate_workflow_code",
            "A workflow with the same normalized code already exists.");

    private static AdministrationConflictException DuplicateSlugConflict() =>
        new(
            "administration.duplicate_workflow_navigation_slug",
            "Another workflow already owns this navigation slug.");

    private static AdministrationConflictException DraftExistsConflict() =>
        new(
            "administration.workflow_draft_exists",
            "This workflow already has a draft version.");

    private static AdministrationConflictException WorkflowVersionImmutableConflict() =>
        new(
            "administration.workflow_version_immutable",
            "Only an unused draft workflow version can be edited.");

    private static AdministrationConflictException WorkflowInUseConflict() =>
        new(
            "administration.workflow_in_use",
            "A workflow version referenced by a request is permanently immutable.");

    private static AdministrationConflictException WorkflowArchivedConflict() =>
        new(
            "administration.workflow_archived",
            "An archived workflow cannot be changed.");

    private static string? NormalizeSearch(string? search)
    {
        var normalized = search?.Trim();

        if (normalized?.Length > MaximumSearchLength)
        {
            throw AdministrationValidationException.For(
                "search",
                $"Search text cannot exceed {MaximumSearchLength} characters.");
        }

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private sealed record MutableWorkflowVersion(
        WorkflowDefinition Root,
        WorkflowVersion Version);
}
