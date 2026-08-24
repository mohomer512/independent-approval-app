namespace IndependentApproval.Api.Contracts.Administration.Users;

public sealed record RoleReferenceResponse(
    Guid Id,
    string Code,
    string NameEnglish,
    string NameArabic,
    bool IsArchived);
