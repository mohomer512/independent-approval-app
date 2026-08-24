namespace IndependentApproval.Api.Domain.Administration;

public sealed class ApplicationUserRole
{
    public Guid Id { get; set; }

    public Guid ApplicationUserId { get; set; }

    public Guid ApplicationRoleId { get; set; }

    public DateTimeOffset AssignedAtUtc { get; set; }

    public string AssignedByAccount { get; set; } = string.Empty;

    public Guid? AssignedByUserId { get; set; }

    public DateTimeOffset? RemovedAtUtc { get; set; }

    public string? RemovedByAccount { get; set; }

    public Guid? RemovedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ApplicationUser ApplicationUser { get; set; } = null!;

    public ApplicationRole ApplicationRole { get; set; } = null!;
}
