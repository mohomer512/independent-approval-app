using System.Buffers;
using Microsoft.Extensions.Options;

namespace IndependentApproval.Api.Infrastructure.Storage;

public sealed class FileSystemDocumentStorage : IDocumentStorage
{
    private const int StreamBufferSize = 64 * 1024;

    private readonly ILogger<FileSystemDocumentStorage> _logger;
    private readonly long _maxFileSizeBytes;
    private readonly string _rootPath;
    private readonly string _rootPathPrefix;
    private readonly HashSet<string> _allowedExtensions;
    private readonly StringComparison _pathComparison;

    public FileSystemDocumentStorage(
        IOptions<DocumentStorageOptions> options,
        ILogger<FileSystemDocumentStorage> logger)
    {
        _logger = logger;
        _maxFileSizeBytes = options.Value.MaxFileSizeBytes;
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        _rootPathPrefix = Path.EndsInDirectorySeparator(_rootPath)
            ? _rootPath
            : $"{_rootPath}{Path.DirectorySeparatorChar}";
        _allowedExtensions = options.Value.AllowedExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    public async Task<StoredDocument> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedExtension = DocumentStorageOptions.NormalizeExtension(extension);

        if (!_allowedExtensions.Contains(normalizedExtension))
        {
            throw new ArgumentException("The file extension is not allowed.", nameof(extension));
        }

        Directory.CreateDirectory(_rootPath);

        var storageKey = $"{Guid.NewGuid():N}{normalizedExtension}";
        var temporaryKey = $"{Guid.NewGuid():N}.uploading";
        var finalPath = ResolveChildPath(storageKey);
        var temporaryPath = ResolveChildPath(temporaryKey);
        var moveCompleted = false;
        long fileSize;

        try
        {
            await using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                StreamBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                fileSize = await CopyWithSizeLimitAsync(content, destination, cancellationToken);

                if (fileSize == 0)
                {
                    throw new EmptyDocumentStorageContentException();
                }

                await destination.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, finalPath, overwrite: false);
            moveCompleted = true;

            return new StoredDocument(storageKey, fileSize);
        }
        finally
        {
            if (!moveCompleted)
            {
                TryDeleteTemporaryFile(temporaryPath, temporaryKey);
            }
        }
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = ResolveStoragePath(storageKey);

        try
        {
            Stream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                StreamBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            return Task.FromResult<Stream?>(stream);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = ResolveStoragePath(storageKey);
        File.Delete(filePath);

        return Task.CompletedTask;
    }

    private string ResolveStoragePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new InvalidOperationException("The document storage key is invalid.");
        }

        var extension = Path.GetExtension(storageKey);
        var identifier = Path.GetFileNameWithoutExtension(storageKey);

        if (!Guid.TryParseExact(identifier, "N", out _)
            || !_allowedExtensions.Contains(extension)
            || !string.Equals(extension, extension.ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The document storage key is invalid.");
        }

        return ResolveChildPath(storageKey);
    }

    private string ResolveChildPath(string fileName)
    {
        if (Path.IsPathFullyQualified(fileName)
            || fileName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0
            || !string.Equals(Path.GetFileName(fileName), fileName, _pathComparison))
        {
            throw new InvalidOperationException("The document storage key is invalid.");
        }

        var resolvedPath = Path.GetFullPath(Path.Combine(_rootPath, fileName));

        if (!resolvedPath.StartsWith(_rootPathPrefix, _pathComparison))
        {
            throw new InvalidOperationException("The resolved document path is outside the storage root.");
        }

        return resolvedPath;
    }

    private void TryDeleteTemporaryFile(string temporaryPath, string temporaryKey)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(
                exception,
                "Unable to delete incomplete document upload {TemporaryKey}.",
                temporaryKey);
        }
    }

    private async Task<long> CopyWithSizeLimitAsync(
        Stream source,
        Stream destination,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        long totalBytesRead = 0;

        try
        {
            while (true)
            {
                var bytesRead = await source.ReadAsync(
                    buffer.AsMemory(0, StreamBufferSize),
                    cancellationToken);

                if (bytesRead == 0)
                {
                    return totalBytesRead;
                }

                totalBytesRead += bytesRead;

                if (totalBytesRead > _maxFileSizeBytes)
                {
                    throw new DocumentStorageSizeLimitExceededException();
                }

                await destination.WriteAsync(
                    buffer.AsMemory(0, bytesRead),
                    cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }
}
