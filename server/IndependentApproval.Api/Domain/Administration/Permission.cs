namespace IndependentApproval.Api.Domain.Administration;

public sealed class Permission
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string NameEnglish { get; set; } = string.Empty;

    public string NameArabic { get; set; } = string.Empty;

    public string DescriptionEnglish { get; set; } = string.Empty;

    public string DescriptionArabic { get; set; } = string.Empty;

    public ICollection<RolePermission> RoleAssignments { get; set; } = [];
}
