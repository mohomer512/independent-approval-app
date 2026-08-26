using System.Globalization;
using System.Text.RegularExpressions;
using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Domain.Workflows;

namespace IndependentApproval.Api.Application.Administration;

internal static partial class WorkflowDefinitionValidator
{
    private const int MaximumCodeLength = 64;
    private const int MaximumNameLength = 200;
    private const int MaximumDescriptionLength = 2000;
    private const int MaximumSlugLength = 200;
    private const int MaximumKeyLength = 64;
    private const int MaximumRoleCount = 200;
    private const decimal MaximumDiagramCoordinate = 100_000m;

    public static CreateWorkflowValues ValidateCreate(CreateWorkflowRequest request)
    {
        var errors = NewErrors();
        var code = RequiredText(request.Code, "code", MaximumCodeLength, errors)
            .ToUpperInvariant();

        if (code.Length > 0 && !CodePattern().IsMatch(code))
        {
            AddError(errors, "code", "Code must start with a letter or number and use only letters, numbers, periods, underscores or hyphens.");
        }

        var version = ValidateVersionValues(
            request.RequestTypeVersionId,
            request.NameEnglish,
            request.NameArabic,
            request.DescriptionEnglish,
            request.DescriptionArabic,
            request.NavigationLabelEnglish,
            request.NavigationLabelArabic,
            request.NavigationSlug,
            request.NavigationOrder,
            errors);
        ThrowIfInvalid(errors);
        return new CreateWorkflowValues(code, version);
    }

    public static WorkflowVersionValues ValidateVersion(UpdateWorkflowVersionRequest request)
    {
        var errors = NewErrors();
        var values = ValidateVersionValues(
            request.RequestTypeVersionId,
            request.NameEnglish,
            request.NameArabic,
            request.DescriptionEnglish,
            request.DescriptionArabic,
            request.NavigationLabelEnglish,
            request.NavigationLabelArabic,
            request.NavigationSlug,
            request.NavigationOrder,
            errors);
        ThrowIfInvalid(errors);
        return values;
    }

    public static WorkflowStepValues ValidateStep(CreateWorkflowStepRequest request)
    {
        var errors = NewErrors();
        var key = ValidateKey(request.Key, "key", errors);
        var values = ValidateStepValues(
            request.NameEnglish,
            request.NameArabic,
            request.SortOrder,
            request.DiagramX,
            request.DiagramY,
            request.IsStartStep,
            request.IsActive,
            request.CanEditRequestData,
            request.CanOpenDocuments,
            request.CanEditDocuments,
            request.CanForwardDocuments,
            request.CommentPolicy,
            errors);
        var roleIds = ValidateRoleIds(request.RoleIds, "roleIds", errors);
        ThrowIfInvalid(errors);
        return new WorkflowStepValues(
            key,
            key.ToUpperInvariant(),
            values.NameEnglish,
            values.NameArabic,
            values.SortOrder,
            values.DiagramX,
            values.DiagramY,
            values.IsStartStep,
            values.IsActive,
            values.CanEditRequestData,
            values.CanOpenDocuments,
            values.CanEditDocuments,
            values.CanForwardDocuments,
            values.CommentPolicy,
            roleIds);
    }

    public static WorkflowStepValues ValidateStep(
        string key,
        UpdateWorkflowStepRequest request)
    {
        var errors = NewErrors();
        var values = ValidateStepValues(
            request.NameEnglish,
            request.NameArabic,
            request.SortOrder,
            request.DiagramX,
            request.DiagramY,
            request.IsStartStep,
            request.IsActive,
            request.CanEditRequestData,
            request.CanOpenDocuments,
            request.CanEditDocuments,
            request.CanForwardDocuments,
            request.CommentPolicy,
            errors);
        ThrowIfInvalid(errors);
        return new WorkflowStepValues(
            key,
            key.ToUpperInvariant(),
            values.NameEnglish,
            values.NameArabic,
            values.SortOrder,
            values.DiagramX,
            values.DiagramY,
            values.IsStartStep,
            values.IsActive,
            values.CanEditRequestData,
            values.CanOpenDocuments,
            values.CanEditDocuments,
            values.CanForwardDocuments,
            values.CommentPolicy,
            []);
    }

