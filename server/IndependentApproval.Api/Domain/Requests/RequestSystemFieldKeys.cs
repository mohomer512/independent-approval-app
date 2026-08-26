namespace IndependentApproval.Api.Domain.Requests;

public static class RequestSystemFieldKeys
{
    public const string Id = "id";
    public const string RequestNumber = "requestNumber";
    public const string RequestTypeId = "requestTypeId";
    public const string RequestTypeVersionId = "requestTypeVersionId";
    public const string WorkflowDefinitionId = "workflowDefinitionId";
    public const string WorkflowVersionId = "workflowVersionId";
    public const string CurrentWorkflowStepId = "currentWorkflowStepId";
    public const string Status = "status";
    public const string RequestedBy = "requestedBy";
    public const string Title = "title";
    public const string CreatedAtUtc = "createdAtUtc";
    public const string ModifiedAtUtc = "modifiedAtUtc";
    public const string SubmittedAtUtc = "submittedAtUtc";
    public const string CompletedAtUtc = "completedAtUtc";
    public const string RowVersion = "rowVersion";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        Id,
        RequestNumber,
        RequestTypeId,
        RequestTypeVersionId,
        WorkflowDefinitionId,
        WorkflowVersionId,
        CurrentWorkflowStepId,
        Status,
        RequestedBy,
        Title,
        CreatedAtUtc,
        ModifiedAtUtc,
        SubmittedAtUtc,
        CompletedAtUtc,
        RowVersion
    };
}

public static class RequestNumberFormat
{
    public const int SequenceDigits = 6;
    public const long FirstSequenceValue = 1;
}
