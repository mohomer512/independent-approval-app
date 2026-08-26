using IndependentApproval.Api.Domain.Administration;

namespace IndependentApproval.Api.Domain.Workflows;

public sealed class WorkflowVersionStarterRole
{
    public Guid WorkflowVersionId { get; set; }

    public Guid ApplicationRoleId { get; set; }

    public WorkflowVersion WorkflowVersion { get; set; } = null!;

    public ApplicationRole ApplicationRole { get; set; } = null!;
}
