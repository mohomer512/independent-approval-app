using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace IndependentApproval.Api.Application.Administration;

public sealed partial class WorkflowAdministrationService
{
    private static readonly IReadOnlyList<WorkflowSystemFieldOptionResponse> SystemFields =
    [
        new(RequestSystemFieldKeys.RequestNumber, "Request number", "رقم الطلب", "shortText", "readOnly", null, false),
        new(RequestSystemFieldKeys.Title, "Title / subject", "العنوان / الموضوع", "shortText", "editable", null, false),
        new(RequestSystemFieldKeys.Status, "Status", "الحالة", "shortText", "readOnly", null, false),
        new(RequestSystemFieldKeys.RequestedBy, "Requested by", "مقدم الطلب", "applicationUser", "readOnly", null, false),
        new(RequestSystemFieldKeys.CreatedAtUtc, "Created at", "تاريخ الإنشاء", "dateTime", "readOnly", null, false),
        new(RequestSystemFieldKeys.ModifiedAtUtc, "Modified at", "تاريخ التعديل", "dateTime", "readOnly", null, false),
        new(RequestSystemFieldKeys.SubmittedAtUtc, "Submitted at", "تاريخ التقديم", "dateTime", "readOnly", null, false),
        new(RequestSystemFieldKeys.CompletedAtUtc, "Completed at", "تاريخ الإكمال", "dateTime", "readOnly", null, false),
        new(RequestSystemFieldKeys.Id, "Internal request identifier", "معرف الطلب الداخلي", "guid", "hidden", null, false),
        new(RequestSystemFieldKeys.RequestTypeId, "Request-type identifier", "معرف نوع الطلب", "guid", "hidden", null, false),
        new(RequestSystemFieldKeys.RequestTypeVersionId, "Request-type version identifier", "معرف إصدار نوع الطلب", "guid", "hidden", null, false),
        new(RequestSystemFieldKeys.WorkflowDefinitionId, "Workflow identifier", "معرف سير العمل", "guid", "hidden", null, false),
        new(RequestSystemFieldKeys.WorkflowVersionId, "Workflow version identifier", "معرف إصدار سير العمل", "guid", "hidden", null, false),
        new(RequestSystemFieldKeys.CurrentWorkflowStepId, "Current step identifier", "معرف الخطوة الحالية", "guid", "hidden", null, false),
        new(RequestSystemFieldKeys.RowVersion, "Concurrency token", "رمز التزامن", "binary", "hidden", null, false)
    ];

    private static readonly IReadOnlyList<WorkflowCatalogueOptionResponse> ActionTypes =
    [
        new("submit", "Submit", "تقديم"),
        new("approve", "Approve", "موافقة"),
        new("reject", "Reject", "رفض"),
        new("putOnHold", "Put on hold", "تعليق"),
        new("resume", "Resume", "استئناف"),
        new("requestMoreInformation", "Request more information", "طلب معلومات إضافية"),
        new("return", "Return", "إرجاع"),
        new("forward", "Forward", "إحالة"),
        new("complete", "Complete", "إكمال")
    ];

    private static readonly IReadOnlyList<WorkflowCatalogueOptionResponse> ResultingStatuses =
    [
        new("draft", "Draft", "مسودة"),
        new("submitted", "Submitted", "مقدم"),
        new("inProgress", "In progress", "قيد التنفيذ"),
        new("onHold", "On hold", "معلق"),
        new("moreInformationRequired", "More information required", "معلومات إضافية مطلوبة"),
        new("approved", "Approved", "موافق عليه"),
        new("rejected", "Rejected", "مرفوض"),
        new("completed", "Completed", "مكتمل"),
        new("cancelled", "Cancelled", "ملغى")
    ];

    private static readonly IReadOnlyList<WorkflowCatalogueOptionResponse> CommentPolicies =
    [
        new("none", "No comments", "بدون تعليقات"),
        new("optional", "Optional", "اختياري"),
        new("required", "Required", "مطلوب")
    ];

    private static readonly IReadOnlyList<WorkflowCatalogueOptionResponse> FieldAccessModes =
    [
        new("hidden", "Hidden", "مخفي"),
        new("readOnly", "Read only", "للقراءة فقط"),
        new("editable", "Editable", "قابل للتعديل"),
        new("editableRequired", "Editable and required", "قابل للتعديل ومطلوب")
    ];

    private static readonly IReadOnlyList<WorkflowCatalogueOptionResponse> DocumentAccessModes =
    [
        new("hidden", "Hidden", "مخفي"),
        new("view", "View", "عرض"),
        new("edit", "Edit", "تعديل")
    ];

