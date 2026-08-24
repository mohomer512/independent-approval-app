namespace IndependentApproval.Api.Application.Authorization;

public static class AccountNameNormalizer
{
    public static string Normalize(string? accountName) =>
        accountName?.Trim().ToUpperInvariant() ?? string.Empty;

    public static (string Domain, string UserName) Parse(string accountName)
    {
        var separatorIndex = accountName.IndexOf('\\');

        if (separatorIndex > 0 && separatorIndex < accountName.Length - 1)
        {
            return (accountName[..separatorIndex], accountName[(separatorIndex + 1)..]);
        }

        separatorIndex = accountName.LastIndexOf('@');

        if (separatorIndex > 0 && separatorIndex < accountName.Length - 1)
        {
            return (accountName[(separatorIndex + 1)..], accountName[..separatorIndex]);
        }

        return (string.Empty, accountName);
    }
}
