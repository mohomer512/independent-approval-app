using IndependentApproval.Api.Domain.Requests;

namespace IndependentApproval.Api.Domain.Workflows;

public sealed class WorkflowTransition
{
    public Guid Id { get; set; }

    public Guid WorkflowVersionId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string NormalizedKey { get; set; } = string.Empty;

    public Guid SourceStepId { get; set; }

    public Guid? TargetStepId { get; set; }

    public string ActionLabelEnglish { get; set; } = string.Empty;

    public string ActionLabelArabic { get; set; } = string.Empty;

    public WorkflowActionType ActionType { get; set; }

    public ApprovalRequestStatus ResultingStatus { get; set; }

    public bool RequiresComment { get; set; }

    public WorkflowTerminalOutcome? TerminalOutcome { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string CreatedByAccount { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedByAccount { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public WorkflowVersion WorkflowVersion { get; set; } = null!;

    public WorkflowStep SourceStep { get; set; } = null!;

    public WorkflowStep? TargetStep { get; set; }
}
