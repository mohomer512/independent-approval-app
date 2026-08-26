using IndependentApproval.Api.Domain.Requests;

namespace IndependentApproval.Api.Domain.Workflows;

public sealed class WorkflowStep
{
    public Guid Id { get; set; }

    public Guid WorkflowVersionId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string NormalizedKey { get; set; } = string.Empty;

    public string NameEnglish { get; set; } = string.Empty;

    public string NameArabic { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public decimal DiagramX { get; set; }

    public decimal DiagramY { get; set; }

    public bool IsStartStep { get; set; }

    public bool IsActive { get; set; } = true;

    public bool CanEditRequestData { get; set; }

    public bool CanOpenDocuments { get; set; } = true;

    public bool CanEditDocuments { get; set; }

    public bool CanForwardDocuments { get; set; }

    public WorkflowCommentPolicy CommentPolicy { get; set; } = WorkflowCommentPolicy.Optional;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string CreatedByAccount { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedByAccount { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public WorkflowVersion WorkflowVersion { get; set; } = null!;

    public ICollection<WorkflowStepRole> AssignedRoles { get; set; } = [];

    public ICollection<WorkflowTransition> OutgoingTransitions { get; set; } = [];

    public ICollection<WorkflowTransition> IncomingTransitions { get; set; } = [];

    public ICollection<WorkflowStepFieldPermission> FieldPermissions { get; set; } = [];

    public ICollection<ApprovalRequest> CurrentRequests { get; set; } = [];
}
