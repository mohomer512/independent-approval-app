using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Documents;

namespace IndependentApproval.Api.Domain.Requests;

public sealed class RequestDocument
{
    public Guid Id { get; set; }

    public Guid ApprovalRequestId { get; set; }

    public Guid RequestTypeVersionId { get; set; }

    public Guid RequestFieldDefinitionId { get; set; }

    public Guid DocumentId { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset AttachedAtUtc { get; set; }

    public Guid AttachedByApplicationUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ApprovalRequest ApprovalRequest { get; set; } = null!;

    public RequestFieldDefinition RequestFieldDefinition { get; set; } = null!;

    public DocumentRecord Document { get; set; } = null!;

    public ApplicationUser AttachedByApplicationUser { get; set; } = null!;
}
