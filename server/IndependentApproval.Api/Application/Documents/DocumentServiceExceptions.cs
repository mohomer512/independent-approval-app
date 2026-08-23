namespace IndependentApproval.Api.Application.Documents;

public sealed class DocumentValidationException(
    string field,
    string message) : Exception(message)
{
    public string Field { get; } = field;
}

public sealed class DocumentIdentityUnavailableException()
    : Exception("An authenticated Windows identity is required.");

public sealed class DocumentFileTooLargeException(long maximumFileSizeBytes)
    : Exception($"The document file must not exceed {maximumFileSizeBytes} bytes.");
