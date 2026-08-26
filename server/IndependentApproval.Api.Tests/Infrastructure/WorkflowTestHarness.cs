using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Contracts.Security;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Requests;

namespace IndependentApproval.Api.Tests.Infrastructure;

internal sealed class WorkflowTestHarness(AdministrationApiFixture fixture)
{
    public HttpClient CreateAdministratorClient() =>
        fixture.CreateClient(AdministrationApiFixture.BootstrapAdministratorAccount);

    public async Task<WorkflowDesignSeed> SeedDesignAsync(
        IReadOnlyList<WorkflowSeedField>? fields = null,
        IReadOnlyList<string>? roleNames = null)
    {
        fields ??=
        [
            new WorkflowSeedField("details", RequestFieldType.LongText, true),
            new WorkflowSeedField(
                "attachment",
                RequestFieldType.FileDocument,
                false,
                DocumentFieldMode.UploadOrCreate)
        ];
        roleNames ??= ["Starter", "Approver"];
        var now = DateTimeOffset.UtcNow;
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var requestTypeId = Guid.NewGuid();
        var requestTypeVersionId = Guid.NewGuid();
        var prefix = $"F{suffix[..7]}";
        var slug = $"form-{suffix.ToLowerInvariant()}";
        var roleSeeds = roleNames.Select((name, index) => new WorkflowSeedRole(
            Guid.NewGuid(),
            name,
            $"{NormalizeCode(name)}_{suffix}_{index}",
            name,
            $"دور {name}"))
            .ToArray();
        var fieldSeeds = fields.Select((field, index) => new WorkflowSeededField(
            Guid.NewGuid(),
            field.Key,
            field.FieldType,
            field.IsRequired,
            field.DocumentMode,
            index + 1))
            .ToArray();

        await fixture.QueryDatabaseAsync(async dbContext =>
        {
            dbContext.ApplicationRoles.AddRange(roleSeeds.Select(role =>
                new ApplicationRole
                {
                    Id = role.Id,
                    Code = role.Code,
                    NormalizedCode = role.Code,
                    NameEnglish = role.NameEnglish,
                    NameArabic = role.NameArabic,
                    DescriptionEnglish = $"Ordinary custom role for {role.LogicalName} tests.",
                    DescriptionArabic = $"دور عادي لاختبار {role.LogicalName}.",
                    IsActive = true,
                    IsArchived = false,
                    CreatedAtUtc = now,
                    CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                    CreatedByUserId = fixture.BootstrapAdministratorId
                }));
            dbContext.RequestTypes.Add(new RequestType
            {
                Id = requestTypeId,
                Code = $"FORM_{suffix}",
                NormalizedCode = $"FORM_{suffix}",
                CreatedAtUtc = now,
                CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                CreatedByUserId = fixture.BootstrapAdministratorId
            });
            dbContext.RequestTypePrefixReservations.Add(new RequestTypePrefixReservation
            {
                NormalizedPrefix = prefix,
                RequestTypeId = requestTypeId,
                ReservedAtUtc = now,
                ReservedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                ReservedByUserId = fixture.BootstrapAdministratorId
            });
            dbContext.RequestTypeSlugReservations.Add(new RequestTypeSlugReservation
            {
                NormalizedSlug = slug,
                RequestTypeId = requestTypeId,
                ReservedAtUtc = now,
                ReservedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                ReservedByUserId = fixture.BootstrapAdministratorId
            });
            dbContext.RequestTypeVersions.Add(new RequestTypeVersion
            {
                Id = requestTypeVersionId,
                RequestTypeId = requestTypeId,
                VersionNumber = 1,
                NameEnglish = $"Form {suffix}",
                NameArabic = $"نموذج {suffix}",
                DescriptionEnglish = "Published form used by workflow integration tests.",
                DescriptionArabic = "نموذج منشور لاختبارات سير العمل.",
                RequestPrefix = prefix,
                NavigationSlug = slug,
                NavigationOrder = 1,
                Lifecycle = RequestTypeVersionLifecycle.Published,
                CreatedAtUtc = now,
                CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                CreatedByUserId = fixture.BootstrapAdministratorId,
                PublishedAtUtc = now,
                PublishedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                PublishedByUserId = fixture.BootstrapAdministratorId
            });
            dbContext.RequestFieldDefinitions.AddRange(fieldSeeds.Select(field =>
                CreateFieldDefinition(
                    requestTypeVersionId,
                    field,
                    now)));
            await dbContext.SaveChangesAsync();
            return true;
        });

        return new WorkflowDesignSeed(
            requestTypeId,
            $"FORM_{suffix}",
            requestTypeVersionId,
            1,
            prefix,
            slug,
            fieldSeeds,
            roleSeeds);
    }

