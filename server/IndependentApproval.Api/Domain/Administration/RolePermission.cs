namespace IndependentApproval.Api.Domain.Administration;

public sealed class RolePermission
{
    public Guid Id { get; set; }

    public Guid ApplicationRoleId { get; set; }

    public Guid PermissionId { get; set; }

    public DateTimeOffset GrantedAtUtc { get; set; }

    public string GrantedByAccount { get; set; } = string.Empty;

    public Guid? GrantedByUserId { get; set; }

    public DateTimeOffset? RemovedAtUtc { get; set; }

    public string? RemovedByAccount { get; set; }

    public Guid? RemovedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ApplicationRole ApplicationRole { get; set; } = null!;

    public Permission Permission { get; set; } = null!;
}
