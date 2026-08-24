using IndependentApproval.Api.Application.Requests;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Infrastructure.Persistence;
using IndependentApproval.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IndependentApproval.Api.Tests;

public sealed class RequestNumberGeneratorRollbackTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Rolled_back_request_number_is_reused_by_the_next_transaction()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var prefix = $"RB{suffix}";
        var requestTypeVersionId = await SeedPublishedRequestTypeAsync(
            prefix,
            $"rollback-{suffix.ToLowerInvariant()}");
        var timestamp = new DateTimeOffset(
            2026,
            8,
            24,
            12,
            0,
            0,
            TimeSpan.Zero);

        var rolledBackNumber = await fixture.ExecuteScopedAsync(
            async serviceProvider =>
            {
                var dbContext = serviceProvider
                    .GetRequiredService<IndependentApprovalDbContext>();
                await using var transaction = await dbContext.Database
                    .BeginTransactionAsync(CancellationToken.None);
                var generator = serviceProvider
                    .GetRequiredService<IRequestNumberGenerator>();

                var requestNumber = await generator.GenerateAsync(
                    requestTypeVersionId,
                    timestamp,
                    CancellationToken.None);

                await transaction.RollbackAsync(CancellationToken.None);
                return requestNumber;
            });

        var reusedNumber = await fixture.ExecuteScopedAsync(
            async serviceProvider =>
            {
                var dbContext = serviceProvider
                    .GetRequiredService<IndependentApprovalDbContext>();
                await using var transaction = await dbContext.Database
                    .BeginTransactionAsync(CancellationToken.None);
                var generator = serviceProvider
                    .GetRequiredService<IRequestNumberGenerator>();

                var requestNumber = await generator.GenerateAsync(
                    requestTypeVersionId,
                    timestamp,
                    CancellationToken.None);

                await transaction.CommitAsync(CancellationToken.None);
                return requestNumber;
            });

        Assert.Equal($"{prefix}-2026-000001", rolledBackNumber);
        Assert.Equal(rolledBackNumber, reusedNumber);

        var nextValue = await fixture.QueryDatabaseAsync(async dbContext =>
            await dbContext.RequestNumberSequences
                .Where(sequence => sequence.NormalizedPrefix == prefix
                                   && sequence.Year == 2026)
                .Select(sequence => sequence.NextValue)
                .SingleAsync());
        Assert.Equal(2, nextValue);
    }

    private async Task<Guid> SeedPublishedRequestTypeAsync(
        string prefix,
        string slug)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var requestTypeId = Guid.NewGuid();
        var requestTypeVersionId = Guid.NewGuid();
        var code = $"ROLLBACK_{prefix}";

        return await fixture.QueryDatabaseAsync(async dbContext =>
        {
            dbContext.RequestTypes.Add(new RequestType
            {
                Id = requestTypeId,
                Code = code,
                NormalizedCode = code,
                CreatedAtUtc = timestamp,
                CreatedByAccount =
                    AdministrationApiFixture.BootstrapAdministratorAccount,
                CreatedByUserId = fixture.BootstrapAdministratorId
            });
            dbContext.RequestTypePrefixReservations.Add(
                new RequestTypePrefixReservation
                {
                    NormalizedPrefix = prefix,
                    RequestTypeId = requestTypeId,
                    ReservedAtUtc = timestamp,
                    ReservedByAccount =
                        AdministrationApiFixture.BootstrapAdministratorAccount,
                    ReservedByUserId = fixture.BootstrapAdministratorId
                });
            dbContext.RequestTypeSlugReservations.Add(
                new RequestTypeSlugReservation
                {
                    NormalizedSlug = slug,
                    RequestTypeId = requestTypeId,
                    ReservedAtUtc = timestamp,
                    ReservedByAccount =
                        AdministrationApiFixture.BootstrapAdministratorAccount,
                    ReservedByUserId = fixture.BootstrapAdministratorId
                });
            dbContext.RequestTypeVersions.Add(new RequestTypeVersion
            {
                Id = requestTypeVersionId,
                RequestTypeId = requestTypeId,
                VersionNumber = 1,
                NameEnglish = "Request-number rollback test",
                NameArabic = "اختبار التراجع عن رقم الطلب",
                DescriptionEnglish =
                    "Published request type for the rollback regression test.",
                DescriptionArabic =
                    "نوع طلب منشور لاختبار التراجع عن حجز الرقم.",
                RequestPrefix = prefix,
                NavigationSlug = slug,
                NavigationOrder = 1,
                Lifecycle = RequestTypeVersionLifecycle.Published,
                CreatedAtUtc = timestamp,
                CreatedByAccount =
                    AdministrationApiFixture.BootstrapAdministratorAccount,
                CreatedByUserId = fixture.BootstrapAdministratorId,
                PublishedAtUtc = timestamp,
                PublishedByAccount =
                    AdministrationApiFixture.BootstrapAdministratorAccount,
                PublishedByUserId = fixture.BootstrapAdministratorId
            });

            await dbContext.SaveChangesAsync();
            return requestTypeVersionId;
        });
    }
}
