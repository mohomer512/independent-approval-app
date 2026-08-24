using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IndependentApproval.Api.Contracts.Administration;
using IndependentApproval.Api.Contracts.Administration.Roles;
using IndependentApproval.Api.Contracts.Administration.Users;
using IndependentApproval.Api.Contracts.Security;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Tests.Infrastructure;

namespace IndependentApproval.Api.Tests;

public sealed class AdministrationFoundationTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Bootstrap_administrator_can_access_administration_summary()
    {
        using var client = fixture.CreateClient(
            AdministrationApiFixture.BootstrapAdministratorAccount);

        using var response = await client.GetAsync("/api/admin/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content
            .ReadFromJsonAsync<AdministrationSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(2, summary.ActiveUserCount);
        Assert.True(summary.ActiveRoleCount >= 1);
    }

    [Fact]
    public async Task Application_user_cannot_access_administration_summary()
    {
        using var client = fixture.CreateClient(
            AdministrationApiFixture.ApplicationUserAccount);

        using var response = await client.GetAsync("/api/admin/summary");

        await AssertProblemAsync(
            response,
            HttpStatusCode.Forbidden,
            "authorization.forbidden");
    }

    [Theory]
    [InlineData(AdministrationApiFixture.UnknownUserAccount)]
    [InlineData(AdministrationApiFixture.LockedUserAccount)]
    [InlineData(AdministrationApiFixture.RemovedUserAccount)]
    public async Task Users_without_current_application_access_cannot_list_documents(
        string accountName)
    {
        using var client = fixture.CreateClient(accountName);

        using var response = await client.GetAsync("/api/documents");

        await AssertProblemAsync(
            response,
            HttpStatusCode.Forbidden,
            "authorization.forbidden");
    }

    [Theory]
    [InlineData("roles")]
    [InlineData("lock")]
    [InlineData("remove")]
    public async Task Represented_bootstrap_administrator_is_protected_from_changes(
        string mutation)
    {
        using var client = fixture.CreateClient(
            AdministrationApiFixture.BootstrapAdministratorAccount);
        var administrator = await client.GetFromJsonAsync<ApplicationUserResponse>(
            $"/api/admin/users/{fixture.BootstrapAdministratorId}");
        Assert.NotNull(administrator);

        using var response = mutation switch
        {
            "roles" => await SendWithAntiforgeryAsync(
                client,
                HttpMethod.Put,
                $"/api/admin/users/{fixture.BootstrapAdministratorId}/roles",
                new UpdateApplicationUserRolesRequest([], administrator.RowVersion)),
            "lock" => await SendWithAntiforgeryAsync(
                client,
                HttpMethod.Post,
                $"/api/admin/users/{fixture.BootstrapAdministratorId}/lock",
                new LockApplicationUserRequest(
                    "A protected administrator cannot be locked.",
                    administrator.RowVersion)),
            "remove" => await SendWithAntiforgeryAsync(
                client,
                HttpMethod.Post,
                $"/api/admin/users/{fixture.BootstrapAdministratorId}/remove",
                new ApplicationUserConcurrencyRequest(administrator.RowVersion)),
            _ => throw new InvalidOperationException($"Unknown mutation '{mutation}'.")
        };

        await AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            "administration.protected_bootstrap_administrator");
    }

    [Fact]
    public async Task Duplicate_normalized_role_code_returns_conflict()
    {
        using var client = fixture.CreateClient(
            AdministrationApiFixture.BootstrapAdministratorAccount);
        var code = $"DUP_{Guid.NewGuid():N}";
        var firstRequest = CreateRoleRequest(code, "Duplicate role");

        using var firstResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/admin/roles",
            firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var duplicateResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/admin/roles",
            CreateRoleRequest(code.ToLowerInvariant(), "Duplicate role again"));

        await AssertProblemAsync(
            duplicateResponse,
            HttpStatusCode.Conflict,
            "administration.duplicate_role_code");
    }

    [Fact]
    public async Task Stale_role_row_version_returns_concurrency_conflict()
    {
        using var client = fixture.CreateClient(
            AdministrationApiFixture.BootstrapAdministratorAccount);
        var code = $"CONCURRENCY_{Guid.NewGuid():N}";

        using var createResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/admin/roles",
            CreateRoleRequest(code, "Original role"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApplicationRoleResponse>();
        Assert.NotNull(created);

        var firstUpdate = new UpdateApplicationRoleRequest(
            "Updated once",
            "تم التحديث مرة واحدة",
            null,
            null,
            true,
            [PermissionCodes.AccessApplication],
            created.RowVersion);
        using var successfulUpdateResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/roles/{created.Id}",
            firstUpdate);
        Assert.Equal(HttpStatusCode.OK, successfulUpdateResponse.StatusCode);

        var staleUpdate = firstUpdate with
        {
            NameEnglish = "Stale update",
            NameArabic = "تحديث قديم"
        };
        using var staleUpdateResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/roles/{created.Id}",
            staleUpdate);

        await AssertProblemAsync(
            staleUpdateResponse,
            HttpStatusCode.Conflict,
            "administration.concurrency_conflict");
    }

    [Fact]
    public async Task Unsafe_administration_mutations_require_antiforgery_token()
    {
        using var client = fixture.CreateClient(
            AdministrationApiFixture.BootstrapAdministratorAccount);

        using var postResponse = await client.PostAsJsonAsync(
            "/api/admin/roles",
            CreateRoleRequest($"NO_CSRF_{Guid.NewGuid():N}", "No CSRF role"));
        await AssertProblemAsync(
            postResponse,
            HttpStatusCode.BadRequest,
            "security.antiforgery_validation_failed");

        using var putResponse = await client.PutAsJsonAsync(
            $"/api/admin/roles/{Guid.NewGuid()}",
            new UpdateApplicationRoleRequest(
                "No CSRF update",
                "تحديث بدون رمز",
                null,
                null,
                true,
                [PermissionCodes.AccessApplication],
                Convert.ToBase64String(new byte[8])));
        await AssertProblemAsync(
            putResponse,
            HttpStatusCode.BadRequest,
            "security.antiforgery_validation_failed");
    }

    private static CreateApplicationRoleRequest CreateRoleRequest(
        string code,
        string englishName) =>
        new(
            code,
            englishName,
            "دور اختبار",
            "Created by an integration test.",
            "تم إنشاؤه بواسطة اختبار تكامل.",
            [PermissionCodes.AccessApplication]);

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
}
