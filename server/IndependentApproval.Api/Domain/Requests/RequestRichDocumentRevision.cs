using IndependentApproval.Api.Domain.Administration;

namespace IndependentApproval.Api.Domain.Requests;

public sealed class RequestRichDocumentRevision
{
    public Guid Id { get; set; }

    public Guid ApprovalRequestId { get; set; }

    public Guid RequestTypeVersionId { get; set; }

    public Guid RequestFieldDefinitionId { get; set; }

    public int RevisionNumber { get; set; }

    public string ContentHtml { get; set; } = string.Empty;

    public string ContentSha256 { get; set; } = string.Empty;

    public int ContentLength { get; set; }

    public string SanitizerVersion { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByApplicationUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ApprovalRequest ApprovalRequest { get; set; } = null!;

    public RequestFieldDefinition RequestFieldDefinition { get; set; } = null!;

    public ApplicationUser CreatedByApplicationUser { get; set; } = null!;
}
