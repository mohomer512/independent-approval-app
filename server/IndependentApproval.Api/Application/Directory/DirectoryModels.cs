namespace IndependentApproval.Api.Application.Directory;

public sealed record DirectorySearchResult(
    IReadOnlyList<DirectoryUser> Items,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record DirectoryUser(
    byte[] Sid,
    Guid? ObjectGuid,
    string AccountName,
    string Domain,
    string SamAccountName,
    string? UserPrincipalName,
    string? DisplayName,
    string? Email);
