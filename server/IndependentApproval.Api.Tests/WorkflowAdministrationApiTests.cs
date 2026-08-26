using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Contracts.Common;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Tests.Infrastructure;

namespace IndependentApproval.Api.Tests;

public sealed class WorkflowAdministrationApiTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Workflow_apis_enforce_the_system_administrator_policy()
    {
        var harness = new WorkflowTestHarness(fixture);
        using var administrator = harness.CreateAdministratorClient();
        using var administratorResponse = await administrator.GetAsync(
            "/api/admin/workflows/options");
        Assert.Equal(HttpStatusCode.OK, administratorResponse.StatusCode);

        using var applicationUser = fixture.CreateClient(
            AdministrationApiFixture.ApplicationUserAccount);
        using var listResponse = await applicationUser.GetAsync(
            "/api/admin/workflows");
        await WorkflowTestHarness.AssertProblemAsync(
            listResponse,
            HttpStatusCode.Forbidden,
            "authorization.forbidden");

        using var createResponse = await WorkflowTestHarness.SendWithAntiforgeryAsync(
            applicationUser,
            HttpMethod.Post,
            "/api/admin/workflows",
            new { });
        await WorkflowTestHarness.AssertProblemAsync(
            createResponse,
            HttpStatusCode.Forbidden,
            "authorization.forbidden");
    }

    [Fact]
    public async Task Every_unsafe_workflow_endpoint_requires_antiforgery()
    {
        var harness = new WorkflowTestHarness(fixture);
        using var client = harness.CreateAdministratorClient();
        var workflowId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var transitionId = Guid.NewGuid();
        var endpoints = new (HttpMethod Method, string Path)[]
        {
            (HttpMethod.Post, "/api/admin/workflows"),
            (HttpMethod.Put, $"/api/admin/workflows/{workflowId}/versions/{versionId}"),
            (HttpMethod.Put, $"/api/admin/workflows/{workflowId}/versions/{versionId}/starter-roles"),
            (HttpMethod.Post, $"/api/admin/workflows/{workflowId}/versions/{versionId}/steps"),
            (HttpMethod.Put, $"/api/admin/workflows/{workflowId}/versions/{versionId}/steps/{stepId}"),
            (HttpMethod.Delete, $"/api/admin/workflows/{workflowId}/versions/{versionId}/steps/{stepId}"),
            (HttpMethod.Put, $"/api/admin/workflows/{workflowId}/versions/{versionId}/steps/{stepId}/roles"),
            (HttpMethod.Put, $"/api/admin/workflows/{workflowId}/versions/{versionId}/steps/{stepId}/field-permissions"),
            (HttpMethod.Post, $"/api/admin/workflows/{workflowId}/versions/{versionId}/transitions"),
            (HttpMethod.Put, $"/api/admin/workflows/{workflowId}/versions/{versionId}/transitions/{transitionId}"),
            (HttpMethod.Delete, $"/api/admin/workflows/{workflowId}/versions/{versionId}/transitions/{transitionId}"),
            (HttpMethod.Post, $"/api/admin/workflows/{workflowId}/versions/{versionId}/validate"),
            (HttpMethod.Post, $"/api/admin/workflows/{workflowId}/versions/{versionId}/publish"),
            (HttpMethod.Post, $"/api/admin/workflows/{workflowId}/versions/{versionId}/clone"),
            (HttpMethod.Post, $"/api/admin/workflows/{workflowId}/versions/{versionId}/archive"),
            (HttpMethod.Post, $"/api/admin/workflows/{workflowId}/archive")
        };

        foreach (var endpoint in endpoints)
        {
            using var request = new HttpRequestMessage(endpoint.Method, endpoint.Path)
            {
                Content = JsonContent.Create(new { })
            };
            using var response = await client.SendAsync(request);
            await WorkflowTestHarness.AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "security.antiforgery_validation_failed");
        }
    }

    [Fact]
    public async Task Create_list_detail_and_options_preserve_bilingual_definition_catalogues()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var payload = harness.CreateWorkflowPayload(seed) with
        {
            NameEnglish = "Travel approval",
            NameArabic = "اعتماد السفر",
            NavigationLabelEnglish = "Travel approvals",
            NavigationLabelArabic = "اعتمادات السفر"
        };
        var created = await harness.CreateWorkflowAsync(client, seed, payload);
        var draft = created.Version;

        Assert.Equal(payload.Code!.Trim().ToUpperInvariant(), created.Detail.Code);
        Assert.False(created.Detail.IsArchived);
        Assert.Equal("draft", draft.Lifecycle);
        Assert.Equal("Travel approval", draft.NameEnglish);
        Assert.Equal("اعتماد السفر", draft.NameArabic);
        Assert.Equal("Travel approvals", draft.NavigationLabelEnglish);
        Assert.Equal("اعتمادات السفر", draft.NavigationLabelArabic);
        Assert.Equal(seed.RequestTypeId, draft.RequestTypeId);
        Assert.Equal(seed.RequestTypeVersionId, draft.RequestTypeVersionId);
        Assert.Equal(seed.RequestTypeCode, draft.RequestTypeCode);
        Assert.Equal(1, draft.RequestTypeVersionNumber);
        AssertValidRowVersion(created.Detail.RowVersion);
        AssertValidRowVersion(draft.RowVersion);

        var list = await client.GetFromJsonAsync<PagedResponse<WorkflowListItemResponse>>(
            $"/api/admin/workflows?search={Uri.EscapeDataString(created.Detail.Code)}" +
            "&page=1&pageSize=25");
        Assert.NotNull(list);
        var item = Assert.Single(list.Items, item => item.Id == created.Detail.Id);
        Assert.Equal(draft.Id, item.DraftVersionId);
        Assert.Equal("اعتماد السفر", item.DisplayVersion?.NameArabic);

        var detail = await client.GetFromJsonAsync<WorkflowDetailResponse>(
            $"/api/admin/workflows/{created.Detail.Id}");
        Assert.NotNull(detail);
        Assert.Equal("Travel approval", Assert.Single(detail.Versions).NameEnglish);
        var version = await client.GetFromJsonAsync<WorkflowVersionResponse>(
            $"/api/admin/workflows/{created.Detail.Id}/versions/{draft.Id}");
        Assert.NotNull(version);
        Assert.Equal("اعتمادات السفر", version.NavigationLabelArabic);

        var options = await client.GetFromJsonAsync<WorkflowOptionsResponse>(
            "/api/admin/workflows/options");
        Assert.NotNull(options);
        Assert.All(seed.Roles, role => Assert.Contains(
            options.Roles,
            option => option.Id == role.Id
                      && option.NameEnglish == role.NameEnglish
                      && option.NameArabic == role.NameArabic));
        var form = Assert.Single(
            options.RequestTypeVersions,
            option => option.RequestTypeVersionId == seed.RequestTypeVersionId);
        Assert.Equal(seed.Fields.Count, form.Fields.Count);
        Assert.All(seed.Fields, field => Assert.Contains(
            form.Fields,
            option => option.Id == field.Id
                      && option.Key == field.Key));
        Assert.Equal(
            new[]
            {
                "submit", "approve", "reject", "putOnHold", "resume",
                "requestMoreInformation", "return", "forward", "complete"
            },
            options.ActionTypes.Select(option => option.Value).ToArray());
        Assert.Equal(
            new[] { "hidden", "readOnly", "editable", "editableRequired" },
            options.FieldAccessModes.Select(option => option.Value).ToArray());
        Assert.Equal(
            new[] { "hidden", "view", "edit" },
            options.DocumentAccessModes.Select(option => option.Value).ToArray());

        Assert.Contains(
            options.SystemFields,
            field => field.Key == "requestNumber"
                     && field.Access == "readOnly"
                     && !field.CanSelectForForwarding);
        Assert.Contains(
            options.SystemFields,
            field => field.Key == "status" && field.Access == "readOnly");
        Assert.Contains(
            options.SystemFields,
            field => field.Key == "requestedBy" && field.Access == "readOnly");
        foreach (var internalKey in new[]
                 {
                     "id", "requestTypeId", "requestTypeVersionId",
                     "workflowDefinitionId", "workflowVersionId",
                     "currentWorkflowStepId", "rowVersion"
                 })
        {
            Assert.Contains(
                options.SystemFields,
                field => field.Key == internalKey
                         && field.Access == "hidden"
                         && field.DocumentAccess is null
                         && !field.CanSelectForForwarding);
        }
        Assert.All(
            options.SystemFields.Where(field =>
                field.Key.EndsWith("AtUtc", StringComparison.Ordinal)),
            field => Assert.Equal("readOnly", field.Access));
    }

    [Fact]
    public async Task Designer_round_trips_all_exact_action_types()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var created = await harness.CreateWorkflowAsync(client, seed);
        var version = created.Version;
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "source",
            [seed.Roles[0].Id],
            1,
            isStartStep: true,
            canEditRequestData: true,
            canEditDocuments: true,
            canForwardDocuments: true);
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "target",
            [seed.Roles[1].Id],
            2,
            canEditRequestData: true,
            canEditDocuments: true,
            canForwardDocuments: true,
            commentPolicy: "required");
        var source = version.Steps.Single(step => step.Key == "source");
        var target = version.Steps.Single(step => step.Key == "target");
        var transitions = new[]
        {
            new ActionCase("submit_action", "submit", "submitted", false, null),
            new ActionCase("approve_action", "approve", "approved", true, "approved"),
            new ActionCase("reject_action", "reject", "rejected", true, "rejected"),
            new ActionCase("hold_action", "putOnHold", "onHold", false, null),
            new ActionCase("resume_action", "resume", "inProgress", false, null),
            new ActionCase("info_action", "requestMoreInformation", "moreInformationRequired", true, null),
            new ActionCase("return_action", "return", "inProgress", true, null),
            new ActionCase("forward_action", "forward", "inProgress", false, null),
            new ActionCase("complete_action", "complete", "completed", false, "completed")
        };

        for (var index = 0; index < transitions.Length; index++)
        {
            var transition = transitions[index];
            version = await harness.AddTransitionAsync(
                client,
                created.Detail.Id,
                version,
                transition.Key,
                source.Id,
                transition.TerminalOutcome is null ? target.Id : null,
                transition.ActionType,
                transition.ResultingStatus,
                index + 1,
                transition.RequiresComment,
                transition.TerminalOutcome);
        }

        Assert.Equal(9, version.Transitions.Count);
        Assert.Equal(
            transitions.Select(transition => transition.ActionType),
            version.Transitions
                .OrderBy(transition => transition.SortOrder)
                .Select(transition => transition.ActionType));
        Assert.All(
            transitions,
            expected => Assert.Contains(
                version.Transitions,
                actual => actual.Key == expected.Key
                          && actual.ResultingStatus == expected.ResultingStatus
                          && actual.RequiresComment == expected.RequiresComment
                          && actual.TerminalOutcome == expected.TerminalOutcome));
    }

    [Fact]
    public async Task Options_and_permissions_cover_every_field_type_and_document_mode()
    {
        var fieldSeeds = new[]
        {
            new WorkflowSeedField("short_text", RequestFieldType.ShortText, true),
            new WorkflowSeedField("long_text", RequestFieldType.LongText, false),
            new WorkflowSeedField("integer", RequestFieldType.Integer, true),
            new WorkflowSeedField("decimal", RequestFieldType.Decimal, false),
            new WorkflowSeedField("date", RequestFieldType.Date, true),
            new WorkflowSeedField("date_time", RequestFieldType.DateTime, false),
            new WorkflowSeedField("yes_no", RequestFieldType.YesNo, true),
            new WorkflowSeedField("single_choice", RequestFieldType.SingleChoice, false),
            new WorkflowSeedField("multiple_choice", RequestFieldType.MultipleChoice, true),
            new WorkflowSeedField("directory_user", RequestFieldType.ActiveDirectoryUser, false),
            new WorkflowSeedField("application_role", RequestFieldType.ApplicationRole, true),
            new WorkflowSeedField(
                "uploaded_file",
                RequestFieldType.FileDocument,
                false,
                DocumentFieldMode.UploadOnly),
            new WorkflowSeedField(
                "editor_document",
                RequestFieldType.RichDocument,
                true,
                DocumentFieldMode.CreateInEditorOnly),
            new WorkflowSeedField(
                "upload_or_create",
                RequestFieldType.FileDocument,
                false,
                DocumentFieldMode.UploadOrCreate)
        };
        var expectedFieldTypes = new[]
        {
            "shortText", "longText", "integer", "decimal", "date", "dateTime",
            "yesNo", "singleChoice", "multipleChoice", "activeDirectoryUser",
            "applicationRole", "fileDocument", "richDocument", "fileDocument"
        };
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync(fieldSeeds);
        using var client = harness.CreateAdministratorClient();

        var options = await client.GetFromJsonAsync<WorkflowOptionsResponse>(
            "/api/admin/workflows/options");
        Assert.NotNull(options);
        var form = Assert.Single(
            options.RequestTypeVersions,
            candidate => candidate.RequestTypeVersionId == seed.RequestTypeVersionId);
        Assert.Equal(expectedFieldTypes, form.Fields.Select(field => field.FieldType));
        Assert.Equal(
            new[] { "uploadOnly", "createInEditorOnly", "uploadOrCreate" },
            form.Fields
                .Where(field => field.DocumentMode is not null)
                .Select(field => field.DocumentMode));
        Assert.True(RequestSystemFieldKeys.All.SetEquals(
            options.SystemFields.Select(field => field.Key)));

        var created = await harness.CreateWorkflowAsync(client, seed);
        var version = await harness.ReplaceStarterRolesAsync(
            client,
            created.Detail.Id,
            created.Version,
            seed.Roles[0].Id);
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "matrix",
            [seed.Roles[0].Id],
            1,
            isStartStep: true,
            canEditRequestData: true,
            canOpenDocuments: true,
            canEditDocuments: true,
            canForwardDocuments: true);
        var accessModes = new[] { "hidden", "readOnly", "editable", "editableRequired" };
        var permissions = seed.Fields.Select((field, index) =>
        {
            if (field.DocumentMode == DocumentFieldMode.UploadOnly)
            {
                return new WorkflowStepFieldPermissionInput(
                    field.Id, "hidden", "hidden", false, null);
            }

            if (field.DocumentMode == DocumentFieldMode.CreateInEditorOnly)
            {
                return new WorkflowStepFieldPermissionInput(
                    field.Id, "readOnly", "view", true, null);
            }

            if (field.DocumentMode == DocumentFieldMode.UploadOrCreate)
            {
                return new WorkflowStepFieldPermissionInput(
                    field.Id, "editableRequired", "edit", true, null);
            }

            return new WorkflowStepFieldPermissionInput(
                field.Id,
                accessModes[index % accessModes.Length],
                null,
                false,
                null);
        }).ToArray();
        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "matrix",
            permissions);
        var step = Assert.Single(version.Steps);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "complete",
            step.Id,
            null,
            "complete",
            "completed",
            1,
            terminalOutcome: "completed");

        var validation = await harness.ValidateAsync(
            client,
            created.Detail.Id,
            version);
        Assert.True(
            validation.IsValid,
            string.Join("; ", validation.Issues.Select(issue => issue.Code)));
        var persistedPermissions = Assert.Single(version.Steps).FieldPermissions;
        Assert.Equal(seed.Fields.Count, persistedPermissions.Count);
        Assert.All(permissions, expected => Assert.Contains(
            persistedPermissions,
            actual => actual.RequestFieldDefinitionId == expected.RequestFieldDefinitionId
                      && actual.Access == expected.Access
                      && actual.DocumentAccess == expected.DocumentAccess
                      && actual.CanSelectForForwarding == expected.CanSelectForForwarding));
    }

    [Fact]
    public async Task Duplicate_code_and_never_deleted_global_slug_ownership_return_safe_conflicts()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var firstPayload = harness.CreateWorkflowPayload(seed);
        var first = await harness.CreateWorkflowAsync(client, seed, firstPayload);

        using (var duplicateCodeResponse = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Post,
                   "/api/admin/workflows",
                   harness.CreateWorkflowPayload(seed) with
                   {
                       Code = firstPayload.Code
                   }))
        {
            await WorkflowTestHarness.AssertProblemAsync(
                duplicateCodeResponse,
                HttpStatusCode.Conflict,
                "administration.duplicate_workflow_code");
        }

        using var archiveResponse = await WorkflowTestHarness.SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{first.Detail.Id}/archive",
            new WorkflowConcurrencyRequest(first.Detail.RowVersion));
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        using (var reusedSlugResponse = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Post,
                   "/api/admin/workflows",
                   harness.CreateWorkflowPayload(seed, slug: firstPayload.NavigationSlug)))
        {
            await WorkflowTestHarness.AssertProblemAsync(
                reusedSlugResponse,
                HttpStatusCode.Conflict,
                "administration.duplicate_workflow_navigation_slug");
        }

        var contestedSlug = $"contested-{Guid.NewGuid():N}";
        var token = await WorkflowTestHarness.GetAntiforgeryTokenAsync(client);
        using var firstRequest = WorkflowTestHarness.CreateAntiforgeryRequest(
            HttpMethod.Post,
            "/api/admin/workflows",
            harness.CreateWorkflowPayload(seed, slug: contestedSlug),
            token);
        using var secondRequest = WorkflowTestHarness.CreateAntiforgeryRequest(
            HttpMethod.Post,
            "/api/admin/workflows",
            harness.CreateWorkflowPayload(seed, slug: contestedSlug),
            token);
        var responses = await Task.WhenAll(
            client.SendAsync(firstRequest),
            client.SendAsync(secondRequest));
        try
        {
            Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.Created);
            var conflict = Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.Conflict);
            var conflictBody = await conflict.Content.ReadAsStringAsync();
            using var problem = JsonDocument.Parse(conflictBody);
            Assert.Contains(
                problem.RootElement.GetProperty("code").GetString(),
                new[]
                {
                    "administration.duplicate_workflow_navigation_slug",
                    "administration.workflow_definition_conflict"
                });
            AssertSafeProblemText(conflictBody);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        var reservationOwners = await fixture.QueryDatabaseAsync(async dbContext =>
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .CountAsync(
                    dbContext.WorkflowSlugReservations.Where(reservation =>
                        reservation.NormalizedSlug == contestedSlug)));
        Assert.Equal(1, reservationOwners);
    }

    [Fact]
    public async Task Missing_workflow_concurrency_tokens_are_validation_errors()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var created = await harness.CreateWorkflowAsync(client, seed);
        var version = created.Version;
        var mutations = new (HttpMethod Method, string Path, object Body)[]
        {
            (
                HttpMethod.Put,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}",
                new UpdateWorkflowVersionRequest(
                    version.RequestTypeVersionId,
                    version.NameEnglish,
                    version.NameArabic,
                    version.DescriptionEnglish,
                    version.DescriptionArabic,
                    version.NavigationLabelEnglish,
                    version.NavigationLabelArabic,
                    version.NavigationSlug,
                    version.NavigationOrder,
                    null)),
            (
                HttpMethod.Put,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}/starter-roles",
                new ReplaceWorkflowStarterRolesRequest([seed.Roles[0].Id], null)),
            (
                HttpMethod.Post,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}/validate",
                new ValidateWorkflowRequest(null)),
            (
                HttpMethod.Post,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}/publish",
                new WorkflowConcurrencyRequest(null))
        };

        foreach (var mutation in mutations)
        {
            using var response = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                client,
                mutation.Method,
                mutation.Path,
                mutation.Body);
            await WorkflowTestHarness.AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "administration.validation_failed");
        }
    }

    private static void AssertValidRowVersion(string rowVersion)
    {
        var bytes = Convert.FromBase64String(rowVersion);
        Assert.Equal(8, bytes.Length);
    }

    private static void AssertSafeProblemText(string body)
    {
        Assert.DoesNotContain("SqlException", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IndependentApprovalTests_", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SPSE26H", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WorkflowDefinitions", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WorkflowSlugReservations", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ActionCase(
        string Key,
        string ActionType,
        string ResultingStatus,
        bool RequiresComment,
        string? TerminalOutcome);
}
