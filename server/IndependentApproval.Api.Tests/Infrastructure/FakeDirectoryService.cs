using System.Collections.Concurrent;
using IndependentApproval.Api.Application.Directory;

namespace IndependentApproval.Api.Tests.Infrastructure;

internal sealed class FakeDirectoryService : IDirectoryService
{
    private readonly IReadOnlyList<DirectoryUser> _searchUsers;
    private readonly IReadOnlyDictionary<string, DirectoryUser> _usersBySid;
    private readonly IReadOnlyDictionary<string, DirectoryUser> _usersByAccountName;
    private readonly ConcurrentQueue<DirectorySearchCall> _searchCalls = new();

    public FakeDirectoryService()
    {
        AddableUser = CreateUser(
            2101,
            "TEST\\directory.addable",
            Guid.Parse("40d14651-0501-4168-a7f8-1beaa9a05f01"),
            "Directory Addable User",
            "directory.addable@test.local");
        DuplicateSidUser = CreateUser(
            1101,
            "TEST\\directory.sid-duplicate",
            Guid.Parse("40d14651-0501-4168-a7f8-1beaa9a05f02"),
            "Duplicate SID Candidate",
            "directory.sid-duplicate@test.local");
        DuplicateAccountUser = CreateUser(
            2103,
            "test\\member",
            Guid.Parse("40d14651-0501-4168-a7f8-1beaa9a05f03"),
            "Normalized Account Duplicate",
            "normalized-duplicate@test.local");
        RefreshedMember = CreateUser(
            1102,
            AdministrationApiFixture.ApplicationUserAccount,
            Guid.Parse("40d14651-0501-4168-a7f8-1beaa9a05f04"),
            "Refreshed Directory Profile",
            "member.refreshed@test.local");
        CurrentSessionMember = RefreshedMember with
        {
            Sid = [.. RefreshedMember.Sid],
            DisplayName = "Directory Session Member"
        };
        BootstrapAdministrator = CreateUser(
            1101,
            AdministrationApiFixture.BootstrapAdministratorAccount,
            Guid.Parse("40d14651-0501-4168-a7f8-1beaa9a05f05"),
            "Directory Preferred Administrator",
            "sp.admin@test.local");

        _searchUsers =
        [
            AddableUser,
            DuplicateSidUser,
            DuplicateAccountUser
        ];
        _usersBySid = new Dictionary<string, DirectoryUser>(StringComparer.Ordinal)
        {
            [SidKey(AddableUser.Sid)] = AddableUser,
            [SidKey(DuplicateSidUser.Sid)] = DuplicateSidUser,
            [SidKey(DuplicateAccountUser.Sid)] = DuplicateAccountUser,
            [SidKey(RefreshedMember.Sid)] = RefreshedMember
        };
        _usersByAccountName = new Dictionary<string, DirectoryUser>(
            StringComparer.OrdinalIgnoreCase)
        {
            [AdministrationApiFixture.BootstrapAdministratorAccount] =
                BootstrapAdministrator,
            [AdministrationApiFixture.ApplicationUserAccount] = CurrentSessionMember
        };
    }

    public DirectoryUser AddableUser { get; }

    public DirectoryUser DuplicateSidUser { get; }

    public DirectoryUser DuplicateAccountUser { get; }

    public DirectoryUser RefreshedMember { get; }

    public DirectoryUser CurrentSessionMember { get; }

    public DirectoryUser BootstrapAdministrator { get; }

    public IReadOnlyCollection<DirectorySearchCall> SearchCalls =>
        _searchCalls.ToArray();

    public Task<DirectorySearchResult> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _searchCalls.Enqueue(new DirectorySearchCall(query, page, pageSize));

        var matches = _searchUsers
            .Where(user => Matches(user, query))
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .ToArray();
        var hasMore = matches.Length > pageSize;

        return Task.FromResult(new DirectorySearchResult(
            matches.Take(pageSize).Select(Clone).ToArray(),
            page,
            pageSize,
            hasMore));
    }

    public Task<DirectoryUser?> FindBySidAsync(
        byte[] sid,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = _usersBySid.GetValueOrDefault(SidKey(sid));
        return Task.FromResult(user is null ? null : Clone(user));
    }

    public Task<DirectoryUser?> FindByAccountNameAsync(
        string accountName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = _usersByAccountName.GetValueOrDefault(accountName.Trim());
        return Task.FromResult(user is null ? null : Clone(user));
    }

    private static DirectoryUser CreateUser(
        int sidRid,
        string accountName,
        Guid objectGuid,
        string displayName,
        string email)
    {
        var separator = accountName.IndexOf('\\');
        var domain = accountName[..separator].ToUpperInvariant();
        var userName = accountName[(separator + 1)..];

        return new DirectoryUser(
            AdministrationApiFixture.CreateBinarySid(sidRid),
            objectGuid,
            $"{domain}\\{userName}",
            domain,
            userName,
            $"{userName}@test.local",
            displayName,
            email);
    }

    private static bool Matches(DirectoryUser user, string query) =>
        user.AccountName.Contains(query, StringComparison.OrdinalIgnoreCase)
        || user.SamAccountName.Contains(query, StringComparison.OrdinalIgnoreCase)
        || (user.UserPrincipalName?.Contains(
            query,
            StringComparison.OrdinalIgnoreCase) ?? false)
        || (user.DisplayName?.Contains(
            query,
            StringComparison.OrdinalIgnoreCase) ?? false)
        || (user.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

    private static DirectoryUser Clone(DirectoryUser user) =>
        user with { Sid = [.. user.Sid] };

    private static string SidKey(byte[] sid) => Convert.ToHexString(sid);
}

internal sealed record DirectorySearchCall(
    string Query,
    int Page,
    int PageSize);
