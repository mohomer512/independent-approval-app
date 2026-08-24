namespace IndependentApproval.Api.Domain.Administration;

public sealed class ApplicationUser
{
    public Guid Id { get; set; }

    public byte[] AdSid { get; set; } = [];

    public Guid? AdObjectGuid { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string SamAccountName { get; set; } = string.Empty;

    public string? UserPrincipalName { get; set; }

    public string NormalizedAccountName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsLocked { get; set; }

    public string? LockReason { get; set; }

    public bool IsRemoved { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string CreatedByAccount { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedByAccount { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset? LockedAtUtc { get; set; }

    public string? LockedByAccount { get; set; }

    public Guid? LockedByUserId { get; set; }

    public DateTimeOffset? RemovedAtUtc { get; set; }

    public string? RemovedByAccount { get; set; }

    public Guid? RemovedByUserId { get; set; }

    public DateTimeOffset? LastSuccessfulAccessAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<ApplicationUserRole> RoleAssignments { get; set; } = [];
}
