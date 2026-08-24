using IndependentApproval.Api.Application.Directory;
using Microsoft.Extensions.Options;

namespace IndependentApproval.Api.Infrastructure.ActiveDirectory;

public sealed class DirectoryOptionsValidator : IValidateOptions<DirectoryOptions>
{
    public ValidateOptionsResult Validate(string? name, DirectoryOptions options)
    {
        var failures = new List<string>();

        Require(options.Server, nameof(options.Server), 255, failures);
        Require(options.DomainName, nameof(options.DomainName), 255, failures);
        Require(options.NetBiosDomainName, nameof(options.NetBiosDomainName), 64, failures);
        Require(options.SearchBase, nameof(options.SearchBase), 1000, failures);

        if (options.Port is < 1 or > 65535)
        {
            failures.Add("Directory port must be between 1 and 65535.");
        }

        if (options.MinimumSearchLength is < 1 or > 100)
        {
            failures.Add("Minimum directory search length must be between 1 and 100.");
        }

        if (options.MaximumSearchLength < options.MinimumSearchLength
            || options.MaximumSearchLength > 500)
        {
            failures.Add(
                "Maximum directory search length must be at least the minimum and no more than 500.");
        }

        if (options.DefaultPageSize is < 1
            || options.DefaultPageSize > options.MaximumPageSize)
        {
            failures.Add("Default directory page size must be within the page-size limit.");
        }

        if (options.MaximumPageSize is < 1 or > 100)
        {
            failures.Add("Maximum directory page size must be between 1 and 100.");
        }

        if (options.MaximumPage is < 1 or > 100)
        {
            failures.Add("Maximum directory page must be between 1 and 100.");
        }

        if (options.MaximumResultCount is < 1 or > 5000
            || options.MaximumResultCount < options.DefaultPageSize)
        {
            failures.Add("Maximum directory result count must be between the default page size and 5000.");
        }

        if (options.SearchTimeoutSeconds is < 1 or > 60)
        {
            failures.Add("Directory search timeout must be between 1 and 60 seconds.");
        }

        if (options.SelectionTokenLifetimeMinutes is < 1 or > 60)
        {
            failures.Add("Directory selection-token lifetime must be between 1 and 60 minutes.");
        }

        var hasUserName = !string.IsNullOrWhiteSpace(options.CredentialUserName);
        var hasPassword = !string.IsNullOrWhiteSpace(options.CredentialPassword);

        if (hasUserName != hasPassword)
        {
            failures.Add(
                "Directory credential username and password must either both be supplied by the environment or both be omitted.");
        }

        if (options.CredentialUserName?.Length > 256
            || options.CredentialPassword?.Length > 1024
            || options.CredentialDomain?.Length > 255)
        {
            failures.Add("A directory credential value exceeds its supported length.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(
        string? value,
        string propertyName,
        int maximumLength,
        ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"Directory {propertyName} is required.");
        }
        else if (value.Length > maximumLength)
        {
            failures.Add($"Directory {propertyName} cannot exceed {maximumLength} characters.");
        }
    }
}
