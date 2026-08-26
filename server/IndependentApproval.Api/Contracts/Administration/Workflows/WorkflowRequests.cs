namespace IndependentApproval.Api.Contracts.Administration.Workflows;

public sealed record CreateWorkflowRequest(
    string? Code,
    Guid RequestTypeVersionId,
    string? NameEnglish,
    string? NameArabic,
    string? DescriptionEnglish,
    string? DescriptionArabic,
    string? NavigationLabelEnglish,
    string? NavigationLabelArabic,
    string? NavigationSlug,
    int NavigationOrder);

public sealed record UpdateWorkflowVersionRequest(
    Guid RequestTypeVersionId,
    string? NameEnglish,
    string? NameArabic,
    string? DescriptionEnglish,
    string? DescriptionArabic,
    string? NavigationLabelEnglish,
    string? NavigationLabelArabic,
    string? NavigationSlug,
    int NavigationOrder,
    string? RowVersion);

public sealed record ReplaceWorkflowStarterRolesRequest(
    IReadOnlyList<Guid>? RoleIds,
    string? VersionRowVersion);

public sealed record CreateWorkflowStepRequest(
    string? Key,
    string? NameEnglish,
    string? NameArabic,
    int SortOrder,
    decimal DiagramX,
    decimal DiagramY,
    bool IsStartStep,
    bool IsActive,
    bool CanEditRequestData,
    bool CanOpenDocuments,
    bool CanEditDocuments,
    bool CanForwardDocuments,
    string? CommentPolicy,
    IReadOnlyList<Guid>? RoleIds,
    string? VersionRowVersion);

public sealed record UpdateWorkflowStepRequest(
    string? NameEnglish,
    string? NameArabic,
    int SortOrder,
    decimal DiagramX,
    decimal DiagramY,
    bool IsStartStep,
    bool IsActive,
    bool CanEditRequestData,
    bool CanOpenDocuments,
    bool CanEditDocuments,
    bool CanForwardDocuments,
    string? CommentPolicy,
    string? VersionRowVersion,
    string? StepRowVersion);

public sealed record ReplaceWorkflowStepRolesRequest(
    IReadOnlyList<Guid>? RoleIds,
    string? VersionRowVersion,
    string? StepRowVersion);

public sealed record DeleteWorkflowStepRequest(
    string? VersionRowVersion,
    string? StepRowVersion);

public sealed record CreateWorkflowTransitionRequest(
    string? Key,
    Guid SourceStepId,
    Guid? TargetStepId,
    string? ActionLabelEnglish,
    string? ActionLabelArabic,
    string? ActionType,
    string? ResultingStatus,
    bool RequiresComment,
    string? TerminalOutcome,
    int SortOrder,
    bool IsActive,
    string? VersionRowVersion);

public sealed record UpdateWorkflowTransitionRequest(
    Guid SourceStepId,
    Guid? TargetStepId,
    string? ActionLabelEnglish,
    string? ActionLabelArabic,
    string? ActionType,
    string? ResultingStatus,
    bool RequiresComment,
    string? TerminalOutcome,
    int SortOrder,
    bool IsActive,
    string? VersionRowVersion,
    string? TransitionRowVersion);

public sealed record DeleteWorkflowTransitionRequest(
    string? VersionRowVersion,
    string? TransitionRowVersion);

public sealed record ReplaceWorkflowStepFieldPermissionsRequest(
    IReadOnlyList<WorkflowStepFieldPermissionInput>? Permissions,
    string? VersionRowVersion,
    string? StepRowVersion);

public sealed record WorkflowStepFieldPermissionInput(
    Guid RequestFieldDefinitionId,
    string? Access,
    string? DocumentAccess,
    bool CanSelectForForwarding,
    string? RowVersion);

public sealed record WorkflowConcurrencyRequest(string? RowVersion);

public sealed record ValidateWorkflowRequest(string? VersionRowVersion);
