namespace IndependentApproval.Api.Application.Requests;

public interface IRequestNumberGenerator
{
    Task<string> GenerateAsync(
        Guid requestTypeVersionId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);
}
