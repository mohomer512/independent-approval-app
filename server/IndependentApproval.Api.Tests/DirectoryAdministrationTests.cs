using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IndependentApproval.Api.Contracts.Administration.Directory;
using IndependentApproval.Api.Contracts.Administration.Users;
using IndependentApproval.Api.Contracts.Auth;
using IndependentApproval.Api.Contracts.Security;
using IndependentApproval.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IndependentApproval.Api.Tests;

public sealed class DirectoryAdministrationTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    private static readonly JsonSerializerOptions WebJson =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Directory_search_is_bounded_and_delegated_to_fake()
    {
        using var client = CreateAdministratorClient();
        var previousCallCount = fixture.DirectoryService.SearchCalls.Count;

        using var response = await client.GetAsync(
            "/api/admin/directory/users?query=Directory&page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<DirectoryUserSearchResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.Items.Count <= result.PageSize);
        var call = Assert.Single(
            fixture.DirectoryService.SearchCalls.Skip(previousCallCount));
        Assert.Equal(new DirectorySearchCall("Directory", 1, 2), call);
    }

    [Theory]
    [InlineData("/api/admin/directory/users?query=a&page=1&pageSize=25")]
    [InlineData("/api/admin/directory/users?query=valid&page=0&pageSize=25")]
    [InlineData("/api/admin/directory/users?query=valid&page=1&pageSize=51")]
    [InlineData("/api/admin/directory/users?query=valid&page=6&pageSize=50")]
    public async Task Directory_search_rejects_requests_outside_configured_bounds(
        string requestUri)
    {
        using var client = CreateAdministratorClient();
        var previousCallCount = fixture.DirectoryService.SearchCalls.Count;

        using var response = await client.GetAsync(requestUri);

        await AssertProblemAsync(
            response,
            HttpStatusCode.BadRequest,
            "administration.validation_failed");
        Assert.Equal(
            previousCallCount,
            fixture.DirectoryService.SearchCalls.Count);
    }

    [Fact]
    public async Task Directory_search_returns_only_safe_selection_fields()
    {
        using var client = CreateAdministratorClient();

        using var response = await client.GetAsync(
            "/api/admin/directory/users?query=directory.addable&page=1&pageSize=25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DirectoryUserSearchResponse>(json, WebJson);
        var item = Assert.Single(Assert.IsType<DirectoryUserSearchResponse>(result).Items);
        Assert.False(string.IsNullOrWhiteSpace(item.SelectionToken));
        Assert.Equal(fixture.DirectoryService.AddableUser.AccountName, item.AccountName);
        Assert.DoesNotContain("\"sid\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"objectGuid\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "\"distinguishedName\":",
            json,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DC=TEST,DC=local", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Add_selected_directory_user_persists_immutable_identity_internally()
    {
        using var client = CreateAdministratorClient();
        var selection = await SearchOneAsync(client, "directory.addable");

        using var response = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/admin/users",
            new AddApplicationUserRequest(
                selection.SelectionToken,
                [fixture.ApplicationRoleId]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseJson = await response.Content.ReadAsStringAsync();
        var added = JsonSerializer.Deserialize<ApplicationUserResponse>(
            responseJson,
            WebJson);
        Assert.NotNull(added);
        Assert.Equal(fixture.DirectoryService.AddableUser.AccountName, added.AccountName);
        Assert.DoesNotContain("\"adSid\":", responseJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "\"adObjectGuid\":",
            responseJson,
            StringComparison.OrdinalIgnoreCase);

        var stored = await fixture.QueryDatabaseAsync(async dbContext =>
            await dbContext.ApplicationUsers
                .AsNoTracking()
                .Where(user => user.Id == added.Id)
                .Select(user => new PersistedDirectoryIdentity(
                    user.AdSid,
                    user.AdObjectGuid))
                .SingleAsync());
        Assert.True(fixture.DirectoryService.AddableUser.Sid.SequenceEqual(stored.Sid));
        Assert.Equal(
            fixture.DirectoryService.AddableUser.ObjectGuid,
            stored.ObjectGuid);
    }

    [Theory]
    [InlineData("sid-duplicate")]
    [InlineData("Normalized Account")]
    public async Task Duplicate_sid_or_normalized_account_returns_conflict(
        string query)
    {
        using var client = CreateAdministratorClient();
        var selection = await SearchOneAsync(client, query);

        using var response = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/admin/users",
            new AddApplicationUserRequest(
                selection.SelectionToken,
                [fixture.ApplicationRoleId]));

        await AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            "administration.duplicate_application_user");
    }

    [Fact]
    public async Task Invalid_and_tampered_selection_tokens_are_rejected()
    {
        using var client = CreateAdministratorClient();
        var selection = await SearchOneAsync(client, "directory.addable");
        var invalidTokens = new[]
        {
            "not-a-selection-token",
            $"{selection.SelectionToken}.tampered"
        };

        foreach (var invalidToken in invalidTokens)
        {
            using var response = await SendWithAntiforgeryAsync(
                client,
                HttpMethod.Post,
                "/api/admin/users",
                new AddApplicationUserRequest(
                    invalidToken,
                    [fixture.ApplicationRoleId]));

            await AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "directory.invalid_selection_token");
        }
    }

    [Fact]
    public async Task Add_directory_user_requires_at_least_one_role()
    {
        using var client = CreateAdministratorClient();
        var selection = await SearchOneAsync(client, "directory.addable");

        using var response = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/admin/users",
            new AddApplicationUserRequest(selection.SelectionToken, []));

        await AssertProblemAsync(
            response,
            HttpStatusCode.BadRequest,
            "administration.validation_failed");
    }

    [Fact]
    public async Task Refresh_updates_mutable_profile_and_rejects_stale_row_version()
    {
        using var client = CreateAdministratorClient();
        var original = await client.GetFromJsonAsync<ApplicationUserResponse>(
            $"/api/admin/users/{fixture.ApplicationUserId}");
        Assert.NotNull(original);

        using var refreshResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/users/{fixture.ApplicationUserId}/refresh-directory-profile",
            new ApplicationUserConcurrencyRequest(original.RowVersion));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await refreshResponse.Content
            .ReadFromJsonAsync<ApplicationUserResponse>();
        Assert.NotNull(refreshed);
        Assert.Equal(
            fixture.DirectoryService.RefreshedMember.DisplayName,
            refreshed.DisplayName);
        Assert.Equal(fixture.DirectoryService.RefreshedMember.Email, refreshed.Email);
        Assert.Equal(
            fixture.DirectoryService.RefreshedMember.UserPrincipalName,
            refreshed.UserPrincipalName);

        using var staleResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/users/{fixture.ApplicationUserId}/refresh-directory-profile",
            new ApplicationUserConcurrencyRequest(original.RowVersion));
        await AssertProblemAsync(
            staleResponse,
            HttpStatusCode.Conflict,
            "administration.concurrency_conflict");
    }

    [Theory]
    [InlineData(
        AdministrationApiFixture.ApplicationUserAccount,
        "Directory Session Member")]
    [InlineData(AdministrationApiFixture.LockedUserAccount, "Locked Member")]
    [InlineData(AdministrationApiFixture.UnknownUserAccount, "unknown")]
    public async Task Current_session_uses_directory_display_name_then_safe_fallbacks(
        string accountName,
        string expectedDisplayName)
    {
        using var client = fixture.CreateClient(accountName);

        var session = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");

        Assert.NotNull(session);
        Assert.Equal(expectedDisplayName, session.DisplayName);
    }

    [Fact]
    public async Task Add_and_refresh_require_antiforgery_tokens()
    {
        using var client = CreateAdministratorClient();
        var selection = await SearchOneAsync(client, "directory.addable");

        using var addResponse = await client.PostAsJsonAsync(
            "/api/admin/users",
            new AddApplicationUserRequest(
                selection.SelectionToken,
                [fixture.ApplicationRoleId]));
        await AssertProblemAsync(
            addResponse,
            HttpStatusCode.BadRequest,
            "security.antiforgery_validation_failed");

        using var refreshResponse = await client.PostAsJsonAsync(
            $"/api/admin/users/{fixture.ApplicationUserId}/refresh-directory-profile",
            new ApplicationUserConcurrencyRequest(Convert.ToBase64String(new byte[8])));
        await AssertProblemAsync(
            refreshResponse,
            HttpStatusCode.BadRequest,
            "security.antiforgery_validation_failed");
    }

    private HttpClient CreateAdministratorClient() =>
        fixture.CreateClient(AdministrationApiFixture.BootstrapAdministratorAccount);

    private static async Task<DirectoryUserSearchItemResponse> SearchOneAsync(
        HttpClient client,
        string query)
    {
        var response = await client.GetFromJsonAsync<DirectoryUserSearchResponse>(
            $"/api/admin/directory/users?query={Uri.EscapeDataString(query)}" +
            "&page=1&pageSize=25");
        Assert.NotNull(response);
        return Assert.Single(response.Items);
    }

    private static async Task<HttpResponseMessage> SendWithAntiforgeryAsync<TBody>(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        TBody body)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/security/antiforgery-token");
        Assert.NotNull(token);

        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", token.RequestToken);
        return await client.SendAsync(request);
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        await using var content = await response.Content.ReadAsStreamAsync();
        using var problem = await JsonDocument.ParseAsync(content);
        Assert.Equal(
            expectedCode,
            problem.RootElement.GetProperty("code").GetString());
    }

    private sealed record PersistedDirectoryIdentity(
        byte[] Sid,
        Guid? ObjectGuid);
}
