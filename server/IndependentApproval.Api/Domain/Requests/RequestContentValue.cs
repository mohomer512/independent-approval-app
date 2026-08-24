using IndependentApproval.Api.Domain.Administration;

namespace IndependentApproval.Api.Domain.Requests;

public sealed class RequestContentValue
{
    public Guid Id { get; set; }

    public Guid ApprovalRequestId { get; set; }

    public Guid RequestTypeVersionId { get; set; }

    public Guid RequestFieldDefinitionId { get; set; }

    public string ValueJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid CreatedByApplicationUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public Guid? ModifiedByApplicationUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ApprovalRequest ApprovalRequest { get; set; } = null!;

    public RequestFieldDefinition RequestFieldDefinition { get; set; } = null!;

    public ApplicationUser CreatedByApplicationUser { get; set; } = null!;

    public ApplicationUser? ModifiedByApplicationUser { get; set; }
}
