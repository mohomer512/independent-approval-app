using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IndependentApproval.Api.Contracts.Administration.RequestTypes;
using IndependentApproval.Api.Contracts.Security;
using IndependentApproval.Api.Tests.Infrastructure;

namespace IndependentApproval.Api.Tests;

public sealed class RequestTypeCanonicalValidationTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Field_api_rejects_noncanonical_date_choice_and_extension_values()
    {
        using var client = fixture.CreateClient(
            AdministrationApiFixture.BootstrapAdministratorAccount);
        var created = await CreateRequestTypeAsync(client);
        var draft = Assert.Single(created.Versions);
        var invalidCases = new[]
        {
            new InvalidFieldCase(
                DateTimeField(
                    "offsetless_default",
                    1,
                    defaultValue: Json("\"2026-08-24T10:15:30\"")),
                "defaultValue",
                "with an offset"),
            new InvalidFieldCase(
                DateTimeField(
                    "offsetless_minimum",
                    2,
                    validationConfig: Json(
                        "{\"minimum\":\"2026-08-24T10:15:30\"}")),
                "validationConfig",
                "with an offset"),
            new InvalidFieldCase(
                DateTimeField(
                    "offsetless_maximum",
                    3,
                    validationConfig: Json(
                        "{\"maximum\":\"2026-08-24T10:15:30\"}")),
                "validationConfig",
                "with an offset"),
            new InvalidFieldCase(
                ChoiceField(
                    "spaced_choice_value",
                    4,
                    "{\"value\":\" open\",\"labelEnglish\":\"Open\",\"labelArabic\":\"مفتوح\",\"sortOrder\":1,\"isActive\":true}"),
                "choiceConfig",
                "surrounding whitespace"),
            new InvalidFieldCase(
                ChoiceField(
                    "spaced_choice_english_label",
                    5,
                    "{\"value\":\"open\",\"labelEnglish\":\"Open \",\"labelArabic\":\"مفتوح\",\"sortOrder\":1,\"isActive\":true}"),
                "choiceConfig",
                "surrounding whitespace"),
            new InvalidFieldCase(
                ChoiceField(
                    "spaced_choice_arabic_label",
                    6,
                    "{\"value\":\"open\",\"labelEnglish\":\"Open\",\"labelArabic\":\" مفتوح\",\"sortOrder\":1,\"isActive\":true}"),
                "choiceConfig",
                "surrounding whitespace"),
            new InvalidFieldCase(
                DocumentField("spaced_extension", 7, " .pdf"),
                "validationConfig",
                "without surrounding whitespace"),
            new InvalidFieldCase(
                DocumentField("uppercase_extension", 8, ".PDF"),
                "validationConfig",
                "lowercase value")
        };

        foreach (var invalidCase in invalidCases)
        {
            var request = invalidCase.Request with
            {
                VersionRowVersion = draft.RowVersion
            };
            using var response = await SendWithAntiforgeryAsync(
                client,
                $"/api/admin/request-types/{created.Id}/versions/{draft.Id}/fields",
                request);

            await AssertValidationProblemAsync(
                response,
                invalidCase.ExpectedField,
                invalidCase.ExpectedMessageFragment);
        }

        var unchanged = await client.GetFromJsonAsync<RequestTypeVersionResponse>(
            $"/api/admin/request-types/{created.Id}/versions/{draft.Id}");
        Assert.NotNull(unchanged);
        Assert.Empty(unchanged.Fields);
        Assert.Equal(draft.RowVersion, unchanged.RowVersion);
    }

    [Fact]
    public async Task Single_choice_default_must_match_option_value_case_exactly()
    {
        using var client = fixture.CreateClient(
            AdministrationApiFixture.BootstrapAdministratorAccount);
        var created = await CreateRequestTypeAsync(client);
        var draft = Assert.Single(created.Versions);
        var request = ChoiceField(
            "case_sensitive_choice_default",
            1,
            "{\"value\":\"open\",\"labelEnglish\":\"Open\",\"labelArabic\":\"مفتوح\",\"sortOrder\":1,\"isActive\":true}") with
        {
            DefaultValue = Json("\"OPEN\""),
            VersionRowVersion = draft.RowVersion
        };

        using var response = await SendWithAntiforgeryAsync(
            client,
            $"/api/admin/request-types/{created.Id}/versions/{draft.Id}/fields",
            request);
        await AssertValidationProblemAsync(
            response,
            "defaultValue",
            "match an active choice value");

        var unchanged = await client.GetFromJsonAsync<RequestTypeVersionResponse>(
            $"/api/admin/request-types/{created.Id}/versions/{draft.Id}");
        Assert.NotNull(unchanged);
        Assert.Empty(unchanged.Fields);
        Assert.Equal(draft.RowVersion, unchanged.RowVersion);
    }

    private static CreateRequestFieldDefinitionRequest DateTimeField(
        string key,
        int sortOrder,
        JsonElement? defaultValue = null,
        JsonElement? validationConfig = null) =>
        Field(
            key,
            "dateTime",
            sortOrder,
            defaultValue,
            validationConfig,
            choiceConfig: null,
            documentMode: null);

    private static CreateRequestFieldDefinitionRequest ChoiceField(
        string key,
        int sortOrder,
        string optionJson) =>
        Field(
            key,
            "singleChoice",
            sortOrder,
            defaultValue: null,
            validationConfig: null,
            Json($"{{\"options\":[{optionJson}]}}"),
            documentMode: null);

    private static CreateRequestFieldDefinitionRequest DocumentField(
        string key,
        int sortOrder,
        string extension) =>
        Field(
            key,
            "fileDocument",
            sortOrder,
            defaultValue: null,
            Json(
                $"{{\"maximumDocuments\":1,\"allowedExtensions\":[\"{extension}\"]}}"),
            choiceConfig: null,
            documentMode: "uploadOnly");

    private static CreateRequestFieldDefinitionRequest Field(
        string key,
        string fieldType,
        int sortOrder,
        JsonElement? defaultValue,
        JsonElement? validationConfig,
        JsonElement? choiceConfig,
        string? documentMode) =>
        new(
            key,
            $"Field {key}",
            $"الحقل {key}",
            null,
            null,
            fieldType,
            false,
            defaultValue,
            validationConfig,
            choiceConfig,
            sortOrder,
            true,
            documentMode,
            null);

    private static async Task<RequestTypeDetailResponse> CreateRequestTypeAsync(
        HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var request = new CreateRequestTypeRequest(
            $"canonical_validation_{suffix}",
            $"Canonical validation {suffix}",
            $"اختبار القيم القياسية {suffix}",
            "Canonical field-value validation regression test.",
            "اختبار التحقق من القيم القياسية للحقول.",
            $"C{suffix[..7]}".ToUpperInvariant(),
            $"canonical-validation-{suffix}",
            100);
        using var response = await SendWithAntiforgeryAsync(
            client,
            "/api/admin/request-types",
            request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content
            .ReadFromJsonAsync<RequestTypeDetailResponse>();
        return Assert.IsType<RequestTypeDetailResponse>(created);
    }

    private static async Task<HttpResponseMessage> SendWithAntiforgeryAsync<TBody>(
        HttpClient client,
        string requestUri,
        TBody body)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/security/antiforgery-token");
        Assert.NotNull(token);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", token.RequestToken);
        return await client.SendAsync(request);
    }

    private static async Task AssertValidationProblemAsync(
        HttpResponseMessage response,
        string expectedField,
        string expectedMessageFragment)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(
            "administration.validation_failed",
            problem.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            (int)HttpStatusCode.BadRequest,
            problem.RootElement.GetProperty("status").GetInt32());
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty(expectedField, out var fieldErrors));
        Assert.Contains(
            fieldErrors.EnumerateArray(),
            error => error.GetString()!.Contains(
                expectedMessageFragment,
                StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("SqlException", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SPSE26H", body, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed record InvalidFieldCase(
        CreateRequestFieldDefinitionRequest Request,
        string ExpectedField,
        string ExpectedMessageFragment);
}
