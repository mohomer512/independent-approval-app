using IndependentApproval.Api.Contracts.Documents;
using IndependentApproval.Api.Domain.Documents;
using IndependentApproval.Api.Infrastructure.Persistence;
using IndependentApproval.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace IndependentApproval.Api.Application.Documents;

public sealed class DocumentService(
    IndependentApprovalDbContext dbContext,
    IDocumentStorage documentStorage,
    IOptions<DocumentStorageOptions> storageOptions,
    IHttpContextAccessor httpContextAccessor,
    LinkGenerator linkGenerator,
    ILogger<DocumentService> logger) : IDocumentService
{
    private const string AvailableStatus = "Available";
    private const string DefaultContentType = "application/octet-stream";
    private const int MaximumDescriptionLength = 2000;
    private const int MaximumFileNameLength = 255;
    private const int MaximumContentTypeLength = 255;
    private const int MaximumPageSize = 100;

    private readonly HashSet<string> _allowedExtensions =
        storageOptions.Value.AllowedExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<DocumentResponse> UploadAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var uploadedBy = GetAuthenticatedAccountName();

        if (request is null || request.File is null)
        {
            throw new DocumentValidationException("File", "A document file is required.");
        }

        var file = request.File;

        if (file.Length <= 0)
        {
            throw new DocumentValidationException("File", "The document file must not be empty.");
        }

        if (file.Length > storageOptions.Value.MaxFileSizeBytes)
        {
            throw new DocumentFileTooLargeException(storageOptions.Value.MaxFileSizeBytes);
        }

        var originalFileName = GetValidatedFileName(file.FileName);
        var extension = DocumentStorageOptions.NormalizeExtension(Path.GetExtension(originalFileName));

        if (!_allowedExtensions.Contains(extension))
        {
            throw new DocumentValidationException("File", "The document file extension is not allowed.");
        }

        var description = NormalizeDescription(request.Description);
        var contentType = NormalizeContentType(file.ContentType);
        StoredDocument storedDocument;

        await using (var content = file.OpenReadStream())
        {
            try
            {
                storedDocument = await documentStorage.SaveAsync(content, extension, cancellationToken);
            }
            catch (DocumentStorageSizeLimitExceededException)
            {
                throw new DocumentFileTooLargeException(storageOptions.Value.MaxFileSizeBytes);
            }
            catch (EmptyDocumentStorageContentException)
            {
                throw new DocumentValidationException("File", "The document file must not be empty.");
            }
        }

        var document = new DocumentRecord
        {
            Id = Guid.NewGuid(),
            OriginalFileName = originalFileName,
            StorageKey = storedDocument.StorageKey,
            ContentType = contentType,
            FileSize = storedDocument.FileSize,
            Description = description,
            Status = AvailableStatus,
            UploadedBy = uploadedBy
        };

        try
        {
            dbContext.Documents.Add(document);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DeleteForCompensationAsync(storedDocument.StorageKey);
            throw;
        }

        return CreateResponse(document);
    }

    public async Task<DocumentListResponse> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        GetAuthenticatedAccountName();
        ValidatePagination(page, pageSize);

        var query = dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Status == AvailableStatus);

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (long)(page - 1) * pageSize;

        if (skip >= totalCount)
        {
            return new DocumentListResponse([], page, pageSize, totalCount);
        }

        var documents = await query
            .OrderByDescending(document => document.UploadedAtUtc)
            .ThenByDescending(document => document.Id)
            .Skip((int)skip)
            .Take(pageSize)
            .Select(document => new DocumentProjection(
                document.Id,
                document.OriginalFileName,
                document.ContentType,
                document.FileSize,
                document.Description,
                document.Status,
                document.UploadedBy,
                document.UploadedAtUtc,
                document.RowVersion))
            .ToListAsync(cancellationToken);

        return new DocumentListResponse(
            documents.Select(CreateResponse).ToArray(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<DocumentDownload?> GetDownloadAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        GetAuthenticatedAccountName();

        var document = await dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == id && document.Status == AvailableStatus)
            .Select(document => new
            {
                document.StorageKey,
                document.ContentType,
                document.OriginalFileName
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return null;
        }

        var content = await documentStorage.OpenReadAsync(document.StorageKey, cancellationToken);

        if (content is null)
        {
            logger.LogWarning(
                "Document {DocumentId} has Available metadata but its stored file is missing.",
                id);

            return null;
        }

        return new DocumentDownload(content, document.ContentType, document.OriginalFileName);
    }

    private string GetAuthenticatedAccountName()
    {
        var identity = httpContextAccessor.HttpContext?.User.Identity;

        if (identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(identity.Name))
        {
            throw new DocumentIdentityUnavailableException();
        }

        return identity.Name;
    }

    private static string GetValidatedFileName(string? suppliedFileName)
    {
        var originalFileName = Path.GetFileName(suppliedFileName?.Trim());

        if (string.IsNullOrWhiteSpace(originalFileName)
            || originalFileName is "." or ".."
            || originalFileName.Length > MaximumFileNameLength
            || originalFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new DocumentValidationException("File", "The document filename is invalid.");
        }

        return originalFileName;
    }

    private static string? NormalizeDescription(string? description)
    {
        var normalizedDescription = description?.Trim();

        if (normalizedDescription?.Length > MaximumDescriptionLength)
        {
            throw new DocumentValidationException(
                "Description",
                $"The document description must not exceed {MaximumDescriptionLength} characters.");
        }

        return string.IsNullOrWhiteSpace(normalizedDescription)
            ? null
            : normalizedDescription;
    }

    private static string NormalizeContentType(string? suppliedContentType)
    {
        var contentType = suppliedContentType?.Trim();

        if (string.IsNullOrWhiteSpace(contentType)
            || contentType.Length > MaximumContentTypeLength
            || !MediaTypeHeaderValue.TryParse(contentType, out var parsedContentType))
        {
            return DefaultContentType;
        }

        var normalizedContentType = parsedContentType.ToString();

        return normalizedContentType.Length <= MaximumContentTypeLength
            ? normalizedContentType
            : DefaultContentType;
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new DocumentValidationException("page", "Page must be greater than or equal to 1.");
        }

        if (pageSize < 1 || pageSize > MaximumPageSize)
        {
            throw new DocumentValidationException(
                "pageSize",
                $"Page size must be between 1 and {MaximumPageSize}.");
        }
    }

    private DocumentResponse CreateResponse(DocumentRecord document)
    {
        return new DocumentResponse(
            document.Id,
            document.OriginalFileName,
            document.ContentType,
            document.FileSize,
            document.Description,
            document.Status,
            document.UploadedBy,
            document.UploadedAtUtc,
            GetDownloadUrl(document.Id),
            WebEncoders.Base64UrlEncode(document.RowVersion));
    }

    private DocumentResponse CreateResponse(DocumentProjection document)
    {
        return new DocumentResponse(
            document.Id,
            document.OriginalFileName,
            document.ContentType,
            document.FileSize,
            document.Description,
            document.Status,
            document.UploadedBy,
            document.UploadedAtUtc,
            GetDownloadUrl(document.Id),
            WebEncoders.Base64UrlEncode(document.RowVersion));
    }

    private string GetDownloadUrl(Guid id)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("An active HTTP context is required.");

        return linkGenerator.GetPathByName(
                httpContext,
                DocumentRoutes.Download,
                values: new { id })
            ?? throw new InvalidOperationException("The document download route could not be generated.");
    }

    private async Task DeleteForCompensationAsync(string storageKey)
    {
        try
        {
            await documentStorage.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unable to delete a stored document after its metadata could not be saved.");
        }
    }

    private sealed record DocumentProjection(
        Guid Id,
        string OriginalFileName,
        string ContentType,
        long FileSize,
        string? Description,
        string Status,
        string UploadedBy,
        DateTimeOffset UploadedAtUtc,
        byte[] RowVersion);
}