    public static WorkflowTransitionValues ValidateTransition(
        CreateWorkflowTransitionRequest request)
    {
        var errors = NewErrors();
        var key = ValidateKey(request.Key, "key", errors);
        var values = ValidateTransitionValues(
            request.SourceStepId,
            request.TargetStepId,
            request.ActionLabelEnglish,
            request.ActionLabelArabic,
            request.ActionType,
            request.ResultingStatus,
            request.RequiresComment,
            request.TerminalOutcome,
            request.SortOrder,
            request.IsActive,
            errors);
        ThrowIfInvalid(errors);
        return values with { Key = key, NormalizedKey = key.ToUpperInvariant() };
    }

    public static WorkflowTransitionValues ValidateTransition(
        string key,
        UpdateWorkflowTransitionRequest request)
    {
        var errors = NewErrors();
        var values = ValidateTransitionValues(
            request.SourceStepId,
            request.TargetStepId,
            request.ActionLabelEnglish,
            request.ActionLabelArabic,
            request.ActionType,
            request.ResultingStatus,
            request.RequiresComment,
            request.TerminalOutcome,
            request.SortOrder,
            request.IsActive,
            errors);
        ThrowIfInvalid(errors);
        return values with { Key = key, NormalizedKey = key.ToUpperInvariant() };
    }

    public static IReadOnlyList<Guid> ValidateRoleIds(
        IReadOnlyList<Guid>? roleIds,
        string field = "roleIds")
    {
        var errors = NewErrors();
        var values = ValidateRoleIds(roleIds, field, errors);
        ThrowIfInvalid(errors);
        return values;
    }

    public static IReadOnlyList<WorkflowPermissionValues> ValidatePermissions(
        IReadOnlyList<WorkflowStepFieldPermissionInput>? permissions)
    {
        var errors = NewErrors();

        if (permissions is null)
        {
            AddError(errors, "permissions", "Permissions are required.");
            ThrowIfInvalid(errors);
        }

        if (permissions!.Count > 500)
        {
            AddError(errors, "permissions", "No more than 500 field permissions may be submitted at once.");
        }

        var fieldIds = new HashSet<Guid>();
        var values = new List<WorkflowPermissionValues>(permissions.Count);

        for (var index = 0; index < permissions.Count; index++)
        {
            var permission = permissions[index];
            var prefix = $"permissions[{index}]";

            if (permission.RequestFieldDefinitionId == Guid.Empty)
            {
                AddError(errors, $"{prefix}.requestFieldDefinitionId", "A request field is required.");
            }
            else if (!fieldIds.Add(permission.RequestFieldDefinitionId))
            {
                AddError(errors, $"{prefix}.requestFieldDefinitionId", "Each request field may appear only once.");
            }

            var access = ParseFieldAccess(permission.Access, $"{prefix}.access", errors);
            var documentAccess = ParseOptionalDocumentAccess(
                permission.DocumentAccess,
                $"{prefix}.documentAccess",
                errors);
            values.Add(new WorkflowPermissionValues(
                permission.RequestFieldDefinitionId,
                access,
                documentAccess,
                permission.CanSelectForForwarding,
                permission.RowVersion));
        }

        ThrowIfInvalid(errors);
        return values;
    }

