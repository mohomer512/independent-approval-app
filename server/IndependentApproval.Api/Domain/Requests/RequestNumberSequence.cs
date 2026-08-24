namespace IndependentApproval.Api.Domain.Requests;

public sealed class RequestNumberSequence
{
    public string NormalizedPrefix { get; set; } = string.Empty;

    public int Year { get; set; }

    public long NextValue { get; set; } = RequestNumberFormat.FirstSequenceValue;

    public byte[] RowVersion { get; set; } = [];
}
