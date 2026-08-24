using IndependentApproval.Api.Domain.Administration;

namespace IndependentApproval.Api.Domain.Requests;

public sealed class RequestTypeSlugReservation
{
    public string NormalizedSlug { get; set; } = string.Empty;

    public Guid RequestTypeId { get; set; }

    public DateTimeOffset ReservedAtUtc { get; set; }

    public string ReservedByAccount { get; set; } = string.Empty;

    public Guid? ReservedByUserId { get; set; }

    public RequestType RequestType { get; set; } = null!;

    public ApplicationUser? ReservedByUser { get; set; }

    public ICollection<RequestTypeVersion> Versions { get; set; } = [];
}
