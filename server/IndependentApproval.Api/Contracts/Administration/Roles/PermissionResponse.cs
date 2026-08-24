namespace IndependentApproval.Api.Contracts.Administration.Roles;

public sealed record PermissionResponse(
    string Code,
    string NameEnglish,
    string NameArabic,
    string DescriptionEnglish,
    string DescriptionArabic);
