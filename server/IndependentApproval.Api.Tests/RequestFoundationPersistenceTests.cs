using System.Text.RegularExpressions;
using IndependentApproval.Api.Application.Requests;
using IndependentApproval.Api.Domain.Documents;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace IndependentApproval.Api.Tests;

public sealed class RequestFoundationPersistenceTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Approval_request_model_has_stable_immutable_system_schema()
    {
        await fixture.QueryDatabaseAsync(dbContext =>
        {
            var request = dbContext.Model.FindEntityType(typeof(ApprovalRequest));
            Assert.NotNull(request);
            var requiredProperties = new[]
            {
                nameof(ApprovalRequest.Id),
                nameof(ApprovalRequest.RequestNumber),
                nameof(ApprovalRequest.RequestTypeId),
                nameof(ApprovalRequest.RequestTypeVersionId),
                nameof(ApprovalRequest.WorkflowDefinitionId),
                nameof(ApprovalRequest.WorkflowVersionId),
                nameof(ApprovalRequest.CurrentWorkflowStepId),
                nameof(ApprovalRequest.Title),
                nameof(ApprovalRequest.Status),
                nameof(ApprovalRequest.RequestedByUserId),
                nameof(ApprovalRequest.CreatedAtUtc),
                nameof(ApprovalRequest.ModifiedAtUtc),
                nameof(ApprovalRequest.SubmittedAtUtc),
                nameof(ApprovalRequest.CompletedAtUtc),
                nameof(ApprovalRequest.RowVersion)
            };

            Assert.All(
                requiredProperties,
                propertyName => Assert.NotNull(request.FindProperty(propertyName)));
            Assert.True(
                request.FindProperty(nameof(ApprovalRequest.RowVersion))!
                    .IsConcurrencyToken);
            Assert.Contains(
                request.GetIndexes(),
                index => index.IsUnique
                         && index.Properties.Select(property => property.Name)
                             .SequenceEqual([nameof(ApprovalRequest.RequestNumber)]));

            foreach (var immutableProperty in new[]
                     {
                         nameof(ApprovalRequest.RequestNumber),
                         nameof(ApprovalRequest.RequestTypeId),
                         nameof(ApprovalRequest.RequestTypeVersionId),
                         nameof(ApprovalRequest.WorkflowDefinitionId),
                         nameof(ApprovalRequest.WorkflowVersionId),
                         nameof(ApprovalRequest.RequestedByUserId)
                     })
            {
                Assert.Equal(
                    PropertySaveBehavior.Throw,
                    request.FindProperty(immutableProperty)!.GetAfterSaveBehavior());
            }

            Assert.Contains(
                request.GetKeys(),
                key => key.Properties.Select(property => property.Name).SequenceEqual(
                    [nameof(ApprovalRequest.Id), nameof(ApprovalRequest.RequestTypeVersionId)]));
            AssertChildUsesVersionBoundCompositeForeignKeys<RequestContentValue>(
                dbContext.Model);
            AssertChildUsesVersionBoundCompositeForeignKeys<RequestDocument>(
                dbContext.Model);
            AssertChildUsesVersionBoundCompositeForeignKeys<RequestRichDocumentRevision>(
                dbContext.Model);

            return Task.FromResult(true);
        });
    }

    [Fact]
    public async Task Request_content_documents_and_rich_revisions_reject_fields_from_another_version()
    {
        var seeded = await SeedTwoVersionRequestAsync();

        await AssertCrossVersionAttachmentRejectedAsync(
            new RequestContentValue
            {
                Id = Guid.NewGuid(),
                ApprovalRequestId = seeded.ApprovalRequestId,
                RequestTypeVersionId = seeded.RequestTypeVersionId,
                RequestFieldDefinitionId = seeded.ForeignFieldId,
                ValueJson = "\"cross-version\"",
                CreatedAtUtc = seeded.Timestamp,
                CreatedByApplicationUserId = fixture.BootstrapAdministratorId
            });
        await AssertCrossVersionAttachmentRejectedAsync(
            new RequestDocument
            {
                Id = Guid.NewGuid(),
                ApprovalRequestId = seeded.ApprovalRequestId,
                RequestTypeVersionId = seeded.RequestTypeVersionId,
                RequestFieldDefinitionId = seeded.ForeignFieldId,
                DocumentId = seeded.DocumentId,
                SortOrder = 99,
                AttachedAtUtc = seeded.Timestamp,
                AttachedByApplicationUserId = fixture.BootstrapAdministratorId
            });
        await AssertCrossVersionAttachmentRejectedAsync(
            new RequestRichDocumentRevision
            {
                Id = Guid.NewGuid(),
                ApprovalRequestId = seeded.ApprovalRequestId,
                RequestTypeVersionId = seeded.RequestTypeVersionId,
                RequestFieldDefinitionId = seeded.ForeignFieldId,
                RevisionNumber = 1,
                ContentHtml = "<p>Cross-version content</p>",
                ContentSha256 = new string('B', 64),
                ContentLength = 28,
                SanitizerVersion = "checkpoint-3-test",
                CreatedAtUtc = seeded.Timestamp,
                CreatedByApplicationUserId = fixture.BootstrapAdministratorId
            });

        var persistedCounts = await fixture.QueryDatabaseAsync(async dbContext =>
            new PersistedChildCounts(
                await dbContext.RequestContentValues.CountAsync(value =>
                    value.ApprovalRequestId == seeded.ApprovalRequestId),
                await dbContext.RequestDocuments.CountAsync(document =>
                    document.ApprovalRequestId == seeded.ApprovalRequestId),
                await dbContext.RequestRichDocumentRevisions.CountAsync(revision =>
                    revision.ApprovalRequestId == seeded.ApprovalRequestId)));
        Assert.Equal(new PersistedChildCounts(1, 1, 1), persistedCounts);
    }

    [Fact]
    public async Task Concurrent_request_number_generation_is_unique_transactional_and_formatted()
    {
        var suffix = Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
        var prefix = $"N{suffix}";
        var versionId = await SeedPublishedRequestTypeAsync(prefix);
        var timestamp = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);
        const int generationCount = 16;

        var generationTasks = Enumerable.Range(0, generationCount)
            .Select(_ => fixture.ExecuteScopedAsync(async serviceProvider =>
            {
                var generator = serviceProvider
                    .GetRequiredService<IRequestNumberGenerator>();
                return await generator.GenerateAsync(
                    versionId,
                    timestamp,
                    CancellationToken.None);
            }));
        var requestNumbers = await Task.WhenAll(generationTasks);

        Assert.Equal(generationCount, requestNumbers.Distinct().Count());
        Assert.All(
            requestNumbers,
            number => Assert.Matches(
                new Regex(
                    $"^{Regex.Escape(prefix)}-2026-[0-9]{{6}}$",
                    RegexOptions.CultureInvariant),
                number));
        Assert.Equal(
            Enumerable.Range(1, generationCount),
            requestNumbers
                .Select(number => int.Parse(number[^6..],
                    System.Globalization.CultureInfo.InvariantCulture))
                .OrderBy(value => value));

        var nextValue = await fixture.QueryDatabaseAsync(async dbContext =>
            await dbContext.RequestNumberSequences
                .Where(sequence => sequence.NormalizedPrefix == prefix
                                   && sequence.Year == 2026)
                .Select(sequence => sequence.NextValue)
                .SingleAsync());
        Assert.Equal(generationCount + 1, nextValue);
    }

    private static void AssertChildUsesVersionBoundCompositeForeignKeys<TChild>(
        IModel model)
    {
        var entity = model.FindEntityType(typeof(TChild));
        Assert.NotNull(entity);
        var foreignKeyPropertySets = entity.GetForeignKeys()
            .Select(foreignKey => foreignKey.Properties
                .Select(property => property.Name)
                .ToHashSet(StringComparer.Ordinal))
            .ToArray();
        Assert.Contains(
            foreignKeyPropertySets,
            properties => properties.SetEquals(
                ["ApprovalRequestId", "RequestTypeVersionId"]));
        Assert.Contains(
            foreignKeyPropertySets,
            properties => properties.SetEquals(
                ["RequestFieldDefinitionId", "RequestTypeVersionId"]));
    }

    private async Task AssertCrossVersionAttachmentRejectedAsync<TEntity>(
        TEntity entity)
        where TEntity : class
    {
        await fixture.QueryDatabaseAsync(async dbContext =>
        {
            dbContext.Add(entity);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                dbContext.SaveChangesAsync());
            Assert.NotNull(exception.InnerException);
            return true;
        });
    }

    private async Task<CrossVersionSeed> SeedTwoVersionRequestAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var firstRootId = Guid.NewGuid();
        var firstVersionId = Guid.NewGuid();
        var secondRootId = Guid.NewGuid();
        var secondVersionId = Guid.NewGuid();
        var contentFieldId = Guid.NewGuid();
        var documentFieldId = Guid.NewGuid();
        var richFieldId = Guid.NewGuid();
        var foreignFieldId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var firstSuffix = Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
        var secondSuffix = Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();

        return await fixture.QueryDatabaseAsync(async dbContext =>
        {
            dbContext.RequestTypes.AddRange(
                RequestTypeRoot(firstRootId, $"ROOT_{firstSuffix}", now),
                RequestTypeRoot(secondRootId, $"ROOT_{secondSuffix}", now));
            dbContext.RequestTypePrefixReservations.AddRange(
                PrefixReservation(firstRootId, $"A{firstSuffix}", now),
                PrefixReservation(secondRootId, $"B{secondSuffix}", now));
            dbContext.RequestTypeSlugReservations.AddRange(
                SlugReservation(firstRootId, $"first-{firstSuffix.ToLowerInvariant()}", now),
                SlugReservation(secondRootId, $"second-{secondSuffix.ToLowerInvariant()}", now));
            dbContext.RequestTypeVersions.AddRange(
                PublishedVersion(
                    firstVersionId,
                    firstRootId,
                    $"A{firstSuffix}",
                    $"first-{firstSuffix.ToLowerInvariant()}",
                    now),
                PublishedVersion(
                    secondVersionId,
                    secondRootId,
                    $"B{secondSuffix}",
                    $"second-{secondSuffix.ToLowerInvariant()}",
                    now));
            dbContext.RequestFieldDefinitions.AddRange(
                FieldDefinition(
                    contentFieldId,
                    firstVersionId,
                    "content_value",
                    RequestFieldType.ShortText,
                    1,
                    null,
                    now),
                FieldDefinition(
                    documentFieldId,
                    firstVersionId,
                    "file_document",
                    RequestFieldType.FileDocument,
                    2,
                    DocumentFieldMode.UploadOnly,
                    now),
                FieldDefinition(
                    richFieldId,
                    firstVersionId,
                    "rich_document",
                    RequestFieldType.RichDocument,
                    3,
                    DocumentFieldMode.CreateInEditorOnly,
                    now),
                FieldDefinition(
                    foreignFieldId,
                    secondVersionId,
                    "foreign_field",
                    RequestFieldType.ShortText,
                    1,
                    null,
                    now));
            dbContext.Documents.Add(new DocumentRecord
            {
                Id = documentId,
                OriginalFileName = "foundation.pdf",
                StorageKey = $"foundation/{Guid.NewGuid():N}.pdf",
                ContentType = "application/pdf",
                FileSize = 128,
                Status = "Available",
                UploadedBy = AdministrationApiFixture.BootstrapAdministratorAccount,
                UploadedAtUtc = now
            });
            dbContext.ApprovalRequests.Add(new ApprovalRequest
            {
                Id = requestId,
                RequestNumber = $"A{firstSuffix}-2026-000001",
                RequestTypeId = firstRootId,
                RequestTypeVersionId = firstVersionId,
                WorkflowDefinitionId = Guid.NewGuid(),
                WorkflowVersionId = Guid.NewGuid(),
                Title = "Stable foundation request",
                Status = ApprovalRequestStatus.Draft,
                RequestedByUserId = fixture.BootstrapAdministratorId,
                CreatedAtUtc = now
            });
            dbContext.RequestContentValues.Add(new RequestContentValue
            {
                Id = Guid.NewGuid(),
                ApprovalRequestId = requestId,
                RequestTypeVersionId = firstVersionId,
                RequestFieldDefinitionId = contentFieldId,
                ValueJson = "\"valid\"",
                CreatedAtUtc = now,
                CreatedByApplicationUserId = fixture.BootstrapAdministratorId
            });
            dbContext.RequestDocuments.Add(new RequestDocument
            {
                Id = Guid.NewGuid(),
                ApprovalRequestId = requestId,
                RequestTypeVersionId = firstVersionId,
                RequestFieldDefinitionId = documentFieldId,
                DocumentId = documentId,
                SortOrder = 1,
                AttachedAtUtc = now,
                AttachedByApplicationUserId = fixture.BootstrapAdministratorId
            });
            dbContext.RequestRichDocumentRevisions.Add(new RequestRichDocumentRevision
            {
                Id = Guid.NewGuid(),
                ApprovalRequestId = requestId,
                RequestTypeVersionId = firstVersionId,
                RequestFieldDefinitionId = richFieldId,
                RevisionNumber = 1,
                ContentHtml = "<p>Sanitized content</p>",
                ContentSha256 = new string('A', 64),
                ContentLength = 24,
                SanitizerVersion = "checkpoint-3-test",
                CreatedAtUtc = now,
                CreatedByApplicationUserId = fixture.BootstrapAdministratorId
            });
            await dbContext.SaveChangesAsync();

            return new CrossVersionSeed(
                requestId,
                firstVersionId,
                foreignFieldId,
                documentId,
                now);
        });
    }

    private async Task<Guid> SeedPublishedRequestTypeAsync(string prefix)
    {
        var now = DateTimeOffset.UtcNow;
        var rootId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        return await fixture.QueryDatabaseAsync(async dbContext =>
        {
            dbContext.RequestTypes.Add(RequestTypeRoot(rootId, $"NUMBER_{suffix}", now));
            dbContext.RequestTypePrefixReservations.Add(
                PrefixReservation(rootId, prefix, now));
            dbContext.RequestTypeSlugReservations.Add(
                SlugReservation(rootId, $"number-{suffix.ToLowerInvariant()}", now));
            dbContext.RequestTypeVersions.Add(PublishedVersion(
                versionId,
                rootId,
                prefix,
                $"number-{suffix.ToLowerInvariant()}",
                now));
            await dbContext.SaveChangesAsync();
            return versionId;
        });
    }

    private RequestType RequestTypeRoot(
        Guid id,
        string code,
        DateTimeOffset timestamp) =>
        new()
        {
            Id = id,
            Code = code,
            NormalizedCode = code.ToUpperInvariant(),
            CreatedAtUtc = timestamp,
            CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
            CreatedByUserId = fixture.BootstrapAdministratorId
        };

    private RequestTypePrefixReservation PrefixReservation(
        Guid requestTypeId,
        string normalizedPrefix,
        DateTimeOffset timestamp) =>
        new()
        {
            NormalizedPrefix = normalizedPrefix,
            RequestTypeId = requestTypeId,
            ReservedAtUtc = timestamp,
            ReservedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
            ReservedByUserId = fixture.BootstrapAdministratorId
        };

    private RequestTypeSlugReservation SlugReservation(
        Guid requestTypeId,
        string normalizedSlug,
        DateTimeOffset timestamp) =>
        new()
        {
            NormalizedSlug = normalizedSlug,
            RequestTypeId = requestTypeId,
            ReservedAtUtc = timestamp,
            ReservedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
            ReservedByUserId = fixture.BootstrapAdministratorId
        };

    private RequestTypeVersion PublishedVersion(
        Guid id,
        Guid requestTypeId,
        string prefix,
        string slug,
        DateTimeOffset timestamp) =>
        new()
        {
            Id = id,
            RequestTypeId = requestTypeId,
            VersionNumber = 1,
            NameEnglish = $"Published {prefix}",
            NameArabic = $"منشور {prefix}",
            DescriptionEnglish = "Published request type for a persistence test.",
            DescriptionArabic = "نوع طلب منشور لاختبار التخزين.",
            RequestPrefix = prefix,
            NavigationSlug = slug,
            NavigationOrder = 1,
            Lifecycle = RequestTypeVersionLifecycle.Published,
            CreatedAtUtc = timestamp,
            CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
            CreatedByUserId = fixture.BootstrapAdministratorId,
            PublishedAtUtc = timestamp,
            PublishedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
            PublishedByUserId = fixture.BootstrapAdministratorId
        };

    private RequestFieldDefinition FieldDefinition(
        Guid id,
        Guid versionId,
        string key,
        RequestFieldType fieldType,
        int sortOrder,
        DocumentFieldMode? documentMode,
        DateTimeOffset timestamp) =>
        new()
        {
            Id = id,
            RequestTypeVersionId = versionId,
            Key = key,
            NormalizedKey = key.ToUpperInvariant(),
            LabelEnglish = key,
            LabelArabic = $"حقل {key}",
            FieldType = fieldType,
            IsRequired = true,
            SortOrder = sortOrder,
            IsActive = true,
            DocumentMode = documentMode,
            CreatedAtUtc = timestamp,
            CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
            CreatedByUserId = fixture.BootstrapAdministratorId
        };

    private sealed record CrossVersionSeed(
        Guid ApprovalRequestId,
        Guid RequestTypeVersionId,
        Guid ForeignFieldId,
        Guid DocumentId,
        DateTimeOffset Timestamp);

    private sealed record PersistedChildCounts(
        int ContentValues,
        int Documents,
        int RichDocumentRevisions);
}
