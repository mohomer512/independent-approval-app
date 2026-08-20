namespace IndependentApproval.Api.Domain.Documents;

public sealed class DocumentRecord
{
    public Guid Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = "Available";

    public string UploadedBy { get; set; } = string.Empty;

    public DateTimeOffset UploadedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
