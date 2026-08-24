namespace IndependentApproval.Api.Domain.Requests;

public static class RequestSystemFieldKeys
{
    public const string RequestNumber = "requestNumber";
    public const string Status = "status";
    public const string RequestedBy = "requestedBy";
    public const string Title = "title";
    public const string CreatedAtUtc = "createdAtUtc";
    public const string ModifiedAtUtc = "modifiedAtUtc";
    public const string SubmittedAtUtc = "submittedAtUtc";
    public const string CompletedAtUtc = "completedAtUtc";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        RequestNumber,
        Status,
        RequestedBy,
        Title,
        CreatedAtUtc,
        ModifiedAtUtc,
        SubmittedAtUtc,
        CompletedAtUtc
    };
}

public static class RequestNumberFormat
{
    public const int SequenceDigits = 6;
    public const long FirstSequenceValue = 1;
}