    private static WorkflowVersionValues ValidateVersionValues(
        Guid requestTypeVersionId,
        string? nameEnglish,
        string? nameArabic,
        string? descriptionEnglish,
        string? descriptionArabic,
        string? navigationLabelEnglish,
        string? navigationLabelArabic,
        string? navigationSlug,
        int navigationOrder,
        IDictionary<string, List<string>> errors)
    {
        if (requestTypeVersionId == Guid.Empty)
        {
            AddError(errors, "requestTypeVersionId", "A published request-type version is required.");
        }

        var normalizedSlug = RequiredText(
            navigationSlug,
            "navigationSlug",
            MaximumSlugLength,
            errors).ToLowerInvariant();

        if (normalizedSlug.Length > 0 && !SlugPattern().IsMatch(normalizedSlug))
        {
            AddError(errors, "navigationSlug", "Navigation slug must use lowercase letters, numbers and single hyphen separators.");
        }

        if (navigationOrder < 0)
        {
            AddError(errors, "navigationOrder", "Navigation order cannot be negative.");
        }

        return new WorkflowVersionValues(
            requestTypeVersionId,
            RequiredText(nameEnglish, "nameEnglish", MaximumNameLength, errors),
            RequiredText(nameArabic, "nameArabic", MaximumNameLength, errors),
            RequiredText(descriptionEnglish, "descriptionEnglish", MaximumDescriptionLength, errors),
            RequiredText(descriptionArabic, "descriptionArabic", MaximumDescriptionLength, errors),
            RequiredText(navigationLabelEnglish, "navigationLabelEnglish", MaximumNameLength, errors),
            RequiredText(navigationLabelArabic, "navigationLabelArabic", MaximumNameLength, errors),
            normalizedSlug,
            navigationOrder);
    }