    private static readonly IReadOnlyList<WorkflowCatalogueOptionResponse> TerminalOutcomes =
    [
        new("approved", "Approved", "موافق عليه"),
        new("rejected", "Rejected", "مرفوض"),
        new("completed", "Completed", "مكتمل")
    ];

    private IQueryable<WorkflowVersion> VersionGraphQuery(bool tracking)
    {
        IQueryable<WorkflowVersion> query = dbContext.WorkflowVersions
            .Include(version => version.WorkflowDefinition)
            .Include(version => version.RequestTypeVersion)
                .ThenInclude(requestTypeVersion => requestTypeVersion.RequestType)
            .Include(version => version.RequestTypeVersion)
                .ThenInclude(requestTypeVersion => requestTypeVersion.Fields)
            .Include(version => version.StarterRoles)
                .ThenInclude(assignment => assignment.ApplicationRole)
            .Include(version => version.Steps)
                .ThenInclude(step => step.AssignedRoles)
                    .ThenInclude(assignment => assignment.ApplicationRole)
            .Include(version => version.Steps)
                .ThenInclude(step => step.FieldPermissions)
                    .ThenInclude(permission => permission.RequestFieldDefinition)
            .Include(version => version.Transitions)
            .AsSplitQuery();

        return tracking ? query : query.AsNoTracking();
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<WorkflowVersionSummaryResponse>>>
        GetVersionSummariesAsync(
            IReadOnlyList<Guid> workflowIds,
            CancellationToken cancellationToken)
    {
        if (workflowIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<WorkflowVersionSummaryResponse>>();
        }

        var projections = await dbContext.WorkflowVersions
            .AsNoTracking()
            .Where(version => workflowIds.Contains(version.WorkflowDefinitionId))
            .Select(version => new WorkflowVersionSummaryProjection(
                version.Id,
                version.WorkflowDefinitionId,
                version.VersionNumber,
                version.RequestTypeVersion.RequestTypeId,
                version.RequestTypeVersionId,
                version.RequestTypeVersion.VersionNumber,
                version.RequestTypeVersion.RequestType.Code,
                version.NameEnglish,
                version.NameArabic,
                version.DescriptionEnglish,
                version.DescriptionArabic,
                version.NavigationLabelEnglish,
                version.NavigationLabelArabic,
                version.NavigationSlug,
                version.NavigationOrder,
                version.Lifecycle,
                version.Requests.Count,
                version.StarterRoles.Count,
                version.Steps.Count(step => step.IsActive),
                version.Transitions.Count(transition => transition.IsActive),
                version.CreatedAtUtc,
                version.CreatedByAccount,
                version.ModifiedAtUtc,
                version.ModifiedByAccount,
                version.PublishedAtUtc,
                version.PublishedByAccount,
                version.ArchivedAtUtc,
                version.ArchivedByAccount,
                version.RowVersion))
            .ToArrayAsync(cancellationToken);

        return projections
            .GroupBy(projection => projection.WorkflowDefinitionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WorkflowVersionSummaryResponse>)group
                    .OrderByDescending(projection => projection.VersionNumber)
                    .Select(CreateSummaryResponse)
                    .ToArray());
    }

