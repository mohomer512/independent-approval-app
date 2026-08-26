using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Domain.Workflows;

namespace IndependentApproval.Api.Application.Administration;

internal static class WorkflowPublishingValidator
{
    public static IReadOnlyList<WorkflowValidationIssueResponse> Validate(
        WorkflowVersion version)
    {
        var issues = new List<WorkflowValidationIssueResponse>();
        ValidateBoundRequestType(version, issues);

        var starterRoleIds = version.StarterRoles
            .Where(assignment =>
                assignment.ApplicationRole.IsActive
                && !assignment.ApplicationRole.IsArchived)
            .Select(assignment => assignment.ApplicationRoleId)
            .ToHashSet();

        foreach (var assignment in version.StarterRoles.Where(assignment =>
                     !assignment.ApplicationRole.IsActive
                     || assignment.ApplicationRole.IsArchived))
        {
            Add(issues, "workflow.starter_role_inactive", "starterRoles", $"Starter role '{assignment.ApplicationRole.Code}' is inactive or archived.");
        }

        if (starterRoleIds.Count == 0)
        {
            Add(issues, "workflow.starter_role_required", "starterRoles", "At least one active starter role is required.");
        }

        var activeSteps = version.Steps
            .Where(step => step.IsActive)
            .OrderBy(step => step.SortOrder)
            .ToArray();
        var activeStepIds = activeSteps.Select(step => step.Id).ToHashSet();
        var startSteps = activeSteps.Where(step => step.IsStartStep).ToArray();

        if (startSteps.Length != 1)
        {
            Add(issues, "workflow.single_start_step_required", "steps", "Exactly one active start step is required.");
        }

        foreach (var step in activeSteps)
        {
            var activeRoleIds = step.AssignedRoles
                .Where(assignment =>
                    assignment.ApplicationRole.IsActive
                    && !assignment.ApplicationRole.IsArchived)
                .Select(assignment => assignment.ApplicationRoleId)
                .ToHashSet();

            foreach (var assignment in step.AssignedRoles.Where(assignment =>
                         !assignment.ApplicationRole.IsActive
                         || assignment.ApplicationRole.IsArchived))
            {
                Add(issues, "workflow.step_role_inactive", $"steps.{step.Id}.roles", $"Assigned role '{assignment.ApplicationRole.Code}' is inactive or archived.");
            }

            if (activeRoleIds.Count == 0)
            {
                Add(issues, "workflow.step_role_required", $"steps.{step.Id}.roles", $"Active step '{step.Key}' requires at least one active role.");
            }

            if (step.IsStartStep
                && starterRoleIds.Count > 0
                && !activeRoleIds.Overlaps(starterRoleIds))
            {
                Add(issues, "workflow.start_role_incompatible", $"steps.{step.Id}.roles", "The start step must be assigned to at least one active starter role.");
            }

            if (step.CanEditDocuments && !step.CanOpenDocuments)
            {
                Add(issues, "workflow.step_document_capability_invalid", $"steps.{step.Id}.canEditDocuments", "A step cannot edit documents unless it can open documents.");
            }

            if (step.CanForwardDocuments && !step.CanOpenDocuments)
            {
                Add(issues, "workflow.step_document_capability_invalid", $"steps.{step.Id}.canForwardDocuments", "A step cannot forward documents unless it can open documents.");
            }
        }

        var activeTransitions = version.Transitions
            .Where(transition => transition.IsActive)
            .OrderBy(transition => transition.SortOrder)
            .ToArray();
        ValidateCommentPolicies(activeSteps, activeTransitions, issues);
        var validTransitions = ValidateTransitions(
            activeTransitions,
            activeStepIds,
            issues);

        ValidateReachability(
            activeSteps,
            startSteps.Length == 1 ? startSteps[0] : null,
            validTransitions,
            issues);
        ValidateFieldPermissionCoverage(version, activeSteps, issues);
        return issues;
    }

    private static void ValidateBoundRequestType(
        WorkflowVersion version,
        ICollection<WorkflowValidationIssueResponse> issues)
    {
        if (version.RequestTypeVersion.Lifecycle != RequestTypeVersionLifecycle.Published
            || version.RequestTypeVersion.RequestType.IsArchived)
        {
            Add(issues, "workflow.request_type_version_ineligible", "requestTypeVersionId", "The workflow must be bound to a published request-type version whose request type is not archived.");
        }
    }

