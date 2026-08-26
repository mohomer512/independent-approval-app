using IndependentApproval.Api.Domain.Workflows;

namespace IndependentApproval.Api.Domain.Requests;

public sealed class RequestFieldDefinition
{
    public Guid Id { get; set; }

    public Guid RequestTypeVersionId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string NormalizedKey { get; set; } = string.Empty;

    public string LabelEnglish { get; set; } = string.Empty;

    public string LabelArabic { get; set; } = string.Empty;

    public string? HelpTextEnglish { get; set; }

    public string? HelpTextArabic { get; set; }

    public RequestFieldType FieldType { get; set; }

    public bool IsRequired { get; set; }

    public string? DefaultValueJson { get; set; }

    public string? ValidationConfigurationJson { get; set; }

    public string? ChoiceConfigurationJson { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DocumentFieldMode? DocumentMode { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string CreatedByAccount { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedByAccount { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public RequestTypeVersion RequestTypeVersion { get; set; } = null!;

    public ICollection<RequestContentValue> ContentValues { get; set; } = [];

    public ICollection<RequestDocument> Documents { get; set; } = [];

    public ICollection<RequestRichDocumentRevision> RichDocumentRevisions { get; set; } = [];

    public ICollection<WorkflowStepFieldPermission> WorkflowStepFieldPermissions { get; set; } = [];
}