    private async Task<IReadOnlyDictionary<Guid, int>> GetRequestCountsAsync(
        IEnumerable<Guid> versionIds,
        CancellationToken cancellationToken)
    {
        var ids = versionIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await dbContext.ApprovalRequests
            .AsNoTracking()
            .Where(request => ids.Contains(request.WorkflowVersionId))
            .GroupBy(request => request.WorkflowVersionId)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Count(),
                cancellationToken);
    }

    private async Task<WorkflowOptionsResponse> CreateOptionsResponseAsync(
        CancellationToken cancellationToken)
    {
        var roles = await dbContext.ApplicationRoles
            .AsNoTracking()
            .Where(role => role.IsActive && !role.IsArchived)
            .OrderBy(role => role.Code)
            .Select(role => new WorkflowRoleOptionResponse(
                role.Id,
                role.Code,
                role.NameEnglish,
                role.NameArabic))
            .ToArrayAsync(cancellationToken);
        var requestTypeVersions = await dbContext.RequestTypeVersions
            .AsNoTracking()
            .Include(version => version.RequestType)
            .Include(version => version.Fields)
            .Where(version =>
                version.Lifecycle == RequestTypeVersionLifecycle.Published
                && !version.RequestType.IsArchived)
            .OrderBy(version => version.RequestType.Code)
            .ThenByDescending(version => version.VersionNumber)
            .AsSplitQuery()
            .ToArrayAsync(cancellationToken);
        var requestTypeOptions = requestTypeVersions
            .Select(version => new WorkflowRequestTypeVersionOptionResponse(
                version.RequestTypeId,
                version.RequestType.Code,
                version.Id,
                version.VersionNumber,
                version.NameEnglish,
                version.NameArabic,
                version.Fields
                    .OrderBy(field => field.SortOrder)
                    .Select(field => new WorkflowRequestFieldOptionResponse(
                        field.Id,
                        field.Key,
                        field.LabelEnglish,
                        field.LabelArabic,
                        field.HelpTextEnglish,
                        field.HelpTextArabic,
                        FieldTypeToApi(field.FieldType),
                        field.IsRequired,
                        field.IsActive,
                        field.DocumentMode is null
                            ? null
                            : DocumentModeToApi(field.DocumentMode.Value)))
                    .ToArray()))
            .ToArray();

        return new WorkflowOptionsResponse(
            roles,
            requestTypeOptions,
            SystemFields,
            ActionTypes,
            ResultingStatuses,
            CommentPolicies,
            FieldAccessModes,
            DocumentAccessModes,
            TerminalOutcomes);
    }

    private static WorkflowDetailResponse CreateDetailResponse(
        WorkflowRootProjection root,
        IReadOnlyList<WorkflowVersion> versions,
        IReadOnlyDictionary<Guid, int> requestCounts) =>
        new(
            root.Id,
            root.Code,
            root.IsArchived,
            versions
                .OrderByDescending(version => version.VersionNumber)
                .Select(version => CreateVersionResponse(
                    version,
                    requestCounts.GetValueOrDefault(version.Id)))
                .ToArray(),
            root.CreatedAtUtc,
            root.CreatedByAccount,
            root.ModifiedAtUtc,
            root.ModifiedByAccount,
            root.ArchivedAtUtc,
            root.ArchivedByAccount,
            AdministrationEncoding.EncodeRowVersion(root.RowVersion));

    private static WorkflowVersionResponse CreateVersionResponse(
        WorkflowVersion version,
        int requestCount) =>
        new(
            version.Id,
            version.VersionNumber,
            version.RequestTypeVersion.RequestTypeId,
            version.RequestTypeVersionId,
            version.RequestTypeVersion.VersionNumber,
            version.RequestTypeVersion.RequestType.Code,
            version.RequestTypeVersion.Fields
                .OrderBy(field => field.SortOrder)
                .Select(field => new WorkflowRequestFieldOptionResponse(
                    field.Id,
                    field.Key,
                    field.LabelEnglish,
                    field.LabelArabic,
                    field.HelpTextEnglish,
                    field.HelpTextArabic,
                    FieldTypeToApi(field.FieldType),
                    field.IsRequired,
                    field.IsActive,
                    field.DocumentMode is null
                        ? null
                        : DocumentModeToApi(field.DocumentMode.Value)))
                .ToArray(),
            version.NameEnglish,
            version.NameArabic,
            version.DescriptionEnglish,
            version.DescriptionArabic,
            version.NavigationLabelEnglish,
            version.NavigationLabelArabic,
            version.NavigationSlug,
            version.NavigationOrder,
            LifecycleToApi(version.Lifecycle),
            requestCount > 0,
            requestCount,
            version.StarterRoles
                .OrderBy(assignment => assignment.ApplicationRole.Code)
                .Select(assignment => CreateRoleResponse(assignment.ApplicationRole))
                .ToArray(),
            version.Steps
                .OrderBy(step => step.SortOrder)
                .ThenBy(step => step.Key)
                .Select(CreateStepResponse)
                .ToArray(),
            version.Transitions
                .OrderBy(transition => transition.SourceStepId)
                .ThenBy(transition => transition.SortOrder)
                .ThenBy(transition => transition.Key)
                .Select(CreateTransitionResponse)
                .ToArray(),
            version.CreatedAtUtc,
            version.CreatedByAccount,
            version.ModifiedAtUtc,
            version.ModifiedByAccount,
            version.PublishedAtUtc,
            version.PublishedByAccount,
            version.ArchivedAtUtc,
            version.ArchivedByAccount,
            AdministrationEncoding.EncodeRowVersion(version.RowVersion));

    private static WorkflowVersionSummaryResponse CreateSummaryResponse(
        WorkflowVersionSummaryProjection projection) =>
        new(
            projection.Id,
            projection.VersionNumber,
            projection.RequestTypeId,
            projection.RequestTypeVersionId,
            projection.RequestTypeVersionNumber,
            projection.RequestTypeCode,
            projection.NameEnglish,
            projection.NameArabic,
            projection.DescriptionEnglish,
            projection.DescriptionArabic,
            projection.NavigationLabelEnglish,
            projection.NavigationLabelArabic,
            projection.NavigationSlug,
            projection.NavigationOrder,
            LifecycleToApi(projection.Lifecycle),
            projection.RequestCount > 0,
            projection.RequestCount,
            projection.StarterRoleCount,
            projection.ActiveStepCount,
            projection.ActiveTransitionCount,
            projection.CreatedAtUtc,
            projection.CreatedByAccount,
            projection.ModifiedAtUtc,
            projection.ModifiedByAccount,
            projection.PublishedAtUtc,
            projection.PublishedByAccount,
            projection.ArchivedAtUtc,
            projection.ArchivedByAccount,
            AdministrationEncoding.EncodeRowVersion(projection.RowVersion));

    private static WorkflowStepResponse CreateStepResponse(WorkflowStep step) =>
        new(
            step.Id,
            step.Key,
            step.NameEnglish,
            step.NameArabic,
            step.SortOrder,
            step.DiagramX,
            step.DiagramY,
            step.IsStartStep,
            step.IsActive,
            step.CanEditRequestData,
            step.CanOpenDocuments,
            step.CanEditDocuments,
            step.CanForwardDocuments,
            CommentPolicyToApi(step.CommentPolicy),
            step.AssignedRoles
                .OrderBy(assignment => assignment.ApplicationRole.Code)
                .Select(assignment => CreateRoleResponse(assignment.ApplicationRole))
                .ToArray(),
            step.FieldPermissions
                .OrderBy(permission => permission.RequestFieldDefinition.SortOrder)
                .Select(CreatePermissionResponse)
                .ToArray(),
            step.CreatedAtUtc,
            step.CreatedByAccount,
            step.ModifiedAtUtc,
            step.ModifiedByAccount,
            AdministrationEncoding.EncodeRowVersion(step.RowVersion));

    private static WorkflowTransitionResponse CreateTransitionResponse(
        WorkflowTransition transition) =>
        new(
            transition.Id,
            transition.Key,
            transition.SourceStepId,
            transition.TargetStepId,
            transition.ActionLabelEnglish,
            transition.ActionLabelArabic,
            ActionTypeToApi(transition.ActionType),
            ApprovalRequestStatusToApi(transition.ResultingStatus),
            transition.RequiresComment,
            transition.TerminalOutcome is null
                ? null
                : TerminalOutcomeToApi(transition.TerminalOutcome.Value),
            transition.SortOrder,
            transition.IsActive,
            transition.CreatedAtUtc,
            transition.CreatedByAccount,
            transition.ModifiedAtUtc,
            transition.ModifiedByAccount,
            AdministrationEncoding.EncodeRowVersion(transition.RowVersion));

    private static WorkflowStepFieldPermissionResponse CreatePermissionResponse(
        WorkflowStepFieldPermission permission) =>
        new(
            permission.Id,
            permission.RequestFieldDefinitionId,
            permission.RequestFieldDefinition.Key,
            FieldTypeToApi(permission.RequestFieldDefinition.FieldType),
            FieldAccessToApi(permission.Access),
            permission.DocumentAccess is null
                ? null
                : DocumentAccessToApi(permission.DocumentAccess.Value),
            permission.CanSelectForForwarding,
            permission.CreatedAtUtc,
            permission.CreatedByAccount,
            permission.ModifiedAtUtc,
            permission.ModifiedByAccount,
            AdministrationEncoding.EncodeRowVersion(permission.RowVersion));

    private static WorkflowRoleReferenceResponse CreateRoleResponse(
        ApplicationRole role) =>
        new(role.Id, role.Code, role.NameEnglish, role.NameArabic);

    private static WorkflowVersion CreateVersion(
        Guid workflowId,
        int versionNumber,
        WorkflowVersionValues values,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        var version = new WorkflowVersion
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            VersionNumber = versionNumber,
            Lifecycle = WorkflowVersionLifecycle.Draft,
            CreatedAtUtc = timestamp,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        ApplyVersionValues(version, values);
        return version;
    }

    private static void ApplyVersionValues(
        WorkflowVersion version,
        WorkflowVersionValues values,
        bool includeRequestTypeBinding = true)
    {
        if (includeRequestTypeBinding)
        {
            version.RequestTypeVersionId = values.RequestTypeVersionId;
        }

        version.NameEnglish = values.NameEnglish;
        version.NameArabic = values.NameArabic;
        version.DescriptionEnglish = values.DescriptionEnglish;
        version.DescriptionArabic = values.DescriptionArabic;
        version.NavigationLabelEnglish = values.NavigationLabelEnglish;
        version.NavigationLabelArabic = values.NavigationLabelArabic;
        version.NavigationSlug = values.NavigationSlug;
        version.NavigationOrder = values.NavigationOrder;
    }

    private static void ApplyStepValues(
        WorkflowStep step,
        WorkflowStepValues values)
    {
        step.NameEnglish = values.NameEnglish;
        step.NameArabic = values.NameArabic;
        step.SortOrder = values.SortOrder;
        step.DiagramX = values.DiagramX;
        step.DiagramY = values.DiagramY;
        step.IsStartStep = values.IsStartStep;
        step.IsActive = values.IsActive;
        step.CanEditRequestData = values.CanEditRequestData;
        step.CanOpenDocuments = values.CanOpenDocuments;
        step.CanEditDocuments = values.CanEditDocuments;
        step.CanForwardDocuments = values.CanForwardDocuments;
        step.CommentPolicy = values.CommentPolicy;
    }

    private static void ApplyTransitionValues(
        WorkflowTransition transition,
        WorkflowTransitionValues values)
    {
        transition.SourceStepId = values.SourceStepId;
        transition.TargetStepId = values.TargetStepId;
        transition.ActionLabelEnglish = values.ActionLabelEnglish;
        transition.ActionLabelArabic = values.ActionLabelArabic;
        transition.ActionType = values.ActionType;
        transition.ResultingStatus = values.ResultingStatus;
        transition.RequiresComment = values.RequiresComment;
        transition.TerminalOutcome = values.TerminalOutcome;
        transition.SortOrder = values.SortOrder;
        transition.IsActive = values.IsActive;
    }

    private static void EnsureUniqueStep(
        WorkflowVersion version,
        WorkflowStepValues values,
        Guid? excludedStepId)
    {
        var others = version.Steps.Where(step => step.Id != excludedStepId).ToArray();

        if (others.Any(step => step.NormalizedKey == values.NormalizedKey))
        {
            throw AdministrationValidationException.For(
                "key",
                "Step keys must be unique within a workflow version.");
        }

        if (others.Any(step => step.SortOrder == values.SortOrder))
        {
            throw AdministrationValidationException.For(
                "sortOrder",
                "Step sort order must be unique within a workflow version.");
        }

        if (values.IsStartStep && others.Any(step => step.IsStartStep))
        {
            throw AdministrationValidationException.For(
                "isStartStep",
                "Only one step can be marked as the start step.");
        }
    }

    private static void EnsureUniqueTransition(
        WorkflowVersion version,
        WorkflowTransitionValues values,
        Guid? excludedTransitionId)
    {
        var others = version.Transitions
            .Where(transition => transition.Id != excludedTransitionId)
            .ToArray();

        if (others.Any(transition =>
                transition.NormalizedKey == values.NormalizedKey))
        {
            throw AdministrationValidationException.For(
                "key",
                "Transition keys must be unique within a workflow version.");
        }

        if (others.Any(transition =>
                transition.SourceStepId == values.SourceStepId
                && transition.SortOrder == values.SortOrder))
        {
            throw AdministrationValidationException.For(
                "sortOrder",
                "Transition sort order must be unique for each source step.");
        }
    }

    private static void EnsureTransitionSteps(
        WorkflowVersion version,
        WorkflowTransitionValues values)
    {
        if (version.Steps.All(step => step.Id != values.SourceStepId))
        {
            throw AdministrationValidationException.For(
                "sourceStepId",
                "The source step must belong to this workflow version.");
        }

        if (values.TargetStepId is Guid targetStepId
            && version.Steps.All(step => step.Id != targetStepId))
        {
            throw AdministrationValidationException.For(
                "targetStepId",
                "The target step must belong to this workflow version.");
        }
    }

    private static WorkflowStep GetStep(
        WorkflowVersion version,
        Guid stepId,
        string? encodedRowVersion)
    {
        var step = version.Steps.SingleOrDefault(candidate => candidate.Id == stepId)
            ?? throw new AdministrationNotFoundException(
                "The workflow step does not exist.");
        EnsureRowVersion(
            step.RowVersion,
            AdministrationEncoding.DecodeRowVersion(encodedRowVersion));
        return step;
    }

    private static WorkflowTransition GetTransition(
        WorkflowVersion version,
        Guid transitionId,
        string? encodedRowVersion)
    {
        var transition = version.Transitions
            .SingleOrDefault(candidate => candidate.Id == transitionId)
            ?? throw new AdministrationNotFoundException(
                "The workflow transition does not exist.");
        EnsureRowVersion(
            transition.RowVersion,
            AdministrationEncoding.DecodeRowVersion(encodedRowVersion));
        return transition;
    }

    private static void ValidatePermissionFields(
        WorkflowVersion version,
        IReadOnlyList<WorkflowPermissionValues> values)
    {
        var boundFields = version.RequestTypeVersion.Fields
            .ToDictionary(field => field.Id);
        var requestedIds = values
            .Select(value => value.RequestFieldDefinitionId)
            .ToHashSet();

        if (!requestedIds.IsSubsetOf(boundFields.Keys))
        {
            throw AdministrationValidationException.For(
                "permissions",
                "Every permission must reference a custom field in the bound request-type version.");
        }

        foreach (var value in values)
        {
            var field = boundFields[value.RequestFieldDefinitionId];
            var isDocument = field.FieldType is RequestFieldType.FileDocument
                or RequestFieldType.RichDocument;

            if (!isDocument
                && (value.DocumentAccess is not null
                    || value.CanSelectForForwarding))
            {
                throw AdministrationValidationException.For(
                    "permissions",
                    $"Non-document field '{field.Key}' cannot have document access or forwarding selection.");
            }

            if (isDocument && value.DocumentAccess is null)
            {
                throw AdministrationValidationException.For(
                    "permissions",
                    $"Document field '{field.Key}' requires a document access mode.");
            }

            if (isDocument
                && value.CanSelectForForwarding
                && value.DocumentAccess == WorkflowDocumentAccess.Hidden)
            {
                throw AdministrationValidationException.For(
                    "permissions",
                    $"Hidden document field '{field.Key}' cannot be selected for forwarding.");
            }
        }
    }

    private void CloneGraph(
        WorkflowVersion source,
        WorkflowVersion clone,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        foreach (var assignment in source.StarterRoles)
        {
            dbContext.WorkflowVersionStarterRoles.Add(
                new WorkflowVersionStarterRole
                {
                    WorkflowVersionId = clone.Id,
                    ApplicationRoleId = assignment.ApplicationRoleId
                });
        }

        var stepIdMap = new Dictionary<Guid, Guid>();

        foreach (var sourceStep in source.Steps)
        {
            var stepId = Guid.NewGuid();
            stepIdMap[sourceStep.Id] = stepId;
            dbContext.WorkflowSteps.Add(new WorkflowStep
            {
                Id = stepId,
                WorkflowVersionId = clone.Id,
                Key = sourceStep.Key,
                NormalizedKey = sourceStep.NormalizedKey,
                NameEnglish = sourceStep.NameEnglish,
                NameArabic = sourceStep.NameArabic,
                SortOrder = sourceStep.SortOrder,
                DiagramX = sourceStep.DiagramX,
                DiagramY = sourceStep.DiagramY,
                IsStartStep = sourceStep.IsStartStep,
                IsActive = sourceStep.IsActive,
                CanEditRequestData = sourceStep.CanEditRequestData,
                CanOpenDocuments = sourceStep.CanOpenDocuments,
                CanEditDocuments = sourceStep.CanEditDocuments,
                CanForwardDocuments = sourceStep.CanForwardDocuments,
                CommentPolicy = sourceStep.CommentPolicy,
                CreatedAtUtc = timestamp,
                CreatedByAccount = actor.AccountName,
                CreatedByUserId = actor.ApplicationUserId
            });

            foreach (var role in sourceStep.AssignedRoles)
            {
                dbContext.WorkflowStepRoles.Add(new WorkflowStepRole
                {
                    WorkflowStepId = stepId,
                    WorkflowVersionId = clone.Id,
                    ApplicationRoleId = role.ApplicationRoleId
                });
            }

            foreach (var permission in sourceStep.FieldPermissions)
            {
                dbContext.WorkflowStepFieldPermissions.Add(
                    new WorkflowStepFieldPermission
                    {
                        Id = Guid.NewGuid(),
                        WorkflowVersionId = clone.Id,
                        RequestTypeVersionId = clone.RequestTypeVersionId,
                        WorkflowStepId = stepId,
                        RequestFieldDefinitionId = permission.RequestFieldDefinitionId,
                        Access = permission.Access,
                        DocumentAccess = permission.DocumentAccess,
                        CanSelectForForwarding = permission.CanSelectForForwarding,
                        CreatedAtUtc = timestamp,
                        CreatedByAccount = actor.AccountName,
                        CreatedByUserId = actor.ApplicationUserId
                    });
            }
        }

        foreach (var transition in source.Transitions)
        {
            dbContext.WorkflowTransitions.Add(new WorkflowTransition
            {
                Id = Guid.NewGuid(),
                WorkflowVersionId = clone.Id,
                Key = transition.Key,
                NormalizedKey = transition.NormalizedKey,
                SourceStepId = stepIdMap[transition.SourceStepId],
                TargetStepId = transition.TargetStepId is Guid targetId
                    ? stepIdMap[targetId]
                    : null,
                ActionLabelEnglish = transition.ActionLabelEnglish,
                ActionLabelArabic = transition.ActionLabelArabic,
                ActionType = transition.ActionType,
                ResultingStatus = transition.ResultingStatus,
                RequiresComment = transition.RequiresComment,
                TerminalOutcome = transition.TerminalOutcome,
                SortOrder = transition.SortOrder,
                IsActive = transition.IsActive,
                CreatedAtUtc = timestamp,
                CreatedByAccount = actor.AccountName,
                CreatedByUserId = actor.ApplicationUserId
            });
        }
    }

    private static void SetModified(
        WorkflowDefinition workflow,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        workflow.ModifiedAtUtc = timestamp;
        workflow.ModifiedByAccount = actor.AccountName;
        workflow.ModifiedByUserId = actor.ApplicationUserId;
    }

    private static void SetModified(
        WorkflowVersion version,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        version.ModifiedAtUtc = timestamp;
        version.ModifiedByAccount = actor.AccountName;
        version.ModifiedByUserId = actor.ApplicationUserId;
    }

    private static void SetModified(
        WorkflowStep step,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        step.ModifiedAtUtc = timestamp;
        step.ModifiedByAccount = actor.AccountName;
        step.ModifiedByUserId = actor.ApplicationUserId;
    }

    private static void SetModified(
        WorkflowTransition transition,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        transition.ModifiedAtUtc = timestamp;
        transition.ModifiedByAccount = actor.AccountName;
        transition.ModifiedByUserId = actor.ApplicationUserId;
    }

    private static void SetModified(
        WorkflowStepFieldPermission permission,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        permission.ModifiedAtUtc = timestamp;
        permission.ModifiedByAccount = actor.AccountName;
        permission.ModifiedByUserId = actor.ApplicationUserId;
    }

    private static void EnsureRowVersion(byte[] actual, byte[] expected)
    {
        if (!actual.SequenceEqual(expected))
        {
            throw ConcurrencyConflict();
        }
    }

    private static int DisplayLifecycleOrder(string lifecycle) => lifecycle switch
    {
        "draft" => 0,
        "published" => 1,
        "archived" => 2,
        _ => 3
    };

    private static string LifecycleToApi(WorkflowVersionLifecycle lifecycle) => lifecycle switch
    {
        WorkflowVersionLifecycle.Draft => "draft",
        WorkflowVersionLifecycle.Published => "published",
        WorkflowVersionLifecycle.Archived => "archived",
        _ => throw new ArgumentOutOfRangeException(nameof(lifecycle))
    };

    private static string CommentPolicyToApi(WorkflowCommentPolicy policy) => policy switch
    {
        WorkflowCommentPolicy.None => "none",
        WorkflowCommentPolicy.Optional => "optional",
        WorkflowCommentPolicy.Required => "required",
        _ => throw new ArgumentOutOfRangeException(nameof(policy))
    };

    private static string ActionTypeToApi(WorkflowActionType actionType) => actionType switch
    {
        WorkflowActionType.Submit => "submit",
        WorkflowActionType.Approve => "approve",
        WorkflowActionType.Reject => "reject",
        WorkflowActionType.PutOnHold => "putOnHold",
        WorkflowActionType.Resume => "resume",
        WorkflowActionType.RequestMoreInformation => "requestMoreInformation",
        WorkflowActionType.Return => "return",
        WorkflowActionType.Forward => "forward",
        WorkflowActionType.Complete => "complete",
        _ => throw new ArgumentOutOfRangeException(nameof(actionType))
    };

    private static string TerminalOutcomeToApi(WorkflowTerminalOutcome outcome) => outcome switch
    {
        WorkflowTerminalOutcome.Approved => "approved",
        WorkflowTerminalOutcome.Rejected => "rejected",
        WorkflowTerminalOutcome.Completed => "completed",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };

    private static string FieldAccessToApi(WorkflowFieldAccess access) => access switch
    {
        WorkflowFieldAccess.Hidden => "hidden",
        WorkflowFieldAccess.ReadOnly => "readOnly",
        WorkflowFieldAccess.Editable => "editable",
        WorkflowFieldAccess.EditableRequired => "editableRequired",
        _ => throw new ArgumentOutOfRangeException(nameof(access))
    };

    private static string DocumentAccessToApi(WorkflowDocumentAccess access) => access switch
    {
        WorkflowDocumentAccess.Hidden => "hidden",
        WorkflowDocumentAccess.View => "view",
        WorkflowDocumentAccess.Edit => "edit",
        _ => throw new ArgumentOutOfRangeException(nameof(access))
    };

    private static string ApprovalRequestStatusToApi(ApprovalRequestStatus status) => status switch
    {
        ApprovalRequestStatus.Draft => "draft",
        ApprovalRequestStatus.Submitted => "submitted",
        ApprovalRequestStatus.InProgress => "inProgress",
        ApprovalRequestStatus.OnHold => "onHold",
        ApprovalRequestStatus.MoreInformationRequired => "moreInformationRequired",
        ApprovalRequestStatus.Approved => "approved",
        ApprovalRequestStatus.Rejected => "rejected",
        ApprovalRequestStatus.Completed => "completed",
        ApprovalRequestStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static string FieldTypeToApi(RequestFieldType fieldType) => fieldType switch
    {
        RequestFieldType.ShortText => "shortText",
        RequestFieldType.LongText => "longText",
        RequestFieldType.Integer => "integer",
        RequestFieldType.Decimal => "decimal",
        RequestFieldType.Date => "date",
        RequestFieldType.DateTime => "dateTime",
        RequestFieldType.YesNo => "yesNo",
        RequestFieldType.SingleChoice => "singleChoice",
        RequestFieldType.MultipleChoice => "multipleChoice",
        RequestFieldType.ActiveDirectoryUser => "activeDirectoryUser",
        RequestFieldType.ApplicationRole => "applicationRole",
        RequestFieldType.FileDocument => "fileDocument",
        RequestFieldType.RichDocument => "richDocument",
        _ => throw new ArgumentOutOfRangeException(nameof(fieldType))
    };

    private static string DocumentModeToApi(DocumentFieldMode mode) => mode switch
    {
        DocumentFieldMode.UploadOnly => "uploadOnly",
        DocumentFieldMode.CreateInEditorOnly => "createInEditorOnly",
        DocumentFieldMode.UploadOrCreate => "uploadOrCreate",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private sealed record WorkflowRootProjection(
        Guid Id,
        string Code,
        bool IsArchived,
        DateTimeOffset CreatedAtUtc,
        string CreatedByAccount,
        DateTimeOffset? ModifiedAtUtc,
        string? ModifiedByAccount,
        DateTimeOffset? ArchivedAtUtc,
        string? ArchivedByAccount,
        byte[] RowVersion);

    private sealed record WorkflowVersionSummaryProjection(
        Guid Id,
        Guid WorkflowDefinitionId,
        int VersionNumber,
        Guid RequestTypeId,
        Guid RequestTypeVersionId,
        int RequestTypeVersionNumber,
        string RequestTypeCode,
        string NameEnglish,
        string NameArabic,
        string DescriptionEnglish,
        string DescriptionArabic,
        string NavigationLabelEnglish,
        string NavigationLabelArabic,
        string NavigationSlug,
        int NavigationOrder,
        WorkflowVersionLifecycle Lifecycle,
        int RequestCount,
        int StarterRoleCount,
        int ActiveStepCount,
        int ActiveTransitionCount,
        DateTimeOffset CreatedAtUtc,
        string CreatedByAccount,
        DateTimeOffset? ModifiedAtUtc,
        string? ModifiedByAccount,
        DateTimeOffset? PublishedAtUtc,
        string? PublishedByAccount,
        DateTimeOffset? ArchivedAtUtc,
        string? ArchivedByAccount,
        byte[] RowVersion);
}
