namespace IndependentApproval.Api.Domain.Administration;

public sealed class ApplicationRole
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string NormalizedCode { get; set; } = string.Empty;

    public string NameEnglish { get; set; } = string.Empty;

    public string NameArabic { get; set; } = string.Empty;

    public string? DescriptionEnglish { get; set; }

    public string? DescriptionArabic { get; set; }

    public bool IsActive { get; set; } = true;

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

    public ICollection<ApplicationUserRole> UserAssignments { get; set; } = [];

    public ICollection<RolePermission> PermissionAssignments { get; set; } = [];
}
