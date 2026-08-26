using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IndependentApproval.Api.Contracts.Administration;
using IndependentApproval.Api.Contracts.Administration.RequestTypes;
using IndependentApproval.Api.Contracts.Common;
using IndependentApproval.Api.Contracts.Security;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Domain.Workflows;
using IndependentApproval.Api.Tests.Infrastructure;

namespace IndependentApproval.Api.Tests;

public sealed class RequestTypeAdministrationTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Request_type_apis_enforce_the_system_administrator_policy()
    {
        using var administrator = CreateAdministratorClient();
        using var administratorResponse = await administrator.GetAsync(
            "/api/admin/request-types/system-fields");
        Assert.Equal(HttpStatusCode.OK, administratorResponse.StatusCode);

        using var applicationUser = fixture.CreateClient(
            AdministrationApiFixture.ApplicationUserAccount);
        using var listResponse = await applicationUser.GetAsync(
            "/api/admin/request-types");
        await AssertProblemAsync(
            listResponse,
            HttpStatusCode.Forbidden,
            "authorization.forbidden");

        using var createResponse = await SendWithAntiforgeryAsync(
            applicationUser,
            HttpMethod.Post,
            "/api/admin/request-types",
            CreateRequestTypePayload());
        await AssertProblemAsync(
            createResponse,
            HttpStatusCode.Forbidden,
            "authorization.forbidden");
    }

    [Fact]
    public async Task Every_unsafe_request_type_endpoint_requires_antiforgery()
    {
        using var client = CreateAdministratorClient();
        var requestTypeId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var endpoints = new (HttpMethod Method, string Path)[]
        {
            (HttpMethod.Post, "/api/admin/request-types"),
            (HttpMethod.Put, $"/api/admin/request-types/{requestTypeId}/versions/{versionId}"),
            (HttpMethod.Post, $"/api/admin/request-types/{requestTypeId}/versions/{versionId}/fields"),
            (HttpMethod.Put, $"/api/admin/request-types/{requestTypeId}/versions/{versionId}/fields/{fieldId}"),
            (HttpMethod.Delete, $"/api/admin/request-types/{requestTypeId}/versions/{versionId}/fields/{fieldId}"),
            (HttpMethod.Post, $"/api/admin/request-types/{requestTypeId}/versions/{versionId}/publish"),
            (HttpMethod.Post, $"/api/admin/request-types/{requestTypeId}/versions/{versionId}/clone"),
            (HttpMethod.Post, $"/api/admin/request-types/{requestTypeId}/archive"),
            (HttpMethod.Post, $"/api/admin/request-types/{requestTypeId}/versions/{versionId}/archive")
        };

        foreach (var endpoint in endpoints)
        {
            using var request = new HttpRequestMessage(endpoint.Method, endpoint.Path)
            {
                Content = JsonContent.Create(new { })
            };
            using var response = await client.SendAsync(request);

            await AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "security.antiforgery_validation_failed");
        }
    }

    [Fact]
    public async Task Create_list_and_detail_preserve_bilingual_draft_definition()
    {
        using var client = CreateAdministratorClient();
        var payload = CreateRequestTypePayload(
            nameEnglish: "Travel authorization",
            nameArabic: "تصريح سفر");

        var created = await CreateRequestTypeAsync(client, payload);
        var draft = Assert.Single(created.Versions);

        Assert.Equal(payload.Code!.Trim().ToUpperInvariant(), created.Code);
        Assert.False(created.IsArchived);
        Assert.Equal("draft", draft.Lifecycle);
        Assert.Equal("Travel authorization", draft.NameEnglish);
        Assert.Equal("تصريح سفر", draft.NameArabic);
        Assert.Equal(payload.DescriptionEnglish, draft.DescriptionEnglish);
        Assert.Equal(payload.DescriptionArabic, draft.DescriptionArabic);
        Assert.Equal(1, draft.VersionNumber);
        Assert.Empty(draft.Fields);
        AssertValidRowVersion(created.RowVersion);
        AssertValidRowVersion(draft.RowVersion);

        var list = await client.GetFromJsonAsync<
            PagedResponse<RequestTypeListItemResponse>>(
            $"/api/admin/request-types?search={Uri.EscapeDataString(created.Code)}" +
            "&page=1&pageSize=25");
        Assert.NotNull(list);
        var item = Assert.Single(list.Items, item => item.Id == created.Id);
        Assert.Equal(draft.Id, item.DraftVersionId);
        Assert.Null(item.LatestPublishedVersionId);
        Assert.Equal("تصريح سفر", item.DisplayVersion?.NameArabic);

        var detail = await client.GetFromJsonAsync<RequestTypeDetailResponse>(
            $"/api/admin/request-types/{created.Id}");
        Assert.NotNull(detail);
        Assert.Equal(created.Code, detail.Code);
        Assert.Equal("Travel authorization", Assert.Single(detail.Versions).NameEnglish);

        var version = await client.GetFromJsonAsync<RequestTypeVersionResponse>(
            $"/api/admin/request-types/{created.Id}/versions/{draft.Id}");
        Assert.NotNull(version);
        Assert.Equal("تصريح سفر", version.NameArabic);
    }

    [Fact]
    public async Task Designer_accepts_every_field_type_document_mode_and_json_native_configuration()
    {
        using var client = CreateAdministratorClient();
        var created = await CreateRequestTypeAsync(client);
        var version = Assert.Single(created.Versions);
        var choiceConfig = Json(
            """
            {"options":[
              {"value":"open","labelEnglish":"Open","labelArabic":"مفتوح","sortOrder":1,"isActive":true},
              {"value":"closed","labelEnglish":"Closed","labelArabic":"مغلق","sortOrder":2,"isActive":true}
            ]}
            """);
        var fields = new[]
        {
            Field("short_text", "shortText", 1, Json("\"Alpha\""),
                Json("{\"minLength\":1,\"maxLength\":20,\"pattern\":\"^[A-Za-z]+$\"}")),
            Field("long_text", "longText", 2, Json("\"Longer text\""),
                Json("{\"minLength\":1,\"maxLength\":1000}")),
            Field("integer_value", "integer", 3, Json("3"),
                Json("{\"minimum\":1,\"maximum\":10}")),
            Field("decimal_value", "decimal", 4, Json("1.25"),
                Json("{\"minimum\":0,\"maximum\":5,\"decimalPlaces\":2}")),
            Field("date_value", "date", 5, Json("\"2026-08-24\""),
                Json("{\"minimum\":\"2026-01-01\",\"maximum\":\"2026-12-31\"}")),
            Field("datetime_value", "dateTime", 6,
                Json("\"2026-08-24T10:30:00+02:00\""),
                Json("{\"minimum\":\"2026-01-01T00:00:00Z\",\"maximum\":\"2026-12-31T23:59:59Z\"}")),
            Field("yes_no", "yesNo", 7, Json("true")),
            Field("single_choice", "singleChoice", 8, Json("\"open\""),
                choiceConfig: choiceConfig),
            Field("multiple_choice", "multipleChoice", 9,
                Json("[\"open\",\"closed\"]"),
                Json("{\"minimumSelections\":1,\"maximumSelections\":2}"),
                choiceConfig),
            Field("directory_user", "activeDirectoryUser", 10),
            Field("application_role", "applicationRole", 11),
            Field("file_document", "fileDocument", 12, validationConfig:
                Json("{\"maximumDocuments\":5,\"allowedExtensions\":[\".pdf\"]}"),
                documentMode: "uploadOnly"),
            Field("rich_document", "richDocument", 13, validationConfig:
                Json("{\"maximumDocuments\":2,\"allowedExtensions\":[\".pdf\",\".docx\"]}"),
                documentMode: "createInEditorOnly"),
            Field("hybrid_document", "richDocument", 14, validationConfig:
                Json("{\"maximumDocuments\":3,\"allowedExtensions\":[\".pdf\"]}"),
                documentMode: "uploadOrCreate")
        };

        foreach (var field in fields)
        {
            version = await AddFieldAsync(client, created.Id, version, field);
        }

        Assert.Equal(14, version.Fields.Count);
        Assert.Equal(
            new[]
            {
                "shortText", "longText", "integer", "decimal", "date", "dateTime",
                "yesNo", "singleChoice", "multipleChoice", "activeDirectoryUser",
                "applicationRole", "fileDocument", "richDocument"
            },
            version.Fields.Select(field => field.FieldType).Distinct().ToArray());
        Assert.Equal(
            new[] { "uploadOnly", "createInEditorOnly", "uploadOrCreate" },
            version.Fields
                .Select(field => field.DocumentMode)
                .Where(mode => mode is not null)
                .Cast<string>()
                .ToArray());

        var shortText = version.Fields.Single(field => field.Key == "short_text");
        Assert.True(shortText.DefaultValue.HasValue);
        Assert.Equal(JsonValueKind.String, shortText.DefaultValue.Value.ValueKind);
        Assert.True(shortText.ValidationConfig.HasValue);
        Assert.Equal(JsonValueKind.Object, shortText.ValidationConfig.Value.ValueKind);
        Assert.Equal(
            20,
            shortText.ValidationConfig.Value.GetProperty("maxLength").GetInt32());
        Assert.Equal("الحقل short_text", shortText.LabelArabic);
        var singleChoice = version.Fields.Single(field => field.Key == "single_choice");
        Assert.True(singleChoice.ChoiceConfig.HasValue);
        Assert.Equal(JsonValueKind.Object, singleChoice.ChoiceConfig.Value.ValueKind);
        var options = singleChoice.ChoiceConfig.Value.GetProperty("options");
        Assert.Equal(JsonValueKind.Array, options.ValueKind);
        Assert.Equal("مفتوح", options.EnumerateArray().First()
            .GetProperty("labelArabic").GetString());
        Assert.True(singleChoice.DefaultValue.HasValue);
        Assert.Equal("open", singleChoice.DefaultValue.Value.GetString());
    }

    [Fact]
    public async Task Field_validation_rejects_invalid_bounds_choices_labels_and_defaults()
    {
        using var client = CreateAdministratorClient();
        var created = await CreateRequestTypeAsync(client);
        var version = Assert.Single(created.Versions);
        var invalidFields = new[]
        {
            Field("invalid_bounds", "shortText", 1, Json("\"value\""),
                Json("{\"minLength\":20,\"maxLength\":10}")),
            Field("invalid_document_bounds", "fileDocument", 2,
                validationConfig: Json("{\"maximumDocuments\":101,\"allowedExtensions\":[\"pdf\"]}"),
                documentMode: "uploadOnly"),
            Field("missing_choice_label", "singleChoice", 3, Json("\"one\""),
                choiceConfig: Json(
                    "{\"options\":[{\"value\":\"one\",\"labelEnglish\":\"One\",\"sortOrder\":1,\"isActive\":true}]}")),
            Field("empty_choices", "singleChoice", 4, Json("\"one\""),
                choiceConfig: Json("{\"options\":[]}")),
            Field("invalid_choice_default", "singleChoice", 5, Json("\"inactive\""),
                choiceConfig: Json(
                    "{\"options\":[" +
                    "{\"value\":\"active\",\"labelEnglish\":\"Active\",\"labelArabic\":\"نشط\",\"sortOrder\":1,\"isActive\":true}," +
                    "{\"value\":\"inactive\",\"labelEnglish\":\"Inactive\",\"labelArabic\":\"غير نشط\",\"sortOrder\":2,\"isActive\":false}]}"))
        };

        foreach (var invalidField in invalidFields)
        {
            var request = invalidField with
            {
                VersionRowVersion = version.RowVersion
            };
            using var response = await SendWithAntiforgeryAsync(
                client,
                HttpMethod.Post,
                $"/api/admin/request-types/{created.Id}/versions/{version.Id}/fields",
                request);

            await AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "administration.validation_failed");
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("errors", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SqlException", body, StringComparison.OrdinalIgnoreCase);
        }

        var unchanged = await client.GetFromJsonAsync<RequestTypeVersionResponse>(
            $"/api/admin/request-types/{created.Id}/versions/{version.Id}");
        Assert.NotNull(unchanged);
        Assert.Empty(unchanged.Fields);
        Assert.Equal(version.RowVersion, unchanged.RowVersion);
    }

    [Fact]
    public async Task System_and_internal_field_keys_are_reserved_and_protected()
    {
        using var client = CreateAdministratorClient();
        var systemFields = await client.GetFromJsonAsync<List<SystemRequestFieldResponse>>(
            "/api/admin/request-types/system-fields");
        Assert.NotNull(systemFields);
        Assert.Contains(systemFields, field => field.Key == "title" && field.IsEditable);
        Assert.All(systemFields, field => Assert.False(field.IsRemovable));
        Assert.All(
            systemFields.Where(field => field.Key != "title"),
            field => Assert.False(field.IsEditable));

        var created = await CreateRequestTypeAsync(client);
        var version = Assert.Single(created.Versions);
        var reservedKeys = systemFields.Select(field => field.Key).Concat(
            new[]
            {
                "id", "rowVersion", "requestTypeId", "requestTypeVersionId",
                "workflowDefinitionId", "workflowVersionId", "currentWorkflowStepId"
            });
        var sortOrder = 1;

        foreach (var key in reservedKeys)
        {
            var request = Field(key.ToUpperInvariant(), "shortText", sortOrder++) with
            {
                VersionRowVersion = version.RowVersion
            };
            using var response = await SendWithAntiforgeryAsync(
                client,
                HttpMethod.Post,
                $"/api/admin/request-types/{created.Id}/versions/{version.Id}/fields",
                request);

            await AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "administration.validation_failed");
        }

        var unchanged = await client.GetFromJsonAsync<RequestTypeVersionResponse>(
            $"/api/admin/request-types/{created.Id}/versions/{version.Id}");
        Assert.NotNull(unchanged);
        Assert.Empty(unchanged.Fields);
    }

    [Fact]
    public async Task Mutations_reject_missing_parent_and_field_concurrency_tokens()
    {
        using var client = CreateAdministratorClient();
        var created = await CreateRequestTypeAsync(client);
        var draft = Assert.Single(created.Versions);
        var missingParentTokenRequests = new (HttpMethod Method, string Path, object Body)[]
        {
            (
                HttpMethod.Put,
                $"/api/admin/request-types/{created.Id}/versions/{draft.Id}",
                VersionUpdate(draft, "Missing token") with { RowVersion = null }),
            (
                HttpMethod.Post,
                $"/api/admin/request-types/{created.Id}/versions/{draft.Id}/fields",
                Field("missing_parent_token", "shortText", 1)),
            (
                HttpMethod.Post,
                $"/api/admin/request-types/{created.Id}/versions/{draft.Id}/publish",
                new RequestTypeConcurrencyRequest(null))
        };

        foreach (var mutation in missingParentTokenRequests)
        {
            using var response = await SendWithAntiforgeryAsync(
                client,
                mutation.Method,
                mutation.Path,
                mutation.Body);
            await AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "administration.validation_failed");
        }

        var withField = await AddFieldAsync(
            client,
            created.Id,
            draft,
            Field("field_with_token", "shortText", 1));
        var field = Assert.Single(withField.Fields);
        using var missingFieldTokenResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/request-types/{created.Id}/versions/{draft.Id}/fields/{field.Id}",
            FieldUpdate(field, withField.RowVersion, "Missing field token") with
            {
                FieldRowVersion = null
            });
        await AssertProblemAsync(
            missingFieldTokenResponse,
            HttpStatusCode.BadRequest,
            "administration.validation_failed");
    }

    [Fact]
    public async Task Publish_clone_and_archive_enforce_explicit_version_lifecycle()
    {
        using var client = CreateAdministratorClient();
        var created = await CreateRequestTypeAsync(client);
        var draft = Assert.Single(created.Versions);
        draft = await AddFieldAsync(
            client,
            created.Id,
            draft,
            Field("subject_detail", "shortText", 1));
        var field = Assert.Single(draft.Fields);

        var published = await MutateVersionAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/request-types/{created.Id}/versions/{draft.Id}/publish",
            new RequestTypeConcurrencyRequest(draft.RowVersion),
            HttpStatusCode.OK);
        Assert.Equal("published", published.Lifecycle);
        Assert.NotNull(published.PublishedAtUtc);

        var immutableMutations = new (HttpMethod Method, string Path, object Body)[]
        {
            (
                HttpMethod.Put,
                $"/api/admin/request-types/{created.Id}/versions/{published.Id}",
                VersionUpdate(published, "Forbidden metadata change")),
            (
                HttpMethod.Post,
                $"/api/admin/request-types/{created.Id}/versions/{published.Id}/fields",
                Field("forbidden_field", "shortText", 2) with
                {
                    VersionRowVersion = published.RowVersion
                }),
            (
                HttpMethod.Put,
                $"/api/admin/request-types/{created.Id}/versions/{published.Id}/fields/{field.Id}",
                FieldUpdate(field, published.RowVersion, "Forbidden field change")),
            (
                HttpMethod.Delete,
                $"/api/admin/request-types/{created.Id}/versions/{published.Id}/fields/{field.Id}",
                new DeleteRequestFieldDefinitionRequest(
                    published.RowVersion,
                    field.RowVersion))
        };

        foreach (var mutation in immutableMutations)
        {
            using var response = await SendWithAntiforgeryAsync(
                client,
                mutation.Method,
                mutation.Path,
                mutation.Body);
            await AssertProblemAsync(
                response,
                HttpStatusCode.Conflict,
                "administration.request_type_version_immutable");
        }

        var clone = await MutateVersionAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/request-types/{created.Id}/versions/{published.Id}/clone",
            new RequestTypeConcurrencyRequest(published.RowVersion),
            HttpStatusCode.Created);
        Assert.Equal(2, clone.VersionNumber);
        Assert.Equal("draft", clone.Lifecycle);
        Assert.Equal(field.Key, Assert.Single(clone.Fields).Key);
        Assert.NotEqual(field.Id, clone.Fields[0].Id);

        using (var duplicateDraftResponse = await SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Post,
                   $"/api/admin/request-types/{created.Id}/versions/{published.Id}/clone",
                   new RequestTypeConcurrencyRequest(published.RowVersion)))
        {
            await AssertProblemAsync(
                duplicateDraftResponse,
                HttpStatusCode.Conflict,
                "administration.request_type_draft_exists");
        }

        var archivedDraft = await MutateVersionAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/request-types/{created.Id}/versions/{clone.Id}/archive",
            new RequestTypeConcurrencyRequest(clone.RowVersion),
            HttpStatusCode.OK);
        Assert.Equal("archived", archivedDraft.Lifecycle);

        using (var alreadyArchivedResponse = await SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Post,
                   $"/api/admin/request-types/{created.Id}/versions/{clone.Id}/archive",
                   new RequestTypeConcurrencyRequest(archivedDraft.RowVersion)))
        {
            await AssertProblemAsync(
                alreadyArchivedResponse,
                HttpStatusCode.Conflict,
                "administration.request_type_version_archived");
        }

        var currentDetail = await client.GetFromJsonAsync<RequestTypeDetailResponse>(
            $"/api/admin/request-types/{created.Id}");
        Assert.NotNull(currentDetail);
        using var archiveRootResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/request-types/{created.Id}/archive",
            new RequestTypeConcurrencyRequest(currentDetail.RowVersion));
        Assert.Equal(HttpStatusCode.OK, archiveRootResponse.StatusCode);
        var archivedRoot = await archiveRootResponse.Content
            .ReadFromJsonAsync<RequestTypeDetailResponse>();
        Assert.NotNull(archivedRoot);
        Assert.True(archivedRoot.IsArchived);

        using var archivedMutationResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/request-types/{created.Id}/versions/{published.Id}/clone",
            new RequestTypeConcurrencyRequest(published.RowVersion));
        await AssertProblemAsync(
            archivedMutationResponse,
            HttpStatusCode.Conflict,
            "administration.request_type_archived");
    }

    [Fact]
    public async Task A_version_referenced_by_request_history_is_immutable_even_if_marked_draft()
    {
        using var client = CreateAdministratorClient();
        var created = await CreateRequestTypeAsync(client);
        var draft = Assert.Single(created.Versions);
        await fixture.QueryDatabaseAsync(async dbContext =>
        {
            var now = DateTimeOffset.UtcNow;
            var workflowDefinitionId = Guid.NewGuid();
            var workflowVersionId = Guid.NewGuid();
            var workflowCode = $"LOCK_{Guid.NewGuid():N}".ToUpperInvariant();
            var workflowSlug = $"lock-{Guid.NewGuid():N}";
            dbContext.WorkflowDefinitions.Add(new WorkflowDefinition
            {
                Id = workflowDefinitionId,
                Code = workflowCode,
                NormalizedCode = workflowCode,
                CreatedAtUtc = now,
                CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                CreatedByUserId = fixture.BootstrapAdministratorId
            });
            dbContext.WorkflowSlugReservations.Add(new WorkflowSlugReservation
            {
                NormalizedSlug = workflowSlug,
                WorkflowDefinitionId = workflowDefinitionId,
                ReservedAtUtc = now,
                ReservedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                ReservedByUserId = fixture.BootstrapAdministratorId
            });
            dbContext.WorkflowVersions.Add(new WorkflowVersion
            {
                Id = workflowVersionId,
                WorkflowDefinitionId = workflowDefinitionId,
                RequestTypeVersionId = draft.Id,
                VersionNumber = 1,
                NameEnglish = "Request-type lock workflow",
                NameArabic = "سير عمل قفل نوع الطلب",
                DescriptionEnglish = "Owns the request used to prove historical locking.",
                DescriptionArabic = "يمتلك الطلب المستخدم لإثبات القفل التاريخي.",
                NavigationLabelEnglish = "Lock workflow",
                NavigationLabelArabic = "سير عمل القفل",
                NavigationSlug = workflowSlug,
                NavigationOrder = 1,
                Lifecycle = WorkflowVersionLifecycle.Draft,
                CreatedAtUtc = now,
                CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                CreatedByUserId = fixture.BootstrapAdministratorId
            });
            dbContext.ApprovalRequests.Add(new ApprovalRequest
            {
                Id = Guid.NewGuid(),
                RequestNumber = $"{draft.RequestPrefix}-2026-999999",
                RequestTypeId = created.Id,
                RequestTypeVersionId = draft.Id,
                WorkflowDefinitionId = workflowDefinitionId,
                WorkflowVersionId = workflowVersionId,
                Title = "Historical version lock test",
                Status = ApprovalRequestStatus.Draft,
                RequestedByUserId = fixture.BootstrapAdministratorId,
                CreatedAtUtc = now
            });
            await dbContext.SaveChangesAsync();
            return true;
        });

        var blockedMutations = new (HttpMethod Method, string Path, object Body)[]
        {
            (
                HttpMethod.Put,
                $"/api/admin/request-types/{created.Id}/versions/{draft.Id}",
                VersionUpdate(draft, "Blocked in-use update")),
            (
                HttpMethod.Post,
                $"/api/admin/request-types/{created.Id}/versions/{draft.Id}/fields",
                Field("blocked_field", "shortText", 1) with
                {
                    VersionRowVersion = draft.RowVersion
                }),
            (
                HttpMethod.Post,
                $"/api/admin/request-types/{created.Id}/versions/{draft.Id}/publish",
                new RequestTypeConcurrencyRequest(draft.RowVersion))
        };

        foreach (var mutation in blockedMutations)
        {
            using var response = await SendWithAntiforgeryAsync(
                client,
                mutation.Method,
                mutation.Path,
                mutation.Body);
            await AssertProblemAsync(
                response,
                HttpStatusCode.Conflict,
                "administration.request_type_version_in_use");
        }
    }

    [Fact]
    public async Task Parent_and_field_row_versions_reject_stale_mutations()
    {
        using var client = CreateAdministratorClient();
        var created = await CreateRequestTypeAsync(client);
        var original = Assert.Single(created.Versions);

        using var updateResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/request-types/{created.Id}/versions/{original.Id}",
            VersionUpdate(original, "Updated once"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content
            .ReadFromJsonAsync<RequestTypeVersionResponse>();
        Assert.NotNull(updated);
        Assert.NotEqual(original.RowVersion, updated.RowVersion);

        using (var staleParentResponse = await SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Put,
                   $"/api/admin/request-types/{created.Id}/versions/{original.Id}",
                   VersionUpdate(original, "Stale parent update")))
        {
            await AssertProblemAsync(
                staleParentResponse,
                HttpStatusCode.Conflict,
                "administration.concurrency_conflict");
        }

        var withField = await AddFieldAsync(
            client,
            created.Id,
            updated,
            Field("concurrent_field", "shortText", 1));
        var originalField = Assert.Single(withField.Fields);
        using var fieldUpdateResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/request-types/{created.Id}/versions/{withField.Id}/fields/{originalField.Id}",
            FieldUpdate(originalField, withField.RowVersion, "Updated field"));
        Assert.Equal(HttpStatusCode.OK, fieldUpdateResponse.StatusCode);
        var fieldUpdatedVersion = await fieldUpdateResponse.Content
            .ReadFromJsonAsync<RequestTypeVersionResponse>();
        Assert.NotNull(fieldUpdatedVersion);
        var updatedField = Assert.Single(fieldUpdatedVersion.Fields);
        Assert.NotEqual(originalField.RowVersion, updatedField.RowVersion);

        using (var staleFieldResponse = await SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Put,
                   $"/api/admin/request-types/{created.Id}/versions/{withField.Id}/fields/{originalField.Id}",
                   FieldUpdate(
                       originalField,
                       fieldUpdatedVersion.RowVersion,
                       "Stale field update")))
        {
            await AssertProblemAsync(
                staleFieldResponse,
                HttpStatusCode.Conflict,
                "administration.concurrency_conflict");
        }

        using var staleVersionForFieldResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/request-types/{created.Id}/versions/{withField.Id}/fields/{updatedField.Id}",
            FieldUpdate(updatedField, withField.RowVersion, "Stale parent for field"));
        await AssertProblemAsync(
            staleVersionForFieldResponse,
            HttpStatusCode.Conflict,
            "administration.concurrency_conflict");
    }

    [Fact]
    public async Task Publish_racing_field_mutation_allows_exactly_one_change()
    {
        using var client = CreateAdministratorClient();
        var created = await CreateRequestTypeAsync(client);
        var version = Assert.Single(created.Versions);
        version = await AddFieldAsync(
            client,
            created.Id,
            version,
            Field("existing_field", "shortText", 1));
        var antiforgery = await GetAntiforgeryTokenAsync(client);
        using var publishRequest = CreateAntiforgeryRequest(
            HttpMethod.Post,
            $"/api/admin/request-types/{created.Id}/versions/{version.Id}/publish",
            new RequestTypeConcurrencyRequest(version.RowVersion),
            antiforgery);
        using var addFieldRequest = CreateAntiforgeryRequest(
            HttpMethod.Post,
            $"/api/admin/request-types/{created.Id}/versions/{version.Id}/fields",
            Field("racing_field", "shortText", 2) with
            {
                VersionRowVersion = version.RowVersion
            },
            antiforgery);

        var responses = await Task.WhenAll(
            client.SendAsync(publishRequest),
            client.SendAsync(addFieldRequest));
        try
        {
            Assert.Single(
                responses,
                response => response.StatusCode is HttpStatusCode.OK
                    or HttpStatusCode.Created);
            var conflict = Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.Conflict);
            await AssertProblemAsync(
                conflict,
                HttpStatusCode.Conflict,
                "administration.concurrency_conflict");
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        var persisted = await client.GetFromJsonAsync<RequestTypeVersionResponse>(
            $"/api/admin/request-types/{created.Id}/versions/{version.Id}");
        Assert.NotNull(persisted);
        Assert.True(
            persisted.Lifecycle == "published" && persisted.Fields.Count == 1
            || persisted.Lifecycle == "draft" && persisted.Fields.Count == 2);
    }

    [Fact]
    public async Task Administration_summary_counts_non_archived_request_type_roots()
    {
        using var client = CreateAdministratorClient();
        var before = await client.GetFromJsonAsync<AdministrationSummaryResponse>(
            "/api/admin/summary");
        Assert.NotNull(before);

        var created = await CreateRequestTypeAsync(client);
        var afterCreate = await client.GetFromJsonAsync<AdministrationSummaryResponse>(
            "/api/admin/summary");
        Assert.NotNull(afterCreate);
        Assert.Equal(
            before.ActiveRequestTypeCount + 1,
            afterCreate.ActiveRequestTypeCount);

        using var archiveResponse = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/request-types/{created.Id}/archive",
            new RequestTypeConcurrencyRequest(created.RowVersion));
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        var afterArchive = await client.GetFromJsonAsync<AdministrationSummaryResponse>(
            "/api/admin/summary");
        Assert.NotNull(afterArchive);
        Assert.Equal(before.ActiveRequestTypeCount, afterArchive.ActiveRequestTypeCount);
    }

    [Fact]
    public async Task Definition_conflicts_return_safe_problem_details_without_database_leakage()
    {
        using var client = CreateAdministratorClient();
        var payload = CreateRequestTypePayload();
        _ = await CreateRequestTypeAsync(client, payload);
        var duplicate = payload with
        {
            RequestPrefix = $"D{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            NavigationSlug = $"duplicate-{Guid.NewGuid():N}"[..20]
        };

        using var response = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/admin/request-types",
            duplicate);
        await AssertProblemAsync(
            response,
            HttpStatusCode.Conflict,
            "administration.duplicate_request_type_code");
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SqlException", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IndependentApprovalTests_", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SPSE26H", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RequestTypes", body, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateAdministratorClient() =>
        fixture.CreateClient(AdministrationApiFixture.BootstrapAdministratorAccount);

    private static CreateRequestTypeRequest CreateRequestTypePayload(
        string? nameEnglish = null,
        string? nameArabic = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new CreateRequestTypeRequest(
            $"request_type_{suffix}",
            nameEnglish ?? $"Request type {suffix}",
            nameArabic ?? $"نوع الطلب {suffix}",
            "English integration-test description.",
            "وصف عربي لاختبار التكامل.",
            $"R{suffix[..7]}".ToUpperInvariant(),
            $"request-{suffix}",
            100);
    }

    private static async Task<RequestTypeDetailResponse> CreateRequestTypeAsync(
        HttpClient client,
        CreateRequestTypeRequest? payload = null)
    {
        using var response = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/admin/request-types",
            payload ?? CreateRequestTypePayload());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content
            .ReadFromJsonAsync<RequestTypeDetailResponse>();
        return Assert.IsType<RequestTypeDetailResponse>(created);
    }

    private static async Task<RequestTypeVersionResponse> AddFieldAsync(
        HttpClient client,
        Guid requestTypeId,
        RequestTypeVersionResponse version,
        CreateRequestFieldDefinitionRequest request)
    {
        request = request with { VersionRowVersion = version.RowVersion };
        using var response = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/request-types/{requestTypeId}/versions/{version.Id}/fields",
            request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var updated = await response.Content
            .ReadFromJsonAsync<RequestTypeVersionResponse>();
        return Assert.IsType<RequestTypeVersionResponse>(updated);
    }

    private static async Task<RequestTypeVersionResponse> MutateVersionAsync<TBody>(
        HttpClient client,
        HttpMethod method,
        string path,
        TBody body,
        HttpStatusCode expectedStatus)
    {
        using var response = await SendWithAntiforgeryAsync(
            client,
            method,
            path,
            body);
        Assert.Equal(expectedStatus, response.StatusCode);
        var updated = await response.Content
            .ReadFromJsonAsync<RequestTypeVersionResponse>();
        return Assert.IsType<RequestTypeVersionResponse>(updated);
    }

    private static CreateRequestFieldDefinitionRequest Field(
        string key,
        string fieldType,
        int sortOrder,
        JsonElement? defaultValue = null,
        JsonElement? validationConfig = null,
        JsonElement? choiceConfig = null,
        string? documentMode = null) =>
        new(
            key,
            $"Field {key}",
            $"الحقل {key}",
            $"Help for {key}",
            $"مساعدة للحقل {key}",
            fieldType,
            true,
            defaultValue,
            validationConfig,
            choiceConfig,
            sortOrder,
            true,
            documentMode,
            null);

    private static UpdateRequestTypeVersionRequest VersionUpdate(
        RequestTypeVersionResponse version,
        string nameEnglish) =>
        new(
            nameEnglish,
            version.NameArabic,
            version.DescriptionEnglish,
            version.DescriptionArabic,
            version.RequestPrefix,
            version.NavigationSlug,
            version.NavigationOrder,
            version.RowVersion);

    private static UpdateRequestFieldDefinitionRequest FieldUpdate(
        RequestFieldDefinitionResponse field,
        string versionRowVersion,
        string labelEnglish) =>
        new(
            labelEnglish,
            field.LabelArabic,
            field.HelpTextEnglish,
            field.HelpTextArabic,
            field.FieldType,
            field.IsRequired,
            field.DefaultValue,
            field.ValidationConfig,
            field.ChoiceConfig,
            field.SortOrder,
            field.IsActive,
            field.DocumentMode,
            versionRowVersion,
            field.RowVersion);

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static void AssertValidRowVersion(string rowVersion)
    {
        var bytes = Convert.FromBase64String(rowVersion);
        Assert.Equal(8, bytes.Length);
    }

    private static async Task<HttpResponseMessage> SendWithAntiforgeryAsync<TBody>(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        TBody body)
    {
        var token = await GetAntiforgeryTokenAsync(client);
        using var request = CreateAntiforgeryRequest(
            method,
            requestUri,
            body,
            token);
        return await client.SendAsync(request);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/security/antiforgery-token");
        Assert.NotNull(token);
        return token.RequestToken;
    }

    private static HttpRequestMessage CreateAntiforgeryRequest<TBody>(
        HttpMethod method,
        string requestUri,
        TBody body,
        string token)
    {
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        return request;
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
