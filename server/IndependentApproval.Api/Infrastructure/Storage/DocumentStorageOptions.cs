namespace IndependentApproval.Api.Infrastructure.Storage;

public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";
    public const long DefaultMaxFileSizeBytes = 25L * 1024 * 1024;
    public const long MultipartBodyLengthLimitBytes = DefaultMaxFileSizeBytes + (1L * 1024 * 1024);

    public string RootPath { get; set; } = string.Empty;

    public long MaxFileSizeBytes { get; set; }

    public string[] AllowedExtensions { get; set; } = [];

    internal void NormalizeAllowedExtensions()
    {
        AllowedExtensions = (AllowedExtensions ?? [])
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Select(NormalizeExtension)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string NormalizeExtension(string extension)
    {
        var normalizedExtension = extension.Trim();

        if (!normalizedExtension.StartsWith('.'))
        {
            normalizedExtension = $".{normalizedExtension}";
        }

        return normalizedExtension.ToLowerInvariant();
    }
}
