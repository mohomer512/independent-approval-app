namespace IndependentApproval.Api.Domain.Requests;

public sealed class RequestTypeVersion
{
    public Guid Id { get; set; }

    public Guid RequestTypeId { get; set; }

    public int VersionNumber { get; set; }

    public string NameEnglish { get; set; } = string.Empty;

    public string NameArabic { get; set; } = string.Empty;

    public string DescriptionEnglish { get; set; } = string.Empty;

    public string DescriptionArabic { get; set; } = string.Empty;

    public string RequestPrefix { get; set; } = string.Empty;

    public string NavigationSlug { get; set; } = string.Empty;

    public int NavigationOrder { get; set; }

    public RequestTypeVersionLifecycle Lifecycle { get; set; } = RequestTypeVersionLifecycle.Draft;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string CreatedByAccount { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedByAccount { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; set; }

    public string? PublishedByAccount { get; set; }

    public Guid? PublishedByUserId { get; set; }

    public DateTimeOffset? ArchivedAtUtc { get; set; }

    public string? ArchivedByAccount { get; set; }

    public Guid? ArchivedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public RequestType RequestType { get; set; } = null!;

    public RequestTypePrefixReservation PrefixReservation { get; set; } = null!;

    public RequestTypeSlugReservation SlugReservation { get; set; } = null!;

    public ICollection<RequestFieldDefinition> Fields { get; set; } = [];

    public ICollection<ApprovalRequest> Requests { get; set; } = [];
}
