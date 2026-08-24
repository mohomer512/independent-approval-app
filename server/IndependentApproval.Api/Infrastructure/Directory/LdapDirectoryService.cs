using System.DirectoryServices.Protocols;
using System.Net;
using IndependentApproval.Api.Application.Authorization;
using IndependentApproval.Api.Application.Directory;
using Microsoft.Extensions.Options;

namespace IndependentApproval.Api.Infrastructure.ActiveDirectory;

public sealed class LdapDirectoryService(
    IOptions<DirectoryOptions> options,
    ILogger<LdapDirectoryService> logger) : IDirectoryService
{
    private static readonly string[] UserAttributes =
    [
        "objectSid",
        "objectGUID",
        "sAMAccountName",
        "userPrincipalName",
        "displayName",
        "mail"
    ];

    private readonly DirectoryOptions _options = options.Value;

    public async Task<DirectorySearchResult> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = DirectorySearchValidator.ValidateAndNormalize(
            query,
            page,
            pageSize,
            _options);
        var escapedQuery = LdapFilterEscaper.EscapeAssertionValue(normalizedQuery);
        var filter =
            "(&(objectCategory=person)(objectClass=user)(sAMAccountName=*)" +
            $"(|(displayName=*{escapedQuery}*)" +
            $"(sAMAccountName=*{escapedQuery}*)" +
            $"(userPrincipalName=*{escapedQuery}*)" +
            $"(mail=*{escapedQuery}*)))";

        try
        {
            using var connection = CreateConnection();
            byte[] cookie = [];

            for (var currentPage = 1; currentPage <= page; currentPage++)
            {
                var request = CreateSearchRequest(filter, SearchScope.Subtree);
                var pageControl = new PageResultRequestControl(pageSize)
                {
                    Cookie = cookie
                };
                request.Controls.Add(pageControl);
                var response = await SendAsync(connection, request, cancellationToken);
                var responseControl = response.Controls
                    .OfType<PageResultResponseControl>()
                    .SingleOrDefault();
                cookie = responseControl?.Cookie ?? [];

                if (currentPage == page)
                {
                    var users = response.Entries
                        .Cast<SearchResultEntry>()
                        .Select(CreateDirectoryUser)
                        .OfType<DirectoryUser>()
                        .Take(pageSize)
                        .ToArray();

                    return new DirectorySearchResult(
                        users,
                        page,
                        pageSize,
                        cookie.Length > 0);
                }

                if (cookie.Length == 0)
                {
                    return new DirectorySearchResult([], page, pageSize, false);
                }
            }

            return new DirectorySearchResult([], page, pageSize, false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsDirectoryFailure(exception))
        {
            logger.LogWarning(
                exception,
                "The configured directory search failed.");
            throw new DirectoryServiceUnavailableException();
        }
    }

    public async Task<DirectoryUser?> FindBySidAsync(
        byte[] sid,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sid);

        if (sid.Length is < 1 or > 68)
        {
            return null;
        }

        var escapedSid = LdapFilterEscaper.EscapeBinary(sid);
        var filter =
            "(&(objectCategory=person)(objectClass=user)" +
            $"(objectSid={escapedSid}))";
        return await FindSingleAsync(filter, cancellationToken);
    }

    public async Task<DirectoryUser?> FindByAccountNameAsync(
        string accountName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountName) || accountName.Length > 320)
        {
            return null;
        }

        var normalizedAccountName = accountName.Trim();
        var (domain, userName) = AccountNameNormalizer.Parse(normalizedAccountName);

        if (!IsConfiguredDomain(domain) || string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        var escapedUserName = LdapFilterEscaper.EscapeAssertionValue(userName);
        var escapedAccountName = LdapFilterEscaper.EscapeAssertionValue(normalizedAccountName);
        var filter =
            "(&(objectCategory=person)(objectClass=user)(sAMAccountName=*)" +
            $"(|(sAMAccountName={escapedUserName})" +
            $"(userPrincipalName={escapedAccountName})))";
        return await FindSingleAsync(filter, cancellationToken);
    }

    private async Task<DirectoryUser?> FindSingleAsync(
        string filter,
        CancellationToken cancellationToken)
    {
        try
        {
            using var connection = CreateConnection();
            var request = CreateSearchRequest(filter, SearchScope.Subtree);
            request.SizeLimit = 2;
            var response = await SendAsync(connection, request, cancellationToken);

            return response.Entries
                .Cast<SearchResultEntry>()
                .Select(CreateDirectoryUser)
                .FirstOrDefault(user => user is not null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsDirectoryFailure(exception))
        {
            logger.LogWarning(
                exception,
                "The configured directory lookup failed.");
            throw new DirectoryServiceUnavailableException();
        }
    }

    private LdapConnection CreateConnection()
    {
        var identifier = new LdapDirectoryIdentifier(
            _options.Server,
            _options.Port,
            fullyQualifiedDnsHostName: false,
            connectionless: false);
        var connection = new LdapConnection(identifier)
        {
            AuthType = AuthType.Negotiate,
            AutoBind = true,
            Timeout = TimeSpan.FromSeconds(_options.SearchTimeoutSeconds)
        };
        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
        connection.SessionOptions.SecureSocketLayer = _options.UseSsl;

        if (!_options.UseSsl)
        {
            connection.SessionOptions.Signing = true;
            connection.SessionOptions.Sealing = true;
        }

        if (!string.IsNullOrWhiteSpace(_options.CredentialUserName))
        {
            connection.Credential = new NetworkCredential(
                _options.CredentialUserName,
                _options.CredentialPassword,
                _options.CredentialDomain);
        }

        return connection;
    }

    private SearchRequest CreateSearchRequest(string filter, SearchScope scope) =>
        new(_options.SearchBase, filter, scope, UserAttributes)
        {
            TimeLimit = TimeSpan.FromSeconds(_options.SearchTimeoutSeconds)
        };

    private async Task<SearchResponse> SendAsync(
        LdapConnection connection,
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        var asyncResult = connection.BeginSendRequest(
            request,
            PartialResultProcessing.NoPartialResultSupport,
            callback: null,
            state: null);

        try
        {
            var responseTask = Task<DirectoryResponse>.Factory.FromAsync(
                asyncResult,
                connection.EndSendRequest);
            var response = await responseTask.WaitAsync(
                TimeSpan.FromSeconds(_options.SearchTimeoutSeconds),
                cancellationToken);
            return (SearchResponse)response;
        }
        catch
        {
            try
            {
                connection.Abort(asyncResult);
            }
            catch (Exception abortException)
            {
                logger.LogDebug(
                    abortException,
                    "The directory request could not be aborted after it failed.");
            }

            throw;
        }
    }

    private DirectoryUser? CreateDirectoryUser(SearchResultEntry entry)
    {
        var sid = GetBinaryAttribute(entry, "objectSid");
        var samAccountName = GetStringAttribute(entry, "sAMAccountName");

        if (sid is null || sid.Length is < 1 or > 68
            || string.IsNullOrWhiteSpace(samAccountName))
        {
            return null;
        }

        var objectGuidBytes = GetBinaryAttribute(entry, "objectGUID");
        Guid? objectGuid = objectGuidBytes?.Length == 16
            ? new Guid(objectGuidBytes)
            : null;
        var normalizedSamAccountName = samAccountName.Trim();

        return new DirectoryUser(
            [.. sid],
            objectGuid,
            $"{_options.NetBiosDomainName}\\{normalizedSamAccountName}",
            _options.NetBiosDomainName,
            normalizedSamAccountName,
            NormalizeOptional(GetStringAttribute(entry, "userPrincipalName")),
            NormalizeOptional(GetStringAttribute(entry, "displayName")),
            NormalizeOptional(GetStringAttribute(entry, "mail")));
    }

    private bool IsConfiguredDomain(string domain) =>
        string.IsNullOrWhiteSpace(domain)
        || string.Equals(
            domain,
            _options.NetBiosDomainName,
            StringComparison.OrdinalIgnoreCase)
        || string.Equals(
            domain,
            _options.DomainName,
            StringComparison.OrdinalIgnoreCase);

    private static string? GetStringAttribute(
        SearchResultEntry entry,
        string attributeName)
    {
        var attribute = entry.Attributes[attributeName];

        if (attribute is null || attribute.Count == 0)
        {
            return null;
        }

        return attribute.GetValues(typeof(string))
            .OfType<string>()
            .FirstOrDefault();
    }

    private static byte[]? GetBinaryAttribute(
        SearchResultEntry entry,
        string attributeName)
    {
        var attribute = entry.Attributes[attributeName];

        if (attribute is null || attribute.Count == 0)
        {
            return null;
        }

        return attribute.GetValues(typeof(byte[]))
            .OfType<byte[]>()
            .FirstOrDefault();
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool IsDirectoryFailure(Exception exception) =>
        exception is LdapException
        or DirectoryOperationException
        or TimeoutException
        or ObjectDisposedException;
}
