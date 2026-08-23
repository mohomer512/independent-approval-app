using IndependentApproval.Api.Contracts.Documents;

namespace IndependentApproval.Api.Application.Documents;

public interface IDocumentService
{
    Task<DocumentResponse> UploadAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken);

    Task<DocumentListResponse> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DocumentDownload?> GetDownloadAsync(
        Guid id,
        CancellationToken cancellationToken);
}

public sealed record DocumentDownload(
    Stream Content,
    string ContentType,
    string OriginalFileName);
