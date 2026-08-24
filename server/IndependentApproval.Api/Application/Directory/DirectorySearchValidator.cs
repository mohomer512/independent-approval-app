namespace IndependentApproval.Api.Application.Directory;

public static class DirectorySearchValidator
{
    public static string ValidateAndNormalize(
        string? query,
        int page,
        int pageSize,
        DirectoryOptions options)
    {
        var normalizedQuery = query?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            throw new DirectorySearchValidationException(
                "query",
                "Directory search text is required.");
        }

        if (normalizedQuery.Length < options.MinimumSearchLength
            || normalizedQuery.Length > options.MaximumSearchLength)
        {
            throw new DirectorySearchValidationException(
                "query",
                $"Directory search text must be between {options.MinimumSearchLength} and " +
                $"{options.MaximumSearchLength} characters.");
        }

        if (page < 1 || page > options.MaximumPage)
        {
            throw new DirectorySearchValidationException(
                "page",
                $"Page must be between 1 and {options.MaximumPage}.");
        }

        if (pageSize < 1 || pageSize > options.MaximumPageSize)
        {
            throw new DirectorySearchValidationException(
                "pageSize",
                $"Page size must be between 1 and {options.MaximumPageSize}.");
        }

        if ((long)page * pageSize > options.MaximumResultCount)
        {
            throw new DirectorySearchValidationException(
                "page",
                $"The requested page exceeds the {options.MaximumResultCount}-result search bound.");
        }

        return normalizedQuery;
    }
}

public sealed class DirectorySearchValidationException(
    string field,
    string message) : Exception(message)
{
    public string Field { get; } = field;
}
