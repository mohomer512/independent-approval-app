namespace IndependentApproval.Api.Contracts.Administration.Roles;

public sealed record UpdateApplicationRoleRequest(
    string? NameEnglish,
    string? NameArabic,
    string? DescriptionEnglish,
    string? DescriptionArabic,
    bool IsActive,
    IReadOnlyList<string>? PermissionCodes,
    string? RowVersion);