    private static IReadOnlyList<WorkflowTransition> ValidateTransitions(
        IReadOnlyList<WorkflowTransition> transitions,
        IReadOnlySet<Guid> activeStepIds,
        ICollection<WorkflowValidationIssueResponse> issues)
    {
        var valid = new List<WorkflowTransition>();

        foreach (var transition in transitions)
        {
            var path = $"transitions.{transition.Id}";
            var sourceValid = activeStepIds.Contains(transition.SourceStepId);

            if (!sourceValid)
            {
                Add(issues, "workflow.transition_source_inactive", $"{path}.sourceStepId", $"Active transition '{transition.Key}' must start at an active step.");
            }

            var targetValid = transition.TargetStepId is not Guid targetId
                || activeStepIds.Contains(targetId);

            if (!targetValid)
            {
                Add(issues, "workflow.transition_target_inactive", $"{path}.targetStepId", $"Active transition '{transition.Key}' must target an active step.");
            }

            if (transition.TerminalOutcome is null
                && transition.TargetStepId is null)
            {
                Add(issues, "workflow.transition_target_required", $"{path}.targetStepId", "A nonterminal transition requires a target step.");
                targetValid = false;
            }
            else if (transition.TerminalOutcome is not null
                     && transition.TargetStepId is not null)
            {
                Add(issues, "workflow.terminal_transition_has_target", $"{path}.targetStepId", "A terminal transition cannot have a target step.");
                targetValid = false;
            }

            if (!TerminalStatusMatchesOutcome(transition))
            {
                Add(issues, "workflow.terminal_status_mismatch", $"{path}.resultingStatus", "A terminal transition's resulting status must match its terminal outcome.");
            }

            ValidateActionSemantics(transition, path, issues);

            if (sourceValid && targetValid)
            {
                valid.Add(transition);
            }
        }

        return valid;
    }

    private static void ValidateReachability(
        IReadOnlyList<WorkflowStep> activeSteps,
        WorkflowStep? startStep,
        IReadOnlyList<WorkflowTransition> transitions,
        ICollection<WorkflowValidationIssueResponse> issues)
    {
        if (startStep is null)
        {
            return;
        }

        var outgoing = transitions
            .Where(transition => transition.TargetStepId is not null)
            .GroupBy(transition => transition.SourceStepId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(transition => transition.TargetStepId!.Value).ToArray());
        var reachable = new HashSet<Guid> { startStep.Id };
        var queue = new Queue<Guid>();
        queue.Enqueue(startStep.Id);

        while (queue.TryDequeue(out var current))
        {
            if (!outgoing.TryGetValue(current, out var targets))
            {
                continue;
            }

            foreach (var target in targets)
            {
                if (reachable.Add(target))
                {
                    queue.Enqueue(target);
                }
            }
        }

        foreach (var step in activeSteps.Where(step => !reachable.Contains(step.Id)))
        {
            Add(issues, "workflow.step_unreachable", $"steps.{step.Id}", $"Active step '{step.Key}' is not reachable from the start step.");
        }

        var reachableTerminalSources = transitions
            .Where(transition =>
                transition.TerminalOutcome is not null
                && reachable.Contains(transition.SourceStepId))
            .Select(transition => transition.SourceStepId)
            .ToHashSet();

        if (reachableTerminalSources.Count == 0)
        {
            Add(issues, "workflow.terminal_transition_required", "transitions", "At least one approved, rejected or completed terminal transition must be reachable from the start step.");
        }

        var canReachTerminal = new HashSet<Guid>(reachableTerminalSources);
        var nonterminal = transitions
            .Where(transition => transition.TargetStepId is not null)
            .ToArray();
        var changed = true;

        while (changed)
        {
            changed = false;

            foreach (var transition in nonterminal)
            {
                if (canReachTerminal.Contains(transition.TargetStepId!.Value)
                    && canReachTerminal.Add(transition.SourceStepId))
                {
                    changed = true;
                }
            }
        }

        foreach (var stepId in reachable.Where(stepId => !canReachTerminal.Contains(stepId)))
        {
            var step = activeSteps.Single(candidate => candidate.Id == stepId);
            Add(issues, "workflow.path_dead_end", $"steps.{step.Id}", $"Active step '{step.Key}' has no path to a terminal transition.");
        }
    }