    public async Task<WorkflowRequestTypeVersionSeed> SeedAdditionalPublishedFormVersionAsync(
        WorkflowDesignSeed seed,
        IReadOnlyList<WorkflowSeedField> fields)
    {
        var now = DateTimeOffset.UtcNow;
        var versionId = Guid.NewGuid();
        var fieldSeeds = fields.Select((field, index) => new WorkflowSeededField(
            Guid.NewGuid(),
            field.Key,
            field.FieldType,
            field.IsRequired,
            field.DocumentMode,
            index + 1))
            .ToArray();
        var nextVersion = await fixture.QueryDatabaseAsync(async dbContext =>
        {
            var versionNumber = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .MaxAsync(
                    dbContext.RequestTypeVersions.Where(version =>
                        version.RequestTypeId == seed.RequestTypeId),
                    version => version.VersionNumber) + 1;
            dbContext.RequestTypeVersions.Add(new RequestTypeVersion
            {
                Id = versionId,
                RequestTypeId = seed.RequestTypeId,
                VersionNumber = versionNumber,
                NameEnglish = $"{seed.RequestTypeCode} version {versionNumber}",
                NameArabic = $"{seed.RequestTypeCode} الإصدار {versionNumber}",
                DescriptionEnglish = "Additional published form version.",
                DescriptionArabic = "إصدار إضافي منشور للنموذج.",
                RequestPrefix = seed.RequestPrefix,
                NavigationSlug = seed.RequestNavigationSlug,
                NavigationOrder = versionNumber,
                Lifecycle = RequestTypeVersionLifecycle.Published,
                CreatedAtUtc = now,
                CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                CreatedByUserId = fixture.BootstrapAdministratorId,
                PublishedAtUtc = now,
                PublishedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
                PublishedByUserId = fixture.BootstrapAdministratorId
            });
            dbContext.RequestFieldDefinitions.AddRange(fieldSeeds.Select(field =>
                CreateFieldDefinition(versionId, field, now)));
            await dbContext.SaveChangesAsync();
            return versionNumber;
        });

        return new WorkflowRequestTypeVersionSeed(
            versionId,
            nextVersion,
            fieldSeeds);
    }

    public CreateWorkflowRequest CreateWorkflowPayload(
        WorkflowDesignSeed seed,
        string? slug = null,
        string? code = null,
        Guid? requestTypeVersionId = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new CreateWorkflowRequest(
            code ?? $"workflow_{suffix}",
            requestTypeVersionId ?? seed.RequestTypeVersionId,
            $"Workflow {suffix}",
            $"سير العمل {suffix}",
            "English workflow integration-test description.",
            "وصف عربي لاختبار سير العمل.",
            $"Workflow {suffix}",
            $"سير العمل {suffix}",
            slug ?? $"workflow-{suffix}",
            100);
    }

    public async Task<CreatedWorkflow> CreateWorkflowAsync(
        HttpClient client,
        WorkflowDesignSeed seed,
        CreateWorkflowRequest? payload = null)
    {
        payload ??= CreateWorkflowPayload(seed);
        using var response = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            "/api/admin/workflows",
            payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<WorkflowDetailResponse>();
        Assert.NotNull(detail);
        return new CreatedWorkflow(detail, payload);
    }

