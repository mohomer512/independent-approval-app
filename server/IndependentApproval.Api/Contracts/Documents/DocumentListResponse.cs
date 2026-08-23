namespace IndependentApproval.Api.Contracts.Documents;

public sealed record DocumentListResponse(
    IReadOnlyList<DocumentResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
