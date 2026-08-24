namespace IndependentApproval.Api.Application.Directory;

public interface IDirectoryService
{
    Task<DirectorySearchResult> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DirectoryUser?> FindBySidAsync(
        byte[] sid,
        CancellationToken cancellationToken);

    Task<DirectoryUser?> FindByAccountNameAsync(
        string accountName,
        CancellationToken cancellationToken);
}
