using IndependentApproval.Api.Application.Directory;
using IndependentApproval.Api.Contracts.Administration.Directory;
using Microsoft.Extensions.Options;

namespace IndependentApproval.Api.Application.Administration;

public sealed class DirectoryAdministrationService(
    IDirectoryService directoryService,
    IDirectorySelectionTokenService selectionTokenService,
    IOptions<DirectoryOptions> directoryOptions) :
    IDirectoryAdministrationService
{
    public async Task<DirectoryUserSearchResponse> SearchAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        string normalizedQuery;

        try
        {
            normalizedQuery = DirectorySearchValidator.ValidateAndNormalize(
                query,
                page,
                pageSize,
                directoryOptions.Value);
        }
        catch (DirectorySearchValidationException exception)
        {
            throw AdministrationValidationException.For(
                exception.Field,
                exception.Message);
        }

        try
        {
            var result = await directoryService.SearchAsync(
                normalizedQuery,
                page,
                pageSize,
                cancellationToken);
            var items = result.Items
                .Take(pageSize)
                .Select(user => new DirectoryUserSearchItemResponse(
                    selectionTokenService.Create(user),
                    user.AccountName,
                    user.Domain,
                    user.SamAccountName,
                    user.UserPrincipalName,
                    user.DisplayName,
                    user.Email))
                .ToArray();

            return new DirectoryUserSearchResponse(
                items,
                page,
                pageSize,
                result.HasMore);
        }
        catch (DirectorySearchValidationException exception)
        {
            throw AdministrationValidationException.For(
                exception.Field,
                exception.Message);
        }
        catch (DirectoryServiceUnavailableException)
        {
            throw new AdministrationServiceUnavailableException(
                "directory.unavailable",
                "The configured directory could not be searched. Try again later.");
        }
    }
}
