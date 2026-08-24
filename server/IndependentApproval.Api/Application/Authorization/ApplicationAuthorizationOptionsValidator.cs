using Microsoft.Extensions.Options;

namespace IndependentApproval.Api.Application.Authorization;

public sealed class ApplicationAuthorizationOptionsValidator :
    IValidateOptions<ApplicationAuthorizationOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        ApplicationAuthorizationOptions options)
    {
        var accounts = options.SystemAdministratorAccounts
            .Select(AccountNameNormalizer.Normalize)
            .Where(account => account.Length > 0)
            .ToArray();

        if (accounts.Length == 0)
        {
            return ValidateOptionsResult.Fail(
                $"At least one bootstrap administrator account must be configured at " +
                $"'{ApplicationAuthorizationOptions.SectionName}:SystemAdministratorAccounts'.");
        }

        if (accounts.Any(account => account.Length > 256))
        {
            return ValidateOptionsResult.Fail(
                "A bootstrap administrator account cannot exceed 256 characters.");
        }

        if (accounts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != accounts.Length)
        {
            return ValidateOptionsResult.Fail(
                "Bootstrap administrator accounts must be unique.");
        }

        options.SystemAdministratorAccounts = accounts;
        return ValidateOptionsResult.Success;
    }
}
