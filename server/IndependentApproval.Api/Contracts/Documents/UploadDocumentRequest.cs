namespace IndependentApproval.Api.Contracts.Documents;

public sealed record UploadDocumentRequest
{
    public IFormFile File { get; init; } = null!;

    public string? Description { get; init; }
}
