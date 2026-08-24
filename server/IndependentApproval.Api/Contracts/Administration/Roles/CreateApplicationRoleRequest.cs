namespace IndependentApproval.Api.Contracts.Administration.Roles;

public sealed record CreateApplicationRoleRequest(
    string? Code,
    string? NameEnglish,
    string? NameArabic,
    string? DescriptionEnglish,
    string? DescriptionArabic,
    IReadOnlyList<string>? PermissionCodes);
