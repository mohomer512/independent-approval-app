namespace IndependentApproval.Api.Infrastructure.Storage;

public interface IDocumentStorage
{
    Task<StoredDocument> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken);
}

public sealed record StoredDocument(
    string StorageKey,
    long FileSize);

public sealed class DocumentStorageSizeLimitExceededException()
    : Exception("The document content exceeded the configured storage limit.");

public sealed class EmptyDocumentStorageContentException()
    : Exception("The document content was empty.");
