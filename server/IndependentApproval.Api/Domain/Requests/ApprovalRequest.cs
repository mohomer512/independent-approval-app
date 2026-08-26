using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Workflows;

namespace IndependentApproval.Api.Domain.Requests;

public sealed class ApprovalRequest
{
    public Guid Id { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public Guid RequestTypeId { get; set; }

    public Guid RequestTypeVersionId { get; set; }

    public Guid WorkflowDefinitionId { get; set; }

    public Guid WorkflowVersionId { get; set; }

    public Guid? CurrentWorkflowStepId { get; set; }

    public string Title { get; set; } = string.Empty;

    public ApprovalRequestStatus Status { get; set; } = ApprovalRequestStatus.Draft;

    public Guid RequestedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public DateTimeOffset? SubmittedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public RequestType RequestType { get; set; } = null!;

    public RequestTypeVersion RequestTypeVersion { get; set; } = null!;

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    public WorkflowVersion WorkflowVersion { get; set; } = null!;

    public WorkflowStep? CurrentWorkflowStep { get; set; }

    public ApplicationUser RequestedByUser { get; set; } = null!;

    public ICollection<RequestContentValue> ContentValues { get; set; } = [];

    public ICollection<RequestDocument> Documents { get; set; } = [];

    public ICollection<RequestRichDocumentRevision> RichDocumentRevisions { get; set; } = [];
}
