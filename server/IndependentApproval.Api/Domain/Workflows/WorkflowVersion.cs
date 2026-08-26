using IndependentApproval.Api.Domain.Requests;

namespace IndependentApproval.Api.Domain.Workflows;

public sealed class WorkflowVersion
{
    public Guid Id { get; set; }

    public Guid WorkflowDefinitionId { get; set; }

    public Guid RequestTypeVersionId { get; set; }

    public int VersionNumber { get; set; }

    public string NameEnglish { get; set; } = string.Empty;

    public string NameArabic { get; set; } = string.Empty;

    public string DescriptionEnglish { get; set; } = string.Empty;

    public string DescriptionArabic { get; set; } = string.Empty;

    public string NavigationLabelEnglish { get; set; } = string.Empty;

    public string NavigationLabelArabic { get; set; } = string.Empty;

    public string NavigationSlug { get; set; } = string.Empty;

    public int NavigationOrder { get; set; }

    public WorkflowVersionLifecycle Lifecycle { get; set; } = WorkflowVersionLifecycle.Draft;

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

    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;

    public RequestTypeVersion RequestTypeVersion { get; set; } = null!;

    public WorkflowSlugReservation SlugReservation { get; set; } = null!;

    public ICollection<WorkflowVersionStarterRole> StarterRoles { get; set; } = [];

    public ICollection<WorkflowStep> Steps { get; set; } = [];

    public ICollection<WorkflowTransition> Transitions { get; set; } = [];

    public ICollection<WorkflowStepFieldPermission> FieldPermissions { get; set; } = [];

    public ICollection<ApprovalRequest> Requests { get; set; } = [];
}
