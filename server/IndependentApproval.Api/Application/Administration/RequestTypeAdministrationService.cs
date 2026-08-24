using System.Text.Json;
using IndependentApproval.Api.Contracts.Administration.RequestTypes;
using IndependentApproval.Api.Contracts.Common;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IndependentApproval.Api.Application.Administration;

public sealed class RequestTypeAdministrationService(
    IndependentApprovalDbContext dbContext,
    IAdministrationActorAccessor actorAccessor) : IRequestTypeAdministrationService
{
    private const int MaximumSearchLength = 200;

    private static readonly IReadOnlyList<SystemRequestFieldResponse> SystemFields =
    [
        new(RequestSystemFieldKeys.RequestNumber, "Request number", "رقم الطلب", "shortText", true, false, false),
        new(RequestSystemFieldKeys.Title, "Title / subject", "العنوان / الموضوع", "shortText", true, true, false),
        new(RequestSystemFieldKeys.Status, "Status", "الحالة", "shortText", true, false, false),
        new(RequestSystemFieldKeys.RequestedBy, "Requested by", "مقدم الطلب", "applicationUser", true, false, false),
        new(RequestSystemFieldKeys.CreatedAtUtc, "Created at", "تاريخ الإنشاء", "dateTime", false, false, false),
        new(RequestSystemFieldKeys.ModifiedAtUtc, "Modified at", "تاريخ التعديل", "dateTime", false, false, false),
        new(RequestSystemFieldKeys.SubmittedAtUtc, "Submitted at", "تاريخ التقديم", "dateTime", false, false, false),
        new(RequestSystemFieldKeys.CompletedAtUtc, "Completed at", "تاريخ الإكمال", "dateTime", false, false, false)
    ];

    public async Task<PagedResponse<RequestTypeListItemResponse>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        AdministrationEncoding.ValidatePagination(page, pageSize);
        var normalizedSearch = NormalizeSearch(search);
        var query = dbContext.RequestTypes
            .AsNoTracking()
            .Where(requestType => includeArchived || !requestType.IsArchived);

        if (normalizedSearch is not null)
        {
            query = query.Where(requestType =>
                requestType.Code.Contains(normalizedSearch)
                || requestType.NormalizedCode.Contains(normalizedSearch)
                || requestType.Versions.Any(version =>
                    version.NameEnglish.Contains(normalizedSearch)
                    || version.NameArabic.Contains(normalizedSearch)
                    || version.RequestPrefix.Contains(normalizedSearch)
                    || version.NavigationSlug.Contains(normalizedSearch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (long)(page - 1) * pageSize;

        if (skip >= totalCount)
        {
            return new PagedResponse<RequestTypeListItemResponse>([], page, pageSize, totalCount);
        }

        var roots = await query
            .OrderBy(requestType => requestType.Code)
            .ThenBy(requestType => requestType.Id)
            .Skip((int)skip)
            .Take(pageSize)
            .Select(requestType => new RequestTypeProjection(
                requestType.Id,
                requestType.Code,
                requestType.IsArchived,
                requestType.CreatedAtUtc,
                requestType.CreatedByAccount,
                requestType.ModifiedAtUtc,
                requestType.ModifiedByAccount,
                requestType.ArchivedAtUtc,
                requestType.ArchivedByAccount,
                requestType.RowVersion))
            .ToListAsync(cancellationToken);
        var versionDetails = await GetVersionDetailsAsync(
            roots.Select(root => root.Id).ToArray(),
            includeFields: false,
            cancellationToken);
        var items = roots.Select(root =>
        {
            var versions = versionDetails.GetValueOrDefault(root.Id) ?? [];
            var displayVersion = versions
                .OrderBy(version => DisplayLifecycleOrder(version.Lifecycle))
                .ThenByDescending(version => version.VersionNumber)
                .FirstOrDefault();

            return new RequestTypeListItemResponse(
                root.Id,
                root.Code,
                root.IsArchived,
                displayVersion is null ? null : CreateSummaryResponse(displayVersion),
                versions.FirstOrDefault(version =>
                    version.Lifecycle == RequestTypeVersionLifecycle.Draft)?.Id,
                versions
                    .Where(version => version.Lifecycle == RequestTypeVersionLifecycle.Published)
                    .OrderByDescending(version => version.VersionNumber)
                    .Select(version => (Guid?)version.Id)
                    .FirstOrDefault(),
                versions.Count,
                root.CreatedAtUtc,
                root.CreatedByAccount,
                root.ModifiedAtUtc,
                root.ModifiedByAccount,
                root.ArchivedAtUtc,
                root.ArchivedByAccount,
                AdministrationEncoding.EncodeRowVersion(root.RowVersion));
        }).ToArray();

        return new PagedResponse<RequestTypeListItemResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<RequestTypeDetailResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var root = await dbContext.RequestTypes
            .AsNoTracking()
            .Where(requestType => requestType.Id == id)
            .Select(requestType => new RequestTypeProjection(
                requestType.Id,
                requestType.Code,
                requestType.IsArchived,
                requestType.CreatedAtUtc,
                requestType.CreatedByAccount,
                requestType.ModifiedAtUtc,
                requestType.ModifiedByAccount,
                requestType.ArchivedAtUtc,
                requestType.ArchivedByAccount,
                requestType.RowVersion))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The request type does not exist.");
        var versions = await GetVersionDetailsAsync(
            [id],
            includeFields: true,
            cancellationToken);

        return CreateDetailResponse(root, versions.GetValueOrDefault(id) ?? []);
    }

    public async Task<RequestTypeVersionResponse> GetVersionAsync(
        Guid requestTypeId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var versions = await GetVersionDetailsAsync(
            [requestTypeId],
            includeFields: true,
            cancellationToken);
        var version = (versions.GetValueOrDefault(requestTypeId) ?? [])
            .SingleOrDefault(version => version.Id == versionId)
            ?? throw new AdministrationNotFoundException(
                "The request-type version does not exist.");
        return CreateVersionResponse(version);
    }

    public IReadOnlyList<SystemRequestFieldResponse> ListSystemFields() => SystemFields;

    public async Task<RequestTypeDetailResponse> CreateAsync(
        CreateRequestTypeRequest request,
        CancellationToken cancellationToken)
    {
        var values = RequestTypeDefinitionValidator.ValidateCreate(request);
        await EnsureUniqueCodeAsync(values.Code, cancellationToken);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var requestType = new RequestType
        {
            Id = Guid.NewGuid(),
            Code = values.Code,
            NormalizedCode = values.Code,
            IsArchived = false,
            CreatedAtUtc = now,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        dbContext.RequestTypes.Add(requestType);
        await ReserveNavigationValuesAsync(
            requestType.Id,
            values.Version.RequestPrefix,
            values.Version.NavigationSlug,
            actor,
            now,
            cancellationToken);
        var version = CreateVersion(
            requestType.Id,
            versionNumber: 1,
            values.Version,
            actor,
            now);
        dbContext.RequestTypeVersions.Add(version);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetAsync(requestType.Id, cancellationToken);
    }

    public async Task<RequestTypeVersionResponse> UpdateVersionAsync(
        Guid requestTypeId,
        Guid versionId,
        UpdateRequestTypeVersionRequest request,
        CancellationToken cancellationToken)
    {
        var values = RequestTypeDefinitionValidator.ValidateVersion(request);
        var mutable = await GetMutableDraftAsync(
            requestTypeId,
            versionId,
            request.RowVersion,
            cancellationToken);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await ReserveNavigationValuesAsync(
            requestTypeId,
            values.RequestPrefix,
            values.NavigationSlug,
            actor,
            now,
            cancellationToken);
        ApplyVersionValues(mutable.Version, values);
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.RequestType, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(requestTypeId, versionId, cancellationToken);
    }

    public async Task<RequestTypeVersionResponse> AddFieldAsync(
        Guid requestTypeId,
        Guid versionId,
        CreateRequestFieldDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var values = RequestTypeDefinitionValidator.ValidateField(request);
        var mutable = await GetMutableDraftAsync(
            requestTypeId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        await EnsureUniqueFieldAsync(
            versionId,
            values.Key,
            values.SortOrder,
            excludedFieldId: null,
            cancellationToken);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var field = new RequestFieldDefinition
        {
            Id = Guid.NewGuid(),
            RequestTypeVersionId = versionId,
            Key = values.Key,
            NormalizedKey = values.Key,
            CreatedAtUtc = now,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        ApplyFieldValues(field, values);
        dbContext.RequestFieldDefinitions.Add(field);
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.RequestType, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(requestTypeId, versionId, cancellationToken);
    }

    public async Task<RequestTypeVersionResponse> UpdateFieldAsync(
        Guid requestTypeId,
        Guid versionId,
        Guid fieldId,
        UpdateRequestFieldDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var mutable = await GetMutableDraftAsync(
            requestTypeId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        var field = await GetFieldAsync(
            versionId,
            fieldId,
            request.FieldRowVersion,
            cancellationToken);
        var values = RequestTypeDefinitionValidator.ValidateField(field.Key, request);
        await EnsureUniqueFieldAsync(
            versionId,
            field.NormalizedKey,
            values.SortOrder,
            fieldId,
            cancellationToken);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        ApplyFieldValues(field, values);
        field.ModifiedAtUtc = now;
        field.ModifiedByAccount = actor.AccountName;
        field.ModifiedByUserId = actor.ApplicationUserId;
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.RequestType, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(requestTypeId, versionId, cancellationToken);
    }

    public async Task<RequestTypeVersionResponse> DeleteFieldAsync(
        Guid requestTypeId,
        Guid versionId,
        Guid fieldId,
        DeleteRequestFieldDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var mutable = await GetMutableDraftAsync(
            requestTypeId,
            versionId,
            request.VersionRowVersion,
            cancellationToken);
        var field = await GetFieldAsync(
            versionId,
            fieldId,
            request.FieldRowVersion,
            cancellationToken);
        var isReferenced = await dbContext.RequestContentValues
            .AsNoTracking()
            .AnyAsync(value => value.RequestFieldDefinitionId == fieldId, cancellationToken)
            || await dbContext.RequestDocuments
                .AsNoTracking()
                .AnyAsync(document => document.RequestFieldDefinitionId == fieldId, cancellationToken)
            || await dbContext.RequestRichDocumentRevisions
                .AsNoTracking()
                .AnyAsync(revision => revision.RequestFieldDefinitionId == fieldId, cancellationToken);

        if (isReferenced)
        {
            throw new AdministrationConflictException(
                "administration.request_field_in_use",
                "A request field referenced by request history cannot be deleted.");
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        dbContext.RequestFieldDefinitions.Remove(field);
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.RequestType, actor, now);
        await SaveChangesAsync(cancellationToken);
        return await GetVersionAsync(requestTypeId, versionId, cancellationToken);
    }

    public async Task<RequestTypeVersionResponse> PublishAsync(
        Guid requestTypeId,
        Guid versionId,
        RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var mutable = await GetMutableDraftAsync(
            requestTypeId,
            versionId,
            request.RowVersion,
            cancellationToken);
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await ReserveNavigationValuesAsync(
            requestTypeId,
            mutable.Version.RequestPrefix,
            mutable.Version.NavigationSlug,
            actor,
            now,
            cancellationToken);
        mutable.Version.Lifecycle = RequestTypeVersionLifecycle.Published;
        mutable.Version.PublishedAtUtc = now;
        mutable.Version.PublishedByAccount = actor.AccountName;
        mutable.Version.PublishedByUserId = actor.ApplicationUserId;
        SetModified(mutable.Version, actor, now);
        SetModified(mutable.RequestType, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(requestTypeId, versionId, cancellationToken);
    }

    public async Task<RequestTypeVersionResponse> CloneAsync(
        Guid requestTypeId,
        Guid versionId,
        RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = AdministrationEncoding.DecodeRowVersion(request.RowVersion);
        var source = await dbContext.RequestTypeVersions
            .Include(version => version.RequestType)
            .Include(version => version.Fields)
            .SingleOrDefaultAsync(
                version => version.Id == versionId
                           && version.RequestTypeId == requestTypeId,
                cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The request-type version does not exist.");
        EnsureRowVersion(source.RowVersion, expectedRowVersion);

        if (source.RequestType.IsArchived)
        {
            throw RequestTypeArchivedConflict();
        }

        if (source.Lifecycle == RequestTypeVersionLifecycle.Draft)
        {
            throw new AdministrationConflictException(
                "administration.request_type_version_is_draft",
                "A draft request-type version cannot be cloned into another draft.");
        }

        if (await dbContext.RequestTypeVersions.AnyAsync(
                version => version.RequestTypeId == requestTypeId
                           && version.Lifecycle == RequestTypeVersionLifecycle.Draft,
                cancellationToken))
        {
            throw DraftExistsConflict();
        }

        var nextVersionNumber = await dbContext.RequestTypeVersions
            .Where(version => version.RequestTypeId == requestTypeId)
            .MaxAsync(version => version.VersionNumber, cancellationToken) + 1;
        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var clone = new RequestTypeVersion
        {
            Id = Guid.NewGuid(),
            RequestTypeId = requestTypeId,
            VersionNumber = nextVersionNumber,
            NameEnglish = source.NameEnglish,
            NameArabic = source.NameArabic,
            DescriptionEnglish = source.DescriptionEnglish,
            DescriptionArabic = source.DescriptionArabic,
            RequestPrefix = source.RequestPrefix,
            NavigationSlug = source.NavigationSlug,
            NavigationOrder = source.NavigationOrder,
            Lifecycle = RequestTypeVersionLifecycle.Draft,
            CreatedAtUtc = now,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        dbContext.RequestTypeVersions.Add(clone);

        foreach (var sourceField in source.Fields.OrderBy(field => field.SortOrder))
        {
            dbContext.RequestFieldDefinitions.Add(new RequestFieldDefinition
            {
                Id = Guid.NewGuid(),
                RequestTypeVersionId = clone.Id,
                Key = sourceField.Key,
                NormalizedKey = sourceField.NormalizedKey,
                LabelEnglish = sourceField.LabelEnglish,
                LabelArabic = sourceField.LabelArabic,
                HelpTextEnglish = sourceField.HelpTextEnglish,
                HelpTextArabic = sourceField.HelpTextArabic,
                FieldType = sourceField.FieldType,
                IsRequired = sourceField.IsRequired,
                DefaultValueJson = sourceField.DefaultValueJson,
                ValidationConfigurationJson = sourceField.ValidationConfigurationJson,
                ChoiceConfigurationJson = sourceField.ChoiceConfigurationJson,
                SortOrder = sourceField.SortOrder,
                IsActive = sourceField.IsActive,
                DocumentMode = sourceField.DocumentMode,
                CreatedAtUtc = now,
                CreatedByAccount = actor.AccountName,
                CreatedByUserId = actor.ApplicationUserId
            });
        }

        SetModified(source.RequestType, actor, now);
        await SaveChangesAsync(cancellationToken, duplicatePossible: true);
        return await GetVersionAsync(requestTypeId, clone.Id, cancellationToken);
    }

    public async Task<RequestTypeDetailResponse> ArchiveAsync(
        Guid requestTypeId,
        RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = AdministrationEncoding.DecodeRowVersion(request.RowVersion);
        var requestType = await dbContext.RequestTypes
            .SingleOrDefaultAsync(type => type.Id == requestTypeId, cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The request type does not exist.");
        EnsureRowVersion(requestType.RowVersion, expectedRowVersion);

        if (requestType.IsArchived)
        {
            throw RequestTypeArchivedConflict();
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        requestType.IsArchived = true;
        requestType.ArchivedAtUtc = now;
        requestType.ArchivedByAccount = actor.AccountName;
        requestType.ArchivedByUserId = actor.ApplicationUserId;
        SetModified(requestType, actor, now);
        await SaveChangesAsync(cancellationToken);
        return await GetAsync(requestTypeId, cancellationToken);
    }

    public async Task<RequestTypeVersionResponse> ArchiveVersionAsync(
        Guid requestTypeId,
        Guid versionId,
        RequestTypeConcurrencyRequest request,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = AdministrationEncoding.DecodeRowVersion(request.RowVersion);
        var version = await dbContext.RequestTypeVersions
            .Include(entity => entity.RequestType)
            .SingleOrDefaultAsync(
                entity => entity.Id == versionId
                          && entity.RequestTypeId == requestTypeId,
                cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The request-type version does not exist.");
        EnsureRowVersion(version.RowVersion, expectedRowVersion);

        if (version.Lifecycle == RequestTypeVersionLifecycle.Archived)
        {
            throw new AdministrationConflictException(
                "administration.request_type_version_archived",
                "The request-type version is already archived.");
        }

        var actor = await actorAccessor.GetCurrentAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        version.Lifecycle = RequestTypeVersionLifecycle.Archived;
        version.ArchivedAtUtc = now;
        version.ArchivedByAccount = actor.AccountName;
        version.ArchivedByUserId = actor.ApplicationUserId;
        SetModified(version, actor, now);
        SetModified(version.RequestType, actor, now);
        await SaveChangesAsync(cancellationToken);
        return await GetVersionAsync(requestTypeId, versionId, cancellationToken);
    }

    private async Task<MutableVersion> GetMutableDraftAsync(
        Guid requestTypeId,
        Guid versionId,
        string? expectedRowVersion,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.RequestTypeVersions
            .Include(entity => entity.RequestType)
            .SingleOrDefaultAsync(
                entity => entity.Id == versionId
                          && entity.RequestTypeId == requestTypeId,
                cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The request-type version does not exist.");

        EnsureRowVersion(
            version.RowVersion,
            AdministrationEncoding.DecodeRowVersion(expectedRowVersion));

        if (version.RequestType.IsArchived)
        {
            throw RequestTypeArchivedConflict();
        }

        if (version.Lifecycle != RequestTypeVersionLifecycle.Draft)
        {
            throw new AdministrationConflictException(
                "administration.request_type_version_immutable",
                "Published or archived request-type versions are immutable. Clone the version to create a new draft.");
        }

        if (await dbContext.ApprovalRequests
                .AsNoTracking()
                .AnyAsync(
                    request => request.RequestTypeVersionId == versionId,
                    cancellationToken))
        {
            throw new AdministrationConflictException(
                "administration.request_type_version_in_use",
                "A request-type version referenced by a request is immutable. Clone it to create a new draft.");
        }

        return new MutableVersion(version.RequestType, version);
    }

    private async Task<RequestFieldDefinition> GetFieldAsync(
        Guid versionId,
        Guid fieldId,
        string? encodedRowVersion,
        CancellationToken cancellationToken)
    {
        var expectedRowVersion = AdministrationEncoding.DecodeRowVersion(encodedRowVersion);
        var field = await dbContext.RequestFieldDefinitions
            .SingleOrDefaultAsync(
                entity => entity.Id == fieldId
                          && entity.RequestTypeVersionId == versionId,
                cancellationToken)
            ?? throw new AdministrationNotFoundException(
                "The request field does not exist.");
        EnsureRowVersion(field.RowVersion, expectedRowVersion);
        return field;
    }

    private async Task EnsureUniqueCodeAsync(
        string normalizedCode,
        CancellationToken cancellationToken)
    {
        if (await dbContext.RequestTypes
                .AsNoTracking()
                .AnyAsync(
                    requestType => requestType.NormalizedCode == normalizedCode,
                    cancellationToken))
        {
            throw new AdministrationConflictException(
                "administration.duplicate_request_type_code",
                "A request type with the same normalized code already exists.");
        }
    }

    private async Task ReserveNavigationValuesAsync(
        Guid requestTypeId,
        string requestPrefix,
        string navigationSlug,
        AdministrationActor actor,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var prefixReservation = await dbContext.RequestTypePrefixReservations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                reservation => reservation.NormalizedPrefix == requestPrefix,
                cancellationToken);

        if (prefixReservation is not null
            && prefixReservation.RequestTypeId != requestTypeId)
        {
            throw DuplicatePrefixConflict();
        }

        if (prefixReservation is null)
        {
            dbContext.RequestTypePrefixReservations.Add(
                new RequestTypePrefixReservation
                {
                    NormalizedPrefix = requestPrefix,
                    RequestTypeId = requestTypeId,
                    ReservedAtUtc = timestamp,
                    ReservedByAccount = actor.AccountName,
                    ReservedByUserId = actor.ApplicationUserId
                });
        }

        var slugReservation = await dbContext.RequestTypeSlugReservations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                reservation => reservation.NormalizedSlug == navigationSlug,
                cancellationToken);

        if (slugReservation is not null
            && slugReservation.RequestTypeId != requestTypeId)
        {
            throw DuplicateSlugConflict();
        }

        if (slugReservation is null)
        {
            dbContext.RequestTypeSlugReservations.Add(
                new RequestTypeSlugReservation
                {
                    NormalizedSlug = navigationSlug,
                    RequestTypeId = requestTypeId,
                    ReservedAtUtc = timestamp,
                    ReservedByAccount = actor.AccountName,
                    ReservedByUserId = actor.ApplicationUserId
                });
        }
    }

    private async Task EnsureUniqueFieldAsync(
        Guid versionId,
        string normalizedKey,
        int sortOrder,
        Guid? excludedFieldId,
        CancellationToken cancellationToken)
    {
        var fields = dbContext.RequestFieldDefinitions
            .AsNoTracking()
            .Where(field => field.RequestTypeVersionId == versionId);

        if (excludedFieldId is Guid fieldId)
        {
            fields = fields.Where(field => field.Id != fieldId);
        }

        if (await fields.AnyAsync(
                field => field.NormalizedKey == normalizedKey,
                cancellationToken))
        {
            throw new AdministrationConflictException(
                "administration.duplicate_request_field_key",
                "A field with the same normalized key already exists in this version.");
        }

        if (await fields.AnyAsync(
                field => field.SortOrder == sortOrder,
                cancellationToken))
        {
            throw new AdministrationConflictException(
                "administration.duplicate_request_field_order",
                "A field with the same sort order already exists in this version.");
        }
    }

    private async Task<Dictionary<Guid, IReadOnlyList<VersionDetails>>> GetVersionDetailsAsync(
        IReadOnlyCollection<Guid> requestTypeIds,
        bool includeFields,
        CancellationToken cancellationToken)
    {
        if (requestTypeIds.Count == 0)
        {
            return [];
        }

        var versions = await dbContext.RequestTypeVersions
            .AsNoTracking()
            .Where(version => requestTypeIds.Contains(version.RequestTypeId))
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => new VersionProjection(
                version.Id,
                version.RequestTypeId,
                version.VersionNumber,
                version.NameEnglish,
                version.NameArabic,
                version.DescriptionEnglish,
                version.DescriptionArabic,
                version.RequestPrefix,
                version.NavigationSlug,
                version.NavigationOrder,
                version.Lifecycle,
                version.CreatedAtUtc,
                version.CreatedByAccount,
                version.ModifiedAtUtc,
                version.ModifiedByAccount,
                version.PublishedAtUtc,
                version.PublishedByAccount,
                version.ArchivedAtUtc,
                version.ArchivedByAccount,
                version.RowVersion))
            .ToListAsync(cancellationToken);
        var versionIds = versions.Select(version => version.Id).ToArray();
        var fieldCounts = await dbContext.RequestFieldDefinitions
            .AsNoTracking()
            .Where(field => versionIds.Contains(field.RequestTypeVersionId))
            .GroupBy(field => field.RequestTypeVersionId)
            .Select(group => new { VersionId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.VersionId, row => row.Count, cancellationToken);
        var requestCounts = await dbContext.ApprovalRequests
            .AsNoTracking()
            .Where(request => versionIds.Contains(request.RequestTypeVersionId))
            .GroupBy(request => request.RequestTypeVersionId)
            .Select(group => new { VersionId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.VersionId, row => row.Count, cancellationToken);
        Dictionary<Guid, IReadOnlyList<RequestFieldDefinitionResponse>> fieldsByVersionId = [];

        if (includeFields && versionIds.Length > 0)
        {
            var fields = await dbContext.RequestFieldDefinitions
                .AsNoTracking()
                .Where(field => versionIds.Contains(field.RequestTypeVersionId))
                .OrderBy(field => field.SortOrder)
                .ThenBy(field => field.Key)
                .Select(field => new FieldProjection(
                    field.Id,
                    field.RequestTypeVersionId,
                    field.Key,
                    field.LabelEnglish,
                    field.LabelArabic,
                    field.HelpTextEnglish,
                    field.HelpTextArabic,
                    field.FieldType,
                    field.IsRequired,
                    field.DefaultValueJson,
                    field.ValidationConfigurationJson,
                    field.ChoiceConfigurationJson,
                    field.SortOrder,
                    field.IsActive,
                    field.DocumentMode,
                    field.CreatedAtUtc,
                    field.CreatedByAccount,
                    field.ModifiedAtUtc,
                    field.ModifiedByAccount,
                    field.RowVersion))
                .ToListAsync(cancellationToken);
            fieldsByVersionId = fields
                .GroupBy(field => field.RequestTypeVersionId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<RequestFieldDefinitionResponse>)group
                        .Select(CreateFieldResponse)
                        .ToArray());
        }

        return versions
            .GroupBy(version => version.RequestTypeId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<VersionDetails>)group
                    .Select(version => new VersionDetails(
                        version,
                        fieldCounts.GetValueOrDefault(version.Id),
                        requestCounts.GetValueOrDefault(version.Id),
                        fieldsByVersionId.GetValueOrDefault(version.Id) ?? []))
                    .ToArray());
    }

    private static RequestTypeDetailResponse CreateDetailResponse(
        RequestTypeProjection root,
        IReadOnlyList<VersionDetails> versions) =>
        new(
            root.Id,
            root.Code,
            root.IsArchived,
            versions.Select(CreateVersionResponse).ToArray(),
            root.CreatedAtUtc,
            root.CreatedByAccount,
            root.ModifiedAtUtc,
            root.ModifiedByAccount,
            root.ArchivedAtUtc,
            root.ArchivedByAccount,
            AdministrationEncoding.EncodeRowVersion(root.RowVersion));

    private static RequestTypeVersionSummaryResponse CreateSummaryResponse(
        VersionDetails details)
    {
        var version = details.Version;
        return new RequestTypeVersionSummaryResponse(
            version.Id,
            version.VersionNumber,
            version.NameEnglish,
            version.NameArabic,
            version.DescriptionEnglish,
            version.DescriptionArabic,
            version.RequestPrefix,
            version.NavigationSlug,
            version.NavigationOrder,
            RequestTypeDefinitionValidator.LifecycleName(version.Lifecycle),
            details.FieldCount,
            details.RequestCount > 0,
            details.RequestCount,
            version.CreatedAtUtc,
            version.CreatedByAccount,
            version.ModifiedAtUtc,
            version.ModifiedByAccount,
            version.PublishedAtUtc,
            version.PublishedByAccount,
            version.ArchivedAtUtc,
            version.ArchivedByAccount,
            AdministrationEncoding.EncodeRowVersion(version.RowVersion));
    }

    private static RequestTypeVersionResponse CreateVersionResponse(
        VersionDetails details)
    {
        var version = details.Version;
        return new RequestTypeVersionResponse(
            version.Id,
            version.VersionNumber,
            version.NameEnglish,
            version.NameArabic,
            version.DescriptionEnglish,
            version.DescriptionArabic,
            version.RequestPrefix,
            version.NavigationSlug,
            version.NavigationOrder,
            RequestTypeDefinitionValidator.LifecycleName(version.Lifecycle),
            details.RequestCount > 0,
            details.RequestCount,
            details.Fields,
            version.CreatedAtUtc,
            version.CreatedByAccount,
            version.ModifiedAtUtc,
            version.ModifiedByAccount,
            version.PublishedAtUtc,
            version.PublishedByAccount,
            version.ArchivedAtUtc,
            version.ArchivedByAccount,
            AdministrationEncoding.EncodeRowVersion(version.RowVersion));
    }

    private static RequestFieldDefinitionResponse CreateFieldResponse(
        FieldProjection field) =>
        new(
            field.Id,
            field.Key,
            field.LabelEnglish,
            field.LabelArabic,
            field.HelpTextEnglish,
            field.HelpTextArabic,
            RequestTypeDefinitionValidator.FieldTypeName(field.FieldType),
            field.IsRequired,
            ParseJson(field.DefaultValueJson),
            ParseJson(field.ValidationConfigurationJson),
            ParseJson(field.ChoiceConfigurationJson),
            field.SortOrder,
            field.IsActive,
            RequestTypeDefinitionValidator.DocumentModeName(field.DocumentMode),
            field.CreatedAtUtc,
            field.CreatedByAccount,
            field.ModifiedAtUtc,
            field.ModifiedByAccount,
            AdministrationEncoding.EncodeRowVersion(field.RowVersion));

    private static JsonElement? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static RequestTypeVersion CreateVersion(
        Guid requestTypeId,
        int versionNumber,
        RequestTypeVersionValues values,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        var version = new RequestTypeVersion
        {
            Id = Guid.NewGuid(),
            RequestTypeId = requestTypeId,
            VersionNumber = versionNumber,
            Lifecycle = RequestTypeVersionLifecycle.Draft,
            CreatedAtUtc = timestamp,
            CreatedByAccount = actor.AccountName,
            CreatedByUserId = actor.ApplicationUserId
        };
        ApplyVersionValues(version, values);
        return version;
    }

    private static void ApplyVersionValues(
        RequestTypeVersion version,
        RequestTypeVersionValues values)
    {
        version.NameEnglish = values.NameEnglish;
        version.NameArabic = values.NameArabic;
        version.DescriptionEnglish = values.DescriptionEnglish;
        version.DescriptionArabic = values.DescriptionArabic;
        version.RequestPrefix = values.RequestPrefix;
        version.NavigationSlug = values.NavigationSlug;
        version.NavigationOrder = values.NavigationOrder;
    }

    private static void ApplyFieldValues(
        RequestFieldDefinition field,
        RequestFieldValues values)
    {
        field.LabelEnglish = values.LabelEnglish;
        field.LabelArabic = values.LabelArabic;
        field.HelpTextEnglish = values.HelpTextEnglish;
        field.HelpTextArabic = values.HelpTextArabic;
        field.FieldType = values.FieldType;
        field.IsRequired = values.IsRequired;
        field.DefaultValueJson = values.DefaultValueJson;
        field.ValidationConfigurationJson = values.ValidationConfigurationJson;
        field.ChoiceConfigurationJson = values.ChoiceConfigurationJson;
        field.SortOrder = values.SortOrder;
        field.IsActive = values.IsActive;
        field.DocumentMode = values.DocumentMode;
    }

    private static void SetModified(
        RequestType requestType,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        requestType.ModifiedAtUtc = timestamp;
        requestType.ModifiedByAccount = actor.AccountName;
        requestType.ModifiedByUserId = actor.ApplicationUserId;
    }

    private static void SetModified(
        RequestTypeVersion version,
        AdministrationActor actor,
        DateTimeOffset timestamp)
    {
        version.ModifiedAtUtc = timestamp;
        version.ModifiedByAccount = actor.AccountName;
        version.ModifiedByUserId = actor.ApplicationUserId;
    }

    private static void EnsureRowVersion(
        byte[] actual,
        byte[] expected)
    {
        if (!actual.SequenceEqual(expected))
        {
            throw ConcurrencyConflict();
        }
    }

    private async Task SaveChangesAsync(
        CancellationToken cancellationToken,
        bool duplicatePossible = false)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw ConcurrencyConflict();
        }
        catch (DbUpdateException exception)
            when (duplicatePossible
                  && exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            if (exception.InnerException.Message.Contains(
                    "RequestTypePrefixReservations",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw DuplicatePrefixConflict();
            }

            if (exception.InnerException.Message.Contains(
                    "RequestTypeSlugReservations",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw DuplicateSlugConflict();
            }

            throw new AdministrationConflictException(
                "administration.request_type_definition_conflict",
                "The request-type definition conflicts with an existing code, draft, field key or ordering value. Reload and try again.");
        }
    }

    private static AdministrationConflictException ConcurrencyConflict() =>
        new(
            "administration.concurrency_conflict",
            "The resource was changed by another administrator. Reload it and try again.");

    private static AdministrationConflictException RequestTypeArchivedConflict() =>
        new(
            "administration.request_type_archived",
            "An archived request type cannot be changed.");

    private static AdministrationConflictException DraftExistsConflict() =>
        new(
            "administration.request_type_draft_exists",
            "This request type already has a draft version.");

    private static AdministrationConflictException DuplicatePrefixConflict() =>
        new(
            "administration.duplicate_request_prefix",
            "Another request type already owns this request-number prefix.");

    private static AdministrationConflictException DuplicateSlugConflict() =>
        new(
            "administration.duplicate_navigation_slug",
            "Another request type already owns this navigation slug.");

    private static string? NormalizeSearch(string? search)
    {
        var normalized = search?.Trim();

        if (normalized?.Length > MaximumSearchLength)
        {
            throw AdministrationValidationException.For(
                "search",
                $"Search text cannot exceed {MaximumSearchLength} characters.");
        }

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static int DisplayLifecycleOrder(RequestTypeVersionLifecycle lifecycle) =>
        lifecycle switch
        {
            RequestTypeVersionLifecycle.Draft => 0,
            RequestTypeVersionLifecycle.Published => 1,
            RequestTypeVersionLifecycle.Archived => 2,
            _ => 3
        };

    private sealed record MutableVersion(
        RequestType RequestType,
        RequestTypeVersion Version);

    private sealed record RequestTypeProjection(
        Guid Id,
        string Code,
        bool IsArchived,
        DateTimeOffset CreatedAtUtc,
        string CreatedByAccount,
        DateTimeOffset? ModifiedAtUtc,
        string? ModifiedByAccount,
        DateTimeOffset? ArchivedAtUtc,
        string? ArchivedByAccount,
        byte[] RowVersion);

    private sealed record VersionProjection(
        Guid Id,
        Guid RequestTypeId,
        int VersionNumber,
        string NameEnglish,
        string NameArabic,
        string DescriptionEnglish,
        string DescriptionArabic,
        string RequestPrefix,
        string NavigationSlug,
        int NavigationOrder,
        RequestTypeVersionLifecycle Lifecycle,
        DateTimeOffset CreatedAtUtc,
        string CreatedByAccount,
        DateTimeOffset? ModifiedAtUtc,
        string? ModifiedByAccount,
        DateTimeOffset? PublishedAtUtc,
        string? PublishedByAccount,
        DateTimeOffset? ArchivedAtUtc,
        string? ArchivedByAccount,
        byte[] RowVersion);

    private sealed record FieldProjection(
        Guid Id,
        Guid RequestTypeVersionId,
        string Key,
        string LabelEnglish,
        string LabelArabic,
        string? HelpTextEnglish,
        string? HelpTextArabic,
        RequestFieldType FieldType,
        bool IsRequired,
        string? DefaultValueJson,
        string? ValidationConfigurationJson,
        string? ChoiceConfigurationJson,
        int SortOrder,
        bool IsActive,
        DocumentFieldMode? DocumentMode,
        DateTimeOffset CreatedAtUtc,
        string CreatedByAccount,
        DateTimeOffset? ModifiedAtUtc,
        string? ModifiedByAccount,
        byte[] RowVersion);

    private sealed record VersionDetails(
        VersionProjection Version,
        int FieldCount,
        int RequestCount,
        IReadOnlyList<RequestFieldDefinitionResponse> Fields)
    {
        public Guid Id => Version.Id;

        public int VersionNumber => Version.VersionNumber;

        public RequestTypeVersionLifecycle Lifecycle => Version.Lifecycle;
    }
}
