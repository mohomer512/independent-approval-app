namespace IndependentApproval.Api.Contracts.Administration.Directory;

public sealed record DirectoryUserSearchResponse(
    IReadOnlyList<DirectoryUserSearchItemResponse> Items,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record DirectoryUserSearchItemResponse(
    string SelectionToken,
    string AccountName,
    string Domain,
    string UserName,
    string? UserPrincipalName,
    string? DisplayName,
    string? Email);
