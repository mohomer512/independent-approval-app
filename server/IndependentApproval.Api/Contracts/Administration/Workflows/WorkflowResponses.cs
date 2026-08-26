namespace IndependentApproval.Api.Contracts.Administration.Workflows;

public sealed record WorkflowListItemResponse(
    Guid Id,
    string Code,
    bool IsArchived,
    WorkflowVersionSummaryResponse? DisplayVersion,
    Guid? DraftVersionId,
    Guid? LatestPublishedVersionId,
    int VersionCount,
    DateTimeOffset CreatedAtUtc,
    string CreatedByAccount,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedByAccount,
    DateTimeOffset? ArchivedAtUtc,
    string? ArchivedByAccount,
    string RowVersion);

public sealed record WorkflowDetailResponse(
    Guid Id,
    string Code,
    bool IsArchived,
    IReadOnlyList<WorkflowVersionResponse> Versions,
    DateTimeOffset CreatedAtUtc,
    string CreatedByAccount,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedByAccount,
    DateTimeOffset? ArchivedAtUtc,
    string? ArchivedByAccount,
    string RowVersion);

public sealed record WorkflowVersionSummaryResponse(
    Guid Id,
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
    string Lifecycle,
    bool IsInUse,
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
    string RowVersion);

public sealed record WorkflowVersionResponse(
    Guid Id,
    int VersionNumber,
    Guid RequestTypeId,
    Guid RequestTypeVersionId,
    int RequestTypeVersionNumber,
    string RequestTypeCode,
    IReadOnlyList<WorkflowRequestFieldOptionResponse> RequestFields,
    string NameEnglish,
    string NameArabic,
    string DescriptionEnglish,
    string DescriptionArabic,
    string NavigationLabelEnglish,
    string NavigationLabelArabic,
    string NavigationSlug,
    int NavigationOrder,
    string Lifecycle,
    bool IsInUse,
    int RequestCount,
    IReadOnlyList<WorkflowRoleReferenceResponse> StarterRoles,
    IReadOnlyList<WorkflowStepResponse> Steps,
    IReadOnlyList<WorkflowTransitionResponse> Transitions,
    DateTimeOffset CreatedAtUtc,
    string CreatedByAccount,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedByAccount,
    DateTimeOffset? PublishedAtUtc,
    string? PublishedByAccount,
    DateTimeOffset? ArchivedAtUtc,
    string? ArchivedByAccount,
    string RowVersion);

public sealed record WorkflowRoleReferenceResponse(
    Guid Id,
    string Code,
    string NameEnglish,
    string NameArabic);

public sealed record WorkflowStepResponse(
    Guid Id,
    string Key,
    string NameEnglish,
    string NameArabic,
    int SortOrder,
    decimal DiagramX,
    decimal DiagramY,
    bool IsStartStep,
    bool IsActive,
    bool CanEditRequestData,
    bool CanOpenDocuments,
    bool CanEditDocuments,
    bool CanForwardDocuments,
    string CommentPolicy,
    IReadOnlyList<WorkflowRoleReferenceResponse> Roles,
    IReadOnlyList<WorkflowStepFieldPermissionResponse> FieldPermissions,
    DateTimeOffset CreatedAtUtc,
    string CreatedByAccount,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedByAccount,
    string RowVersion);

public sealed record WorkflowTransitionResponse(
    Guid Id,
    string Key,
    Guid SourceStepId,
    Guid? TargetStepId,
    string ActionLabelEnglish,
    string ActionLabelArabic,
    string ActionType,
    string ResultingStatus,
    bool RequiresComment,
    string? TerminalOutcome,
    int SortOrder,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    string CreatedByAccount,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedByAccount,
    string RowVersion);

public sealed record WorkflowStepFieldPermissionResponse(
    Guid Id,
    Guid RequestFieldDefinitionId,
    string FieldKey,
    string FieldType,
    string Access,
    string? DocumentAccess,
    bool CanSelectForForwarding,
    DateTimeOffset CreatedAtUtc,
    string CreatedByAccount,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedByAccount,
    string RowVersion);

public sealed record WorkflowValidationResponse(
    bool IsValid,
    IReadOnlyList<WorkflowValidationIssueResponse> Issues);

public sealed record WorkflowValidationIssueResponse(
    string Code,
    string Path,
    string Message);

public sealed record WorkflowOptionsResponse(
    IReadOnlyList<WorkflowRoleOptionResponse> Roles,
    IReadOnlyList<WorkflowRequestTypeVersionOptionResponse> RequestTypeVersions,
    IReadOnlyList<WorkflowSystemFieldOptionResponse> SystemFields,
    IReadOnlyList<WorkflowCatalogueOptionResponse> ActionTypes,
    IReadOnlyList<WorkflowCatalogueOptionResponse> ResultingStatuses,
    IReadOnlyList<WorkflowCatalogueOptionResponse> CommentPolicies,
    IReadOnlyList<WorkflowCatalogueOptionResponse> FieldAccessModes,
    IReadOnlyList<WorkflowCatalogueOptionResponse> DocumentAccessModes,
    IReadOnlyList<WorkflowCatalogueOptionResponse> TerminalOutcomes);

public sealed record WorkflowRoleOptionResponse(
    Guid Id,
    string Code,
    string NameEnglish,
    string NameArabic);

public sealed record WorkflowRequestTypeVersionOptionResponse(
    Guid RequestTypeId,
    string RequestTypeCode,
    Guid RequestTypeVersionId,
    int VersionNumber,
    string NameEnglish,
    string NameArabic,
    IReadOnlyList<WorkflowRequestFieldOptionResponse> Fields);

public sealed record WorkflowRequestFieldOptionResponse(
    Guid Id,
    string Key,
    string LabelEnglish,
    string LabelArabic,
    string? HelpTextEnglish,
    string? HelpTextArabic,
    string FieldType,
    bool IsRequired,
    bool IsActive,
    string? DocumentMode);

public sealed record WorkflowSystemFieldOptionResponse(
    string Key,
    string LabelEnglish,
    string LabelArabic,
    string DataType,
    string Access,
    string? DocumentAccess,
    bool CanSelectForForwarding);

public sealed record WorkflowCatalogueOptionResponse(
    string Value,
    string LabelEnglish,
    string LabelArabic);
