using IndependentApproval.Api.Contracts.Administration.Directory;

namespace IndependentApproval.Api.Application.Administration;

public interface IDirectoryAdministrationService
{
    Task<DirectoryUserSearchResponse> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
