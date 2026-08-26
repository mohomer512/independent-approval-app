using IndependentApproval.Api.Domain.Requests;

namespace IndependentApproval.Api.Domain.Workflows;

public sealed class WorkflowDefinition
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string NormalizedCode { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string CreatedByAccount { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedByAccount { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset? ArchivedAtUtc { get; set; }

    public string? ArchivedByAccount { get; set; }

    public Guid? ArchivedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<WorkflowVersion> Versions { get; set; } = [];

    public ICollection<WorkflowSlugReservation> SlugReservations { get; set; } = [];

    public ICollection<ApprovalRequest> Requests { get; set; } = [];
}
