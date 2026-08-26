using IndependentApproval.Api.Domain.Administration;

namespace IndependentApproval.Api.Domain.Workflows;

public sealed class WorkflowStepRole
{
    public Guid WorkflowStepId { get; set; }

    public Guid WorkflowVersionId { get; set; }

    public Guid ApplicationRoleId { get; set; }

    public WorkflowStep WorkflowStep { get; set; } = null!;

    public WorkflowVersion WorkflowVersion { get; set; } = null!;

    public ApplicationRole ApplicationRole { get; set; } = null!;
}
