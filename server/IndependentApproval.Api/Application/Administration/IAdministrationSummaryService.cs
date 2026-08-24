using IndependentApproval.Api.Contracts.Administration;

namespace IndependentApproval.Api.Application.Administration;

public interface IAdministrationSummaryService
{
    Task<AdministrationSummaryResponse> GetAsync(CancellationToken cancellationToken);
}