    private static WorkflowStepValues ValidateStepValues(
        string? nameEnglish,
        string? nameArabic,
        int sortOrder,
        decimal diagramX,
        decimal diagramY,
        bool isStartStep,
        bool isActive,
        bool canEditRequestData,
        bool canOpenDocuments,
        bool canEditDocuments,
        bool canForwardDocuments,
        string? commentPolicy,
        IDictionary<string, List<string>> errors)
    {
        if (sortOrder < 0)
        {
            AddError(errors, "sortOrder", "Sort order cannot be negative.");
        }

        if (Math.Abs(diagramX) > MaximumDiagramCoordinate)
        {
            AddError(errors, "diagramX", $"Diagram X must be between {(-MaximumDiagramCoordinate).ToString(CultureInfo.InvariantCulture)} and {MaximumDiagramCoordinate.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (Math.Abs(diagramY) > MaximumDiagramCoordinate)
        {
            AddError(errors, "diagramY", $"Diagram Y must be between {(-MaximumDiagramCoordinate).ToString(CultureInfo.InvariantCulture)} and {MaximumDiagramCoordinate.ToString(CultureInfo.InvariantCulture)}.");
        }

        return new WorkflowStepValues(
            string.Empty,
            string.Empty,
            RequiredText(nameEnglish, "nameEnglish", MaximumNameLength, errors),
            RequiredText(nameArabic, "nameArabic", MaximumNameLength, errors),
            sortOrder,
            diagramX,
            diagramY,
            isStartStep,
            isActive,
            canEditRequestData,
            canOpenDocuments,
            canEditDocuments,
            canForwardDocuments,
            ParseCommentPolicy(commentPolicy, "commentPolicy", errors),
            []);
    }

    private static WorkflowTransitionValues ValidateTransitionValues(
        Guid sourceStepId,
        Guid? targetStepId,
        string? actionLabelEnglish,
        string? actionLabelArabic,
        string? actionType,
        string? resultingStatus,
        bool requiresComment,
        string? terminalOutcome,
        int sortOrder,
        bool isActive,
        IDictionary<string, List<string>> errors)
    {
        if (sourceStepId == Guid.Empty)
        {
            AddError(errors, "sourceStepId", "A source step is required.");
        }

        if (targetStepId == Guid.Empty)
        {
            AddError(errors, "targetStepId", "Target step cannot be an empty identifier.");
        }

        if (sortOrder < 0)
        {
            AddError(errors, "sortOrder", "Sort order cannot be negative.");
        }

        var outcome = ParseOptionalTerminalOutcome(terminalOutcome, "terminalOutcome", errors);

        if (outcome is null && targetStepId is null)
        {
            AddError(errors, "targetStepId", "A nonterminal transition requires a target step.");
        }
        else if (outcome is not null && targetStepId is not null)
        {
            AddError(errors, "targetStepId", "A terminal transition cannot have a target step.");
        }

        var status = ParseApprovalRequestStatus(resultingStatus, "resultingStatus", errors);

        if (outcome is WorkflowTerminalOutcome.Approved
            && status != ApprovalRequestStatus.Approved
            || outcome is WorkflowTerminalOutcome.Rejected
            && status != ApprovalRequestStatus.Rejected
            || outcome is WorkflowTerminalOutcome.Completed
            && status != ApprovalRequestStatus.Completed)
        {
            AddError(errors, "resultingStatus", "Terminal transition status must match its terminal outcome.");
        }

        return new WorkflowTransitionValues(
            string.Empty,
            string.Empty,
            sourceStepId,
            targetStepId,
            RequiredText(actionLabelEnglish, "actionLabelEnglish", MaximumNameLength, errors),
            RequiredText(actionLabelArabic, "actionLabelArabic", MaximumNameLength, errors),
            ParseActionType(actionType, "actionType", errors),
            status,
            requiresComment,
            outcome,
            sortOrder,
            isActive);
    }

    private static string ValidateKey(
        string? value,
        string field,
        IDictionary<string, List<string>> errors)
    {
        var key = RequiredText(value, field, MaximumKeyLength, errors).ToLowerInvariant();

        if (key.Length > 0 && !KeyPattern().IsMatch(key))
        {
            AddError(errors, field, "Key must start with a lowercase letter and use only lowercase letters, numbers and underscores.");
        }

        return key;
    }

    private static IReadOnlyList<Guid> ValidateRoleIds(
        IReadOnlyList<Guid>? roleIds,
        string field,
        IDictionary<string, List<string>> errors)
    {
        if (roleIds is null)
        {
            AddError(errors, field, "Role identifiers are required.");
            return [];
        }

        if (roleIds.Count > MaximumRoleCount)
        {
            AddError(errors, field, $"No more than {MaximumRoleCount} roles may be assigned.");
        }

        if (roleIds.Any(roleId => roleId == Guid.Empty))
        {
            AddError(errors, field, "Role identifiers cannot be empty.");
        }

        var distinct = roleIds.Distinct().ToArray();

        if (distinct.Length != roleIds.Count)
        {
            AddError(errors, field, "Role identifiers must be unique.");
        }

        return distinct;
    }

    private static WorkflowCommentPolicy ParseCommentPolicy(
        string? value,
        string field,
        IDictionary<string, List<string>> errors) =>
        value switch
        {
            "none" => WorkflowCommentPolicy.None,
            "optional" => WorkflowCommentPolicy.Optional,
            "required" => WorkflowCommentPolicy.Required,
            _ => InvalidEnum(value, field, errors, WorkflowCommentPolicy.None,
                "none, optional or required")
        };

    private static WorkflowActionType ParseActionType(
        string? value,
        string field,
        IDictionary<string, List<string>> errors) =>
        value switch
        {
            "submit" => WorkflowActionType.Submit,
            "approve" => WorkflowActionType.Approve,
            "reject" => WorkflowActionType.Reject,
            "putOnHold" => WorkflowActionType.PutOnHold,
            "resume" => WorkflowActionType.Resume,
            "requestMoreInformation" => WorkflowActionType.RequestMoreInformation,
            "return" => WorkflowActionType.Return,
            "forward" => WorkflowActionType.Forward,
            "complete" => WorkflowActionType.Complete,
            _ => InvalidEnum(value, field, errors, WorkflowActionType.Submit,
                "submit, approve, reject, putOnHold, resume, requestMoreInformation, return, forward or complete")
        };

    private static ApprovalRequestStatus ParseApprovalRequestStatus(
        string? value,
        string field,
        IDictionary<string, List<string>> errors) =>
        value switch
        {
            "draft" => ApprovalRequestStatus.Draft,
            "submitted" => ApprovalRequestStatus.Submitted,
            "inProgress" => ApprovalRequestStatus.InProgress,
            "onHold" => ApprovalRequestStatus.OnHold,
            "moreInformationRequired" => ApprovalRequestStatus.MoreInformationRequired,
            "approved" => ApprovalRequestStatus.Approved,
            "rejected" => ApprovalRequestStatus.Rejected,
            "completed" => ApprovalRequestStatus.Completed,
            "cancelled" => ApprovalRequestStatus.Cancelled,
            _ => InvalidEnum(value, field, errors, ApprovalRequestStatus.Draft,
                "draft, submitted, inProgress, onHold, moreInformationRequired, approved, rejected, completed or cancelled")
        };

    private static WorkflowTerminalOutcome? ParseOptionalTerminalOutcome(
        string? value,
        string field,
        IDictionary<string, List<string>> errors) =>
        value switch
        {
            null => null,
            "approved" => WorkflowTerminalOutcome.Approved,
            "rejected" => WorkflowTerminalOutcome.Rejected,
            "completed" => WorkflowTerminalOutcome.Completed,
            _ => InvalidEnum<WorkflowTerminalOutcome?>(value, field, errors, null,
                "approved, rejected, completed or null")
        };

    private static WorkflowFieldAccess ParseFieldAccess(
        string? value,
        string field,
        IDictionary<string, List<string>> errors) =>
        value switch
        {
            "hidden" => WorkflowFieldAccess.Hidden,
            "readOnly" => WorkflowFieldAccess.ReadOnly,
            "editable" => WorkflowFieldAccess.Editable,
            "editableRequired" => WorkflowFieldAccess.EditableRequired,
            _ => InvalidEnum(value, field, errors, WorkflowFieldAccess.Hidden,
                "hidden, readOnly, editable or editableRequired")
        };

    private static WorkflowDocumentAccess? ParseOptionalDocumentAccess(
        string? value,
        string field,
        IDictionary<string, List<string>> errors) =>
        value switch
        {
            null => null,
            "hidden" => WorkflowDocumentAccess.Hidden,
            "view" => WorkflowDocumentAccess.View,
            "edit" => WorkflowDocumentAccess.Edit,
            _ => InvalidEnum<WorkflowDocumentAccess?>(value, field, errors, null,
                "hidden, view, edit or null")
        };

    private static T InvalidEnum<T>(
        string? value,
        string field,
        IDictionary<string, List<string>> errors,
        T fallback,
        string allowed)
    {
        AddError(errors, field, $"Value must be {allowed}.");
        return fallback;
    }

    private static string RequiredText(
        string? value,
        string field,
        int maximumLength,
        IDictionary<string, List<string>> errors)
    {
        var normalized = value?.Trim() ?? string.Empty;

        if (normalized.Length == 0)
        {
            AddError(errors, field, "Value is required.");
        }
        else if (normalized.Length > maximumLength)
        {
            AddError(errors, field, $"Value cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    private static Dictionary<string, List<string>> NewErrors() =>
        new(StringComparer.OrdinalIgnoreCase);

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string field,
        string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        messages.Add(message);
    }

    private static void ThrowIfInvalid(IDictionary<string, List<string>> errors)
    {
        if (errors.Count == 0)
        {
            return;
        }

        throw new AdministrationValidationException(
            errors.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.OrdinalIgnoreCase));
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9._-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    [GeneratedRegex("^[a-z][a-z0-9_]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}

internal sealed record CreateWorkflowValues(
    string Code,
    WorkflowVersionValues Version);

internal sealed record WorkflowVersionValues(
    Guid RequestTypeVersionId,
    string NameEnglish,
    string NameArabic,
    string DescriptionEnglish,
    string DescriptionArabic,
    string NavigationLabelEnglish,
    string NavigationLabelArabic,
    string NavigationSlug,
    int NavigationOrder);

internal sealed record WorkflowStepValues(
    string Key,
    string NormalizedKey,
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
    WorkflowCommentPolicy CommentPolicy,
    IReadOnlyList<Guid> RoleIds);

internal sealed record WorkflowTransitionValues(
    string Key,
    string NormalizedKey,
    Guid SourceStepId,
    Guid? TargetStepId,
    string ActionLabelEnglish,
    string ActionLabelArabic,
    WorkflowActionType ActionType,
    ApprovalRequestStatus ResultingStatus,
    bool RequiresComment,
    WorkflowTerminalOutcome? TerminalOutcome,
    int SortOrder,
    bool IsActive);

internal sealed record WorkflowPermissionValues(
    Guid RequestFieldDefinitionId,
    WorkflowFieldAccess Access,
    WorkflowDocumentAccess? DocumentAccess,
    bool CanSelectForForwarding,
    string? RowVersion);
