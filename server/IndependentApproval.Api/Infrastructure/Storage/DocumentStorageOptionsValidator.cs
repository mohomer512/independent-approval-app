using Microsoft.Extensions.Options;

namespace IndependentApproval.Api.Infrastructure.Storage;

public sealed class DocumentStorageOptionsValidator(IHostEnvironment hostEnvironment)
    : IValidateOptions<DocumentStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, DocumentStorageOptions options)
    {
        var failures = new List<string>();

        ValidateRootPath(options.RootPath, failures);

        if (options.MaxFileSizeBytes <= 0)
        {
            failures.Add("DocumentStorage:MaxFileSizeBytes must be greater than zero.");
        }

        if (options.AllowedExtensions.Length == 0)
        {
            failures.Add("DocumentStorage:AllowedExtensions must contain at least one extension.");
        }
        else if (options.AllowedExtensions.Any(extension => !IsValidExtension(extension)))
        {
            failures.Add(
                "DocumentStorage:AllowedExtensions entries must contain only a leading dot followed by letters or digits.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private void ValidateRootPath(string rootPath, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            failures.Add("DocumentStorage:RootPath is required.");
            return;
        }

        if (!Path.IsPathFullyQualified(rootPath))
        {
            failures.Add("DocumentStorage:RootPath must be an absolute path.");
            return;
        }

        try
        {
            var normalizedRootPath = Path.GetFullPath(rootPath);
            var normalizedContentRootPath = Path.GetFullPath(hostEnvironment.ContentRootPath);

            if (IsSamePathOrDescendant(normalizedRootPath, normalizedContentRootPath))
            {
                failures.Add("DocumentStorage:RootPath must be outside the application deployment directory.");
            }
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            failures.Add("DocumentStorage:RootPath is not a valid absolute path.");
        }
    }

    private static bool IsValidExtension(string extension)
    {
        return extension.Length > 1
            && extension[0] == '.'
            && extension.Skip(1).All(char.IsLetterOrDigit);
    }

    private static bool IsSamePathOrDescendant(string candidatePath, string parentPath)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var normalizedCandidatePath = Path.TrimEndingDirectorySeparator(candidatePath);
        var normalizedParentPath = Path.TrimEndingDirectorySeparator(parentPath);

        if (string.Equals(normalizedCandidatePath, normalizedParentPath, comparison))
        {
            return true;
        }

        var parentPathPrefix = Path.EndsInDirectorySeparator(normalizedParentPath)
            ? normalizedParentPath
            : $"{normalizedParentPath}{Path.DirectorySeparatorChar}";

        return normalizedCandidatePath.StartsWith(parentPathPrefix, comparison);
    }
}
