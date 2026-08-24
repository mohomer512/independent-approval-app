namespace IndependentApproval.Api.Application.Administration;

public sealed record AdministrationActor(string AccountName, Guid? ApplicationUserId);

public interface IAdministrationActorAccessor
{
    Task<AdministrationActor> GetCurrentAsync(CancellationToken cancellationToken);
}