    private static void ValidateFieldPermissionCoverage(
        WorkflowVersion version,
        IReadOnlyList<WorkflowStep> activeSteps,
        ICollection<WorkflowValidationIssueResponse> issues)
    {
        var activeFields = version.RequestTypeVersion.Fields
            .Where(field => field.IsActive)
            .OrderBy(field => field.SortOrder)
            .ToArray();

        foreach (var step in activeSteps)
        {
            var permissionByField = step.FieldPermissions
                .GroupBy(permission => permission.RequestFieldDefinitionId)
                .ToDictionary(group => group.Key, group => group.ToArray());

            foreach (var field in activeFields)
            {
                var path = $"steps.{step.Id}.fieldPermissions.{field.Id}";

                if (!permissionByField.TryGetValue(field.Id, out var permissions)
                    || permissions.Length == 0)
                {
                    Add(issues, "workflow.field_permission_required", path, $"Step '{step.Key}' requires a permission for active field '{field.Key}'.");
                    continue;
                }

                if (permissions.Length > 1)
                {
                    Add(issues, "workflow.field_permission_duplicate", path, $"Step '{step.Key}' has duplicate permissions for field '{field.Key}'.");
                    continue;
                }

                ValidatePermission(step, field, permissions[0], path, issues);
            }
        }
    }

    private static void ValidateCommentPolicies(
        IReadOnlyList<WorkflowStep> activeSteps,
        IReadOnlyList<WorkflowTransition> activeTransitions,
        ICollection<WorkflowValidationIssueResponse> issues)
    {
        foreach (var step in activeSteps)
        {
            var outgoing = activeTransitions
                .Where(transition => transition.SourceStepId == step.Id)
                .ToArray();

            foreach (var transition in outgoing)
            {
                if (step.CommentPolicy == WorkflowCommentPolicy.None
                    && transition.RequiresComment)
                {
                    Add(issues, "workflow.comment_policy_conflict", $"transitions.{transition.Id}.requiresComment", $"Step '{step.Key}' does not allow comments, so its actions cannot require one.");
                }

                if (step.CommentPolicy == WorkflowCommentPolicy.Required
                    && !transition.RequiresComment)
                {
                    Add(issues, "workflow.comment_policy_conflict", $"transitions.{transition.Id}.requiresComment", $"Every active action from step '{step.Key}' must require a comment.");
                }
            }
        }
    }

    private static void ValidateActionSemantics(
        WorkflowTransition transition,
        string path,
        ICollection<WorkflowValidationIssueResponse> issues)
    {
        var isTerminal = transition.TerminalOutcome is not null;
        var valid = transition.ActionType switch
        {
            WorkflowActionType.Submit =>
                !isTerminal
                && transition.ResultingStatus is ApprovalRequestStatus.Submitted
                    or ApprovalRequestStatus.InProgress,
            WorkflowActionType.Approve =>
                !isTerminal
                && transition.ResultingStatus == ApprovalRequestStatus.InProgress
                || transition.TerminalOutcome == WorkflowTerminalOutcome.Approved
                && transition.ResultingStatus == ApprovalRequestStatus.Approved,
            WorkflowActionType.Reject =>
                transition.TerminalOutcome == WorkflowTerminalOutcome.Rejected
                && transition.ResultingStatus == ApprovalRequestStatus.Rejected,
            WorkflowActionType.PutOnHold =>
                !isTerminal
                && transition.ResultingStatus == ApprovalRequestStatus.OnHold,
            WorkflowActionType.Resume =>
                !isTerminal
                && transition.ResultingStatus == ApprovalRequestStatus.InProgress,
            WorkflowActionType.RequestMoreInformation =>
                !isTerminal
                && transition.ResultingStatus
                == ApprovalRequestStatus.MoreInformationRequired,
            WorkflowActionType.Return =>
                !isTerminal
                && transition.ResultingStatus is ApprovalRequestStatus.InProgress
                    or ApprovalRequestStatus.MoreInformationRequired,
            WorkflowActionType.Forward =>
                !isTerminal
                && transition.ResultingStatus == ApprovalRequestStatus.InProgress,
            WorkflowActionType.Complete =>
                transition.TerminalOutcome == WorkflowTerminalOutcome.Completed
                && transition.ResultingStatus == ApprovalRequestStatus.Completed,
            _ => false
        };

        if (!valid)
        {
            Add(issues, "workflow.action_status_incompatible", $"{path}.actionType", $"Action '{transition.ActionType}' is incompatible with its resulting status and terminal outcome.");
        }
    }

