using System.Text.Json;
using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Tests.Infrastructure;

namespace IndependentApproval.Api.Tests;

public sealed class WorkflowCompatibilityDefinitionTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Generic_model_represents_leave_request_definition_with_ordinary_custom_roles()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync(
            [
                new WorkflowSeedField("start_date", RequestFieldType.Date, true),
                new WorkflowSeedField("end_date", RequestFieldType.Date, true),
                new WorkflowSeedField("leave_for", RequestFieldType.SingleChoice, true)
            ],
            ["Employee", "Office Manager", "HR"]);
        using var client = harness.CreateAdministratorClient();
        var created = await harness.CreateWorkflowAsync(
            client,
            seed,
            harness.CreateWorkflowPayload(seed) with
            {
                NameEnglish = "Leave Request",
                NameArabic = "طلب إجازة",
                NavigationLabelEnglish = "Leave Requests",
                NavigationLabelArabic = "طلبات الإجازة"
            });
        var version = await harness.ReplaceStarterRolesAsync(
            client,
            created.Detail.Id,
            created.Version,
            seed.Role("Employee").Id);
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "employee",
            [seed.Role("Employee").Id],
            1,
            isStartStep: true,
            canEditRequestData: true,
            canOpenDocuments: false,
            commentPolicy: "optional");
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "office_manager",
            [seed.Role("Office Manager").Id],
            2,
            canOpenDocuments: false,
            commentPolicy: "required");
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "hr",
            [seed.Role("HR").Id],
            3,
            canOpenDocuments: false,
            commentPolicy: "required");
        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "employee",
            WorkflowTestHarness.PermissionsForStep(
                seed.Fields,
                editable: true,
                canEditDocuments: false));
        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "office_manager",
            WorkflowTestHarness.PermissionsForStep(
                seed.Fields,
                editable: false,
                canEditDocuments: false));
        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "hr",
            WorkflowTestHarness.PermissionsForStep(
                seed.Fields,
                editable: false,
                canEditDocuments: false));
        var employee = version.Steps.Single(step => step.Key == "employee");
        var officeManager = version.Steps.Single(step => step.Key == "office_manager");
        var hr = version.Steps.Single(step => step.Key == "hr");
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "submit",
            employee.Id,
            officeManager.Id,
            "submit",
            "submitted",
            1);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "office_approve",
            officeManager.Id,
            hr.Id,
            "approve",
            "inProgress",
            1,
            requiresComment: true);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "office_reject",
            officeManager.Id,
            null,
            "reject",
            "rejected",
            2,
            requiresComment: true,
            terminalOutcome: "rejected");
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "hr_approve",
            hr.Id,
            null,
            "approve",
            "approved",
            1,
            requiresComment: true,
            terminalOutcome: "approved");
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "hr_reject",
            hr.Id,
            null,
            "reject",
            "rejected",
            2,
            requiresComment: true,
            terminalOutcome: "rejected");

        var validation = await harness.ValidateAsync(
            client,
            created.Detail.Id,
            version);
        Assert.True(
            validation.IsValid,
            string.Join("; ", validation.Issues.Select(issue => issue.Code)));
        Assert.Equal(
            new[] { "start_date", "end_date", "leave_for" },
            seed.Fields.Select(field => field.Key));
        Assert.Equal(
            new[] { "Employee", "Office Manager", "HR" },
            seed.Roles.Select(role => role.LogicalName));
        Assert.All(seed.Roles, role => Assert.Contains(
            version.Steps.SelectMany(step => step.Roles),
            reference => reference.Id == role.Id));
        Assert.Equal(5, version.Transitions.Count);
        Assert.Contains(version.Transitions, transition =>
            transition.Key == "office_approve"
            && transition.TargetStepId == hr.Id);
    }

    [Fact]
    public async Task Generic_model_represents_gm_approval_loops_document_forwarding_and_editor_mode()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync(
            [
                new WorkflowSeedField("request_subject", RequestFieldType.ShortText, true),
                new WorkflowSeedField(
                    "approval_document",
                    RequestFieldType.RichDocument,
                    true,
                    DocumentFieldMode.UploadOrCreate)
            ],
            ["Secretary", "Office Manager", "HOD", "GM"]);
        using var client = harness.CreateAdministratorClient();
        var created = await harness.CreateWorkflowAsync(
            client,
            seed,
            harness.CreateWorkflowPayload(seed) with
            {
                NameEnglish = "GM Approval",
                NameArabic = "اعتماد المدير العام",
                NavigationLabelEnglish = "GM Approvals",
                NavigationLabelArabic = "اعتمادات المدير العام"
            });
        var version = await harness.ReplaceStarterRolesAsync(
            client,
            created.Detail.Id,
            created.Version,
            seed.Role("Secretary").Id);
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "secretary",
            [seed.Role("Secretary").Id],
            1,
            isStartStep: true,
            canEditRequestData: true,
            canOpenDocuments: true,
            canEditDocuments: true,
            commentPolicy: "optional");
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "office_manager",
            [seed.Role("Office Manager").Id],
            2,
            canEditRequestData: true,
            canOpenDocuments: true,
            canEditDocuments: true,
            canForwardDocuments: true,
            commentPolicy: "optional");
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "hod",
            [seed.Role("HOD").Id],
            3,
            canEditRequestData: true,
            canOpenDocuments: true,
            commentPolicy: "required");
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "gm",
            [seed.Role("GM").Id],
            4,
            canEditRequestData: true,
            canOpenDocuments: true,
            canEditDocuments: true,
            commentPolicy: "required");
        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "secretary",
            WorkflowTestHarness.PermissionsForStep(
                seed.Fields,
                editable: true,
                canEditDocuments: true));
        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "office_manager",
            WorkflowTestHarness.PermissionsForStep(
                seed.Fields,
                editable: true,
                canEditDocuments: true,
                canForwardDocuments: true));
        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "hod",
            WorkflowTestHarness.PermissionsForStep(
                seed.Fields,
                editable: true,
                canEditDocuments: false));
        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "gm",
            WorkflowTestHarness.PermissionsForStep(
                seed.Fields,
                editable: true,
                canEditDocuments: true));
        var secretary = version.Steps.Single(step => step.Key == "secretary");
        var officeManager = version.Steps.Single(step => step.Key == "office_manager");
        var hod = version.Steps.Single(step => step.Key == "hod");
        var gm = version.Steps.Single(step => step.Key == "gm");
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "submit",
            secretary.Id,
            officeManager.Id,
            "submit",
            "submitted",
            1);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "hold",
            officeManager.Id,
            officeManager.Id,
            "putOnHold",
            "onHold",
            1);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "resume",
            officeManager.Id,
            officeManager.Id,
            "resume",
            "inProgress",
            2);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "more_info_hod",
            officeManager.Id,
            hod.Id,
            "requestMoreInformation",
            "moreInformationRequired",
            3,
            requiresComment: true);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "more_info_secretary",
            officeManager.Id,
            secretary.Id,
            "requestMoreInformation",
            "moreInformationRequired",
            4,
            requiresComment: true);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "hod_return",
            hod.Id,
            officeManager.Id,
            "return",
            "inProgress",
            1,
            requiresComment: true);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "secretary_return",
            secretary.Id,
            officeManager.Id,
            "return",
            "inProgress",
            2,
            requiresComment: true);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "forward_gm",
            officeManager.Id,
            gm.Id,
            "forward",
            "inProgress",
            5);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "gm_approve",
            gm.Id,
            null,
            "approve",
            "approved",
            1,
            requiresComment: true,
            terminalOutcome: "approved");
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "gm_reject",
            gm.Id,
            null,
            "reject",
            "rejected",
            2,
            requiresComment: true,
            terminalOutcome: "rejected");

        var validation = await harness.ValidateAsync(
            client,
            created.Detail.Id,
            version);
        Assert.True(
            validation.IsValid,
            string.Join("; ", validation.Issues.Select(issue =>
                $"{issue.Code}:{issue.Path}")));
        var editorDocument = seed.Field("approval_document");
        Assert.Equal(RequestFieldType.RichDocument, editorDocument.FieldType);
        Assert.Equal(DocumentFieldMode.UploadOrCreate, editorDocument.DocumentMode);
        var officeDocumentPermission = officeManager.FieldPermissions.Single(permission =>
            permission.RequestFieldDefinitionId == editorDocument.Id);
        Assert.Equal("edit", officeDocumentPermission.DocumentAccess);
        Assert.True(officeDocumentPermission.CanSelectForForwarding);
        var gmDocumentPermission = gm.FieldPermissions.Single(permission =>
            permission.RequestFieldDefinitionId == editorDocument.Id);
        Assert.Equal("edit", gmDocumentPermission.DocumentAccess);
        Assert.Contains(version.Transitions, transition =>
            transition.Key == "more_info_hod"
            && transition.TargetStepId == hod.Id);
        Assert.Contains(version.Transitions, transition =>
            transition.Key == "more_info_secretary"
            && transition.TargetStepId == secretary.Id);
        Assert.Contains(version.Transitions, transition =>
            transition.Key == "hod_return"
            && transition.TargetStepId == officeManager.Id);

        var definitionJson = JsonSerializer.Serialize(version);
        Assert.DoesNotContain("file://", definitionJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("smb", definitionJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shared folder", definitionJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("physicalPath", definitionJson, StringComparison.OrdinalIgnoreCase);
    }
}
