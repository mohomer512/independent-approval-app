namespace IndependentApproval.Api.Contracts.Documents;

public sealed record DocumentResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string? Description,
    string Status,
    string UploadedBy,
    DateTimeOffset UploadedAtUtc,
    string DownloadUrl,
    string RowVersion);