    private static void ValidatePermission(
        WorkflowStep step,
        RequestFieldDefinition field,
        WorkflowStepFieldPermission permission,
        string path,
        ICollection<WorkflowValidationIssueResponse> issues)
    {
        var editable = permission.Access is WorkflowFieldAccess.Editable
            or WorkflowFieldAccess.EditableRequired;

        if (editable && !step.CanEditRequestData)
        {
            Add(issues, "workflow.field_edit_not_allowed", $"{path}.access", $"Step '{step.Key}' cannot grant editable access when request-data editing is disabled.");
        }

        var isDocument = field.FieldType is RequestFieldType.FileDocument
            or RequestFieldType.RichDocument;

        if (!isDocument)
        {
            if (permission.DocumentAccess is not null)
            {
                Add(issues, "workflow.document_access_non_document_field", $"{path}.documentAccess", $"Field '{field.Key}' is not a document field and cannot have document access.");
            }

            if (permission.CanSelectForForwarding)
            {
                Add(issues, "workflow.forwarding_non_document_field", $"{path}.canSelectForForwarding", $"Field '{field.Key}' is not a document field and cannot be selected for forwarding.");
            }

            return;
        }

        if (permission.DocumentAccess is null)
        {
            Add(issues, "workflow.document_access_required", $"{path}.documentAccess", $"Document field '{field.Key}' requires a document access mode.");
            return;
        }

        if (permission.Access == WorkflowFieldAccess.Hidden
            && (permission.DocumentAccess is WorkflowDocumentAccess.View
                or WorkflowDocumentAccess.Edit
                || permission.CanSelectForForwarding))
        {
            Add(issues, "workflow.hidden_field_document_access", path, $"Hidden document field '{field.Key}' cannot be viewed, edited or selected for forwarding.");
        }

        if (permission.DocumentAccess is WorkflowDocumentAccess.View or WorkflowDocumentAccess.Edit
            && !step.CanOpenDocuments)
        {
            Add(issues, "workflow.document_open_not_allowed", $"{path}.documentAccess", $"Step '{step.Key}' cannot grant document viewing when opening documents is disabled.");
        }

        if (permission.DocumentAccess == WorkflowDocumentAccess.Edit
            && !step.CanEditDocuments)
        {
            Add(issues, "workflow.document_edit_not_allowed", $"{path}.documentAccess", $"Step '{step.Key}' cannot grant document editing when document editing is disabled.");
        }

        if (permission.CanSelectForForwarding
            && (!step.CanOpenDocuments
                || !step.CanForwardDocuments
                || permission.DocumentAccess == WorkflowDocumentAccess.Hidden))
        {
            Add(issues, "workflow.document_forward_not_allowed", $"{path}.canSelectForForwarding", $"Step '{step.Key}' cannot forward this document with its current capabilities and access mode.");
        }
    }

    private static bool TerminalStatusMatchesOutcome(WorkflowTransition transition) =>
        transition.TerminalOutcome switch
        {
            null => true,
            WorkflowTerminalOutcome.Approved =>
                transition.ResultingStatus == ApprovalRequestStatus.Approved,
            WorkflowTerminalOutcome.Rejected =>
                transition.ResultingStatus == ApprovalRequestStatus.Rejected,
            WorkflowTerminalOutcome.Completed =>
                transition.ResultingStatus == ApprovalRequestStatus.Completed,
            _ => false
        };

    private static void Add(
        ICollection<WorkflowValidationIssueResponse> issues,
        string code,
        string path,
        string message) =>
        issues.Add(new WorkflowValidationIssueResponse(code, path, message));
}
