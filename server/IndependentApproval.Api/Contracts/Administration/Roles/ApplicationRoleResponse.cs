namespace IndependentApproval.Api.Contracts.Administration.Roles;

public sealed record ApplicationRoleResponse(
    Guid Id,
    string Code,
    string NameEnglish,
    string NameArabic,
    string? DescriptionEnglish,
    string? DescriptionArabic,
    bool IsActive,
    bool IsArchived,
    IReadOnlyList<string> PermissionCodes,
    int UserCount,
    DateTimeOffset CreatedAtUtc,
    string CreatedByAccount,
    DateTimeOffset? ModifiedAtUtc,
    string? ModifiedByAccount,
    string RowVersion);