    public async Task<WorkflowVersionResponse> ReplaceStarterRolesAsync(
        HttpClient client,
        Guid workflowId,
        WorkflowVersionResponse version,
        params Guid[] roleIds) =>
        await MutateVersionAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/workflows/{workflowId}/versions/{version.Id}/starter-roles",
            new ReplaceWorkflowStarterRolesRequest(roleIds, version.RowVersion),
            HttpStatusCode.OK);

    public async Task<WorkflowVersionResponse> AddStepAsync(
        HttpClient client,
        Guid workflowId,
        WorkflowVersionResponse version,
        string key,
        IReadOnlyList<Guid> roleIds,
        int sortOrder,
        bool isStartStep = false,
        bool isActive = true,
        bool canEditRequestData = false,
        bool canOpenDocuments = true,
        bool canEditDocuments = false,
        bool canForwardDocuments = false,
        string commentPolicy = "optional") =>
        await MutateVersionAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{workflowId}/versions/{version.Id}/steps",
            new CreateWorkflowStepRequest(
                key,
                $"Step {key}",
                $"خطوة {key}",
                sortOrder,
                sortOrder * 200,
                sortOrder * 100,
                isStartStep,
                isActive,
                canEditRequestData,
                canOpenDocuments,
                canEditDocuments,
                canForwardDocuments,
                commentPolicy,
                roleIds,
                version.RowVersion),
            HttpStatusCode.Created);

    public async Task<WorkflowVersionResponse> ReplacePermissionsAsync(
        HttpClient client,
        Guid workflowId,
        WorkflowVersionResponse version,
        string stepKey,
        IReadOnlyList<WorkflowStepFieldPermissionInput> permissions)
    {
        var step = version.Steps.Single(candidate => candidate.Key == stepKey);
        return await MutateVersionAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/workflows/{workflowId}/versions/{version.Id}" +
            $"/steps/{step.Id}/field-permissions",
            new ReplaceWorkflowStepFieldPermissionsRequest(
                permissions,
                version.RowVersion,
                step.RowVersion),
            HttpStatusCode.OK);
    }

    public async Task<WorkflowVersionResponse> AddTransitionAsync(
        HttpClient client,
        Guid workflowId,
        WorkflowVersionResponse version,
        string key,
        Guid sourceStepId,
        Guid? targetStepId,
        string actionType,
        string resultingStatus,
        int sortOrder,
        bool requiresComment = false,
        string? terminalOutcome = null,
        bool isActive = true) =>
        await MutateVersionAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{workflowId}/versions/{version.Id}/transitions",
            new CreateWorkflowTransitionRequest(
                key,
                sourceStepId,
                targetStepId,
                $"Action {key}",
                $"إجراء {key}",
                actionType,
                resultingStatus,
                requiresComment,
                terminalOutcome,
                sortOrder,
                isActive,
                version.RowVersion),
            HttpStatusCode.Created);

    public async Task<WorkflowValidationResponse> ValidateAsync(
        HttpClient client,
        Guid workflowId,
        WorkflowVersionResponse version)
    {
        using var response = await SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{workflowId}/versions/{version.Id}/validate",
            new ValidateWorkflowRequest(version.RowVersion));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var validation = await response.Content
            .ReadFromJsonAsync<WorkflowValidationResponse>();
        return Assert.IsType<WorkflowValidationResponse>(validation);
    }

    public async Task<WorkflowVersionResponse> PublishAsync(
        HttpClient client,
        Guid workflowId,
        WorkflowVersionResponse version) =>
        await MutateVersionAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{workflowId}/versions/{version.Id}/publish",
            new WorkflowConcurrencyRequest(version.RowVersion),
            HttpStatusCode.OK);

    public async Task<WorkflowVersionResponse> BuildSimpleValidGraphAsync(
        HttpClient client,
        Guid workflowId,
        WorkflowDesignSeed seed,
        WorkflowVersionResponse version)
    {
        version = await ReplaceStarterRolesAsync(
            client,
            workflowId,
            version,
            seed.Roles[0].Id);
        version = await AddStepAsync(
            client,
            workflowId,
            version,
            "start",
            [seed.Roles[0].Id],
            1,
            isStartStep: true,
            canEditRequestData: true,
            canOpenDocuments: true,
            canEditDocuments: true,
            commentPolicy: "optional");
        version = await AddStepAsync(
            client,
            workflowId,
            version,
            "approval",
            [seed.Roles[1].Id],
            2,
            canOpenDocuments: true,
            commentPolicy: "required");
        version = await ReplacePermissionsAsync(
            client,
            workflowId,
            version,
            "start",
            PermissionsForStep(
                seed.Fields,
                editable: true,
                canEditDocuments: true));
        version = await ReplacePermissionsAsync(
            client,
            workflowId,
            version,
            "approval",
            PermissionsForStep(
                seed.Fields,
                editable: false,
                canEditDocuments: false));
        var start = version.Steps.Single(step => step.Key == "start");
        var approval = version.Steps.Single(step => step.Key == "approval");
        version = await AddTransitionAsync(
            client,
            workflowId,
            version,
            "submit",
            start.Id,
            approval.Id,
            "submit",
            "submitted",
            1);
        version = await AddTransitionAsync(
            client,
            workflowId,
            version,
            "approve",
            approval.Id,
            null,
            "approve",
            "approved",
            1,
            requiresComment: true,
            terminalOutcome: "approved");
        return version;
    }

    public async Task<WorkflowVersionResponse> MutateVersionAsync<TBody>(
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
        var version = await response.Content
            .ReadFromJsonAsync<WorkflowVersionResponse>();
        return Assert.IsType<WorkflowVersionResponse>(version);
    }

    public static IReadOnlyList<WorkflowStepFieldPermissionInput> PermissionsForStep(
        IReadOnlyList<WorkflowSeededField> fields,
        bool editable,
        bool canEditDocuments,
        bool canForwardDocuments = false) =>
        fields.Select(field =>
        {
            var isDocument = field.FieldType is RequestFieldType.FileDocument
                or RequestFieldType.RichDocument;
            var access = editable
                ? field.IsRequired ? "editableRequired" : "editable"
                : "readOnly";
            var documentAccess = isDocument
                ? canEditDocuments ? "edit" : "view"
                : null;
            return new WorkflowStepFieldPermissionInput(
                field.Id,
                access,
                documentAccess,
                isDocument && canForwardDocuments,
                null);
        }).ToArray();

    public static async Task<HttpResponseMessage> SendWithAntiforgeryAsync<TBody>(
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

    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/security/antiforgery-token");
        Assert.NotNull(token);
        return token.RequestToken;
    }

    public static HttpRequestMessage CreateAntiforgeryRequest<TBody>(
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

    public static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(
            expectedCode,
            problem.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain("SqlException", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IndependentApprovalTests_", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SPSE26H", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WorkflowDefinitions", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("WorkflowSlugReservations", body, StringComparison.OrdinalIgnoreCase);
    }

    private RequestFieldDefinition CreateFieldDefinition(
        Guid versionId,
        WorkflowSeededField field,
        DateTimeOffset timestamp) =>
        new()
        {
            Id = field.Id,
            RequestTypeVersionId = versionId,
            Key = field.Key,
            NormalizedKey = field.Key.ToUpperInvariant(),
            LabelEnglish = $"Field {field.Key}",
            LabelArabic = $"حقل {field.Key}",
            FieldType = field.FieldType,
            IsRequired = field.IsRequired,
            SortOrder = field.SortOrder,
            IsActive = true,
            DocumentMode = field.DocumentMode,
            CreatedAtUtc = timestamp,
            CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
            CreatedByUserId = fixture.BootstrapAdministratorId
        };

    private static string NormalizeCode(string name) =>
        new(name
            .Where(character => char.IsAsciiLetterOrDigit(character))
            .Select(char.ToUpperInvariant)
            .ToArray());
}

internal sealed record WorkflowSeedField(
    string Key,
    RequestFieldType FieldType,
    bool IsRequired,
    DocumentFieldMode? DocumentMode = null);

internal sealed record WorkflowSeededField(
    Guid Id,
    string Key,
    RequestFieldType FieldType,
    bool IsRequired,
    DocumentFieldMode? DocumentMode,
    int SortOrder);

internal sealed record WorkflowSeedRole(
    Guid Id,
    string LogicalName,
    string Code,
    string NameEnglish,
    string NameArabic);

internal sealed record WorkflowDesignSeed(
    Guid RequestTypeId,
    string RequestTypeCode,
    Guid RequestTypeVersionId,
    int RequestTypeVersionNumber,
    string RequestPrefix,
    string RequestNavigationSlug,
    IReadOnlyList<WorkflowSeededField> Fields,
    IReadOnlyList<WorkflowSeedRole> Roles)
{
    public WorkflowSeedRole Role(string logicalName) => Roles.Single(role =>
        string.Equals(role.LogicalName, logicalName, StringComparison.Ordinal));

    public WorkflowSeededField Field(string key) => Fields.Single(field =>
        string.Equals(field.Key, key, StringComparison.Ordinal));
}

internal sealed record WorkflowRequestTypeVersionSeed(
    Guid Id,
    int VersionNumber,
    IReadOnlyList<WorkflowSeededField> Fields);

internal sealed record CreatedWorkflow(
    WorkflowDetailResponse Detail,
    CreateWorkflowRequest Payload)
{
    public WorkflowVersionResponse Version => Assert.Single(Detail.Versions);
}
