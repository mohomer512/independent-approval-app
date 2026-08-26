using IndependentApproval.Api.Domain.Administration;

namespace IndependentApproval.Api.Domain.Workflows;

public sealed class WorkflowSlugReservation
{
    public string NormalizedSlug { get; set; } = string.Empty;

    public Guid WorkflowDefinitionId { get; set; }

    public DateTimeOffset ReservedAtUtc { get; set; }

    public string ReservedByAccount { get; set; } = string.Empty;

    public Guid? ReservedByUserId { get; set; }

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    public ApplicationUser? ReservedByUser { get; set; }

    public ICollection<WorkflowVersion> Versions { get; set; } = [];
}
