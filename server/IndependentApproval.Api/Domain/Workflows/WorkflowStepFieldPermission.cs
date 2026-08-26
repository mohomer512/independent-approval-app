using IndependentApproval.Api.Domain.Requests;

namespace IndependentApproval.Api.Domain.Workflows;

public sealed class WorkflowStepFieldPermission
{
    public Guid Id { get; set; }

    public Guid WorkflowVersionId { get; set; }

    public Guid RequestTypeVersionId { get; set; }

    public Guid WorkflowStepId { get; set; }

    public Guid RequestFieldDefinitionId { get; set; }

    public WorkflowFieldAccess Access { get; set; }

    public WorkflowDocumentAccess? DocumentAccess { get; set; }

    public bool CanSelectForForwarding { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string CreatedByAccount { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedByAccount { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public WorkflowVersion WorkflowVersion { get; set; } = null!;

    public WorkflowStep WorkflowStep { get; set; } = null!;

    public RequestFieldDefinition RequestFieldDefinition { get; set; } = null!;
}
