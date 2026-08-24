using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IndependentApproval.Api.Contracts.Administration.RequestTypes;
using IndependentApproval.Api.Contracts.Security;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Infrastructure.Persistence;
using IndependentApproval.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IndependentApproval.Api.Tests;

public sealed class RequestTypeReservationTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Concurrent_creates_with_same_normalized_prefix_have_one_owner()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var sharedPrefix = $"P{suffix[..7]}".ToUpperInvariant();
        var first = CreatePayload(
            code: $"prefix_first_{suffix}",
            requestPrefix: $"  {sharedPrefix.ToLowerInvariant()}  ",
            navigationSlug: $"prefix-first-{suffix}");
        var second = CreatePayload(
            code: $"prefix_second_{suffix}",
            requestPrefix: sharedPrefix,
            navigationSlug: $"prefix-second-{suffix}");

        var result = await RunConcurrentCreateAsync(
            first,
            second,
            "administration.duplicate_request_prefix");

        await AssertSingleReservationOwnerAsync(result.Created, first, second);
    }

    [Fact]
    public async Task Concurrent_creates_with_same_normalized_slug_have_one_owner()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var sharedSlug = $"slug-race-{suffix}";
        var first = CreatePayload(
            code: $"slug_first_{suffix}",
            requestPrefix: $"A{suffix[..7]}",
            navigationSlug: $"  {sharedSlug.ToUpperInvariant()}  ");
        var second = CreatePayload(
            code: $"slug_second_{suffix}",
            requestPrefix: $"B{suffix[..7]}",
            navigationSlug: sharedSlug);

        var result = await RunConcurrentCreateAsync(
            first,
            second,
            "administration.duplicate_navigation_slug");

        await AssertSingleReservationOwnerAsync(result.Created, first, second);
    }

    private async Task<CreateRaceResult> RunConcurrentCreateAsync(
        CreateRequestTypeRequest first,
        CreateRequestTypeRequest second,
        string expectedConflictCode)
    {
        using var firstClient = CreateAdministratorClient();
        using var secondClient = CreateAdministratorClient();
        var tokenTasks = new[]
        {
            GetAntiforgeryTokenAsync(firstClient),
            GetAntiforgeryTokenAsync(secondClient)
        };
        var tokens = await Task.WhenAll(tokenTasks);
        using var firstRequest = CreateAntiforgeryRequest(first, tokens[0]);
        using var secondRequest = CreateAntiforgeryRequest(second, tokens[1]);
        var start = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var firstResponseTask = SendWhenReleasedAsync(
            firstClient,
            firstRequest,
            start.Task);
        var secondResponseTask = SendWhenReleasedAsync(
            secondClient,
            secondRequest,
            start.Task);

        start.SetResult();
        var responses = await Task.WhenAll(firstResponseTask, secondResponseTask);

        try
        {
            Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.Created);
            Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.Conflict);

            var createdResponse = responses.Single(
                response => response.StatusCode == HttpStatusCode.Created);
            var created = await createdResponse.Content
                .ReadFromJsonAsync<RequestTypeDetailResponse>();
            Assert.NotNull(created);

            var conflictResponse = responses.Single(
                response => response.StatusCode == HttpStatusCode.Conflict);
            var conflictBody = await conflictResponse.Content.ReadAsStringAsync();
            AssertSafeConflict(conflictBody, expectedConflictCode);

            return new CreateRaceResult(created);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    private async Task AssertSingleReservationOwnerAsync(
        RequestTypeDetailResponse created,
        CreateRequestTypeRequest first,
        CreateRequestTypeRequest second)
    {
        var candidateCodes = new[]
        {
            NormalizeCode(first.Code),
            NormalizeCode(second.Code)
        };
        var candidatePrefixes = new[]
        {
            NormalizePrefix(first.RequestPrefix),
            NormalizePrefix(second.RequestPrefix)
        };
        var candidateSlugs = new[]
        {
            NormalizeSlug(first.NavigationSlug),
            NormalizeSlug(second.NavigationSlug)
        };

        _ = await fixture.QueryDatabaseAsync(async dbContext =>
        {
            var roots = await dbContext.RequestTypes
                .AsNoTracking()
                .Where(requestType => candidateCodes.Contains(requestType.NormalizedCode))
                .Select(requestType => new
                {
                    requestType.Id,
                    requestType.NormalizedCode
                })
                .ToListAsync();
            var root = Assert.Single(roots);
            Assert.Equal(created.Id, root.Id);
            Assert.Equal(created.Code, root.NormalizedCode);

            var versions = await dbContext.RequestTypeVersions
                .AsNoTracking()
                .Where(version => version.RequestTypeId == root.Id)
                .Select(version => new
                {
                    version.RequestTypeId,
                    version.RequestPrefix,
                    version.NavigationSlug
                })
                .ToListAsync();
            var version = Assert.Single(versions);

            var prefixReservations = await dbContext.RequestTypePrefixReservations
                .AsNoTracking()
                .Where(reservation =>
                    candidatePrefixes.Contains(reservation.NormalizedPrefix))
                .Select(reservation => new
                {
                    reservation.NormalizedPrefix,
                    reservation.RequestTypeId
                })
                .ToListAsync();
            var prefixReservation = Assert.Single(prefixReservations);
            Assert.Equal(root.Id, prefixReservation.RequestTypeId);
            Assert.Equal(version.RequestTypeId, prefixReservation.RequestTypeId);
            Assert.Equal(version.RequestPrefix, prefixReservation.NormalizedPrefix);

            var slugReservations = await dbContext.RequestTypeSlugReservations
                .AsNoTracking()
                .Where(reservation =>
                    candidateSlugs.Contains(reservation.NormalizedSlug))
                .Select(reservation => new
                {
                    reservation.NormalizedSlug,
                    reservation.RequestTypeId
                })
                .ToListAsync();
            var slugReservation = Assert.Single(slugReservations);
            Assert.Equal(root.Id, slugReservation.RequestTypeId);
            Assert.Equal(version.RequestTypeId, slugReservation.RequestTypeId);
            Assert.Equal(version.NavigationSlug, slugReservation.NormalizedSlug);

            AssertReservationOwnershipModel(dbContext);
            return true;
        });
    }

    private static void AssertReservationOwnershipModel(
        IndependentApprovalDbContext dbContext)
    {
        AssertReservationOwnership<RequestTypePrefixReservation>(
            dbContext,
            nameof(RequestTypePrefixReservation.NormalizedPrefix),
            nameof(RequestTypeVersion.RequestPrefix));
        AssertReservationOwnership<RequestTypeSlugReservation>(
            dbContext,
            nameof(RequestTypeSlugReservation.NormalizedSlug),
            nameof(RequestTypeVersion.NavigationSlug));
    }

    private static void AssertReservationOwnership<TReservation>(
        IndependentApprovalDbContext dbContext,
        string normalizedValueProperty,
        string versionValueProperty)
    {
        var reservationEntity = dbContext.Model.FindEntityType(typeof(TReservation));
        Assert.NotNull(reservationEntity);
        Assert.Equal(
            [normalizedValueProperty],
            reservationEntity.FindPrimaryKey()!.Properties.Select(property => property.Name));

        var ownerForeignKey = Assert.Single(
            reservationEntity.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(RequestType));
        Assert.Equal(
            [nameof(RequestTypePrefixReservation.RequestTypeId)],
            ownerForeignKey.Properties.Select(property => property.Name));

        var versionEntity = dbContext.Model.FindEntityType(typeof(RequestTypeVersion));
        Assert.NotNull(versionEntity);
        var ownershipForeignKey = Assert.Single(
            versionEntity.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType == reservationEntity);
        Assert.Equal(
            [nameof(RequestTypeVersion.RequestTypeId), versionValueProperty],
            ownershipForeignKey.Properties.Select(property => property.Name));
        Assert.Equal(
            [nameof(RequestTypePrefixReservation.RequestTypeId), normalizedValueProperty],
            ownershipForeignKey.PrincipalKey.Properties.Select(property => property.Name));
    }

    private HttpClient CreateAdministratorClient() =>
        fixture.CreateClient(AdministrationApiFixture.BootstrapAdministratorAccount);

    private static CreateRequestTypeRequest CreatePayload(
        string code,
        string requestPrefix,
        string navigationSlug) =>
        new(
            code,
            $"Reservation race {code}",
            $"اختبار الحجز {code}",
            "Concurrent reservation regression test.",
            "اختبار تزامن حجز تعريف نوع الطلب.",
            requestPrefix,
            navigationSlug,
            100);

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/security/antiforgery-token");
        Assert.NotNull(token);
        return token.RequestToken;
    }

    private static HttpRequestMessage CreateAntiforgeryRequest(
        CreateRequestTypeRequest body,
        string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/request-types")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return request;
    }

    private static async Task<HttpResponseMessage> SendWhenReleasedAsync(
        HttpClient client,
        HttpRequestMessage request,
        Task start)
    {
        await start;
        return await client.SendAsync(request);
    }

    private static void AssertSafeConflict(
        string body,
        string expectedCode)
    {
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(
            expectedCode,
            problem.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            (int)HttpStatusCode.Conflict,
            problem.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("SqlException", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "IndependentApprovalTests_",
            body,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SPSE26H", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "RequestTypePrefixReservations",
            body,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "RequestTypeSlugReservations",
            body,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCode(string? code) =>
        Assert.IsType<string>(code).Trim().ToUpperInvariant();

    private static string NormalizePrefix(string? prefix) =>
        Assert.IsType<string>(prefix).Trim().ToUpperInvariant();

    private static string NormalizeSlug(string? slug) =>
        Assert.IsType<string>(slug).Trim().ToLowerInvariant();

    private sealed record CreateRaceResult(RequestTypeDetailResponse Created);
}
