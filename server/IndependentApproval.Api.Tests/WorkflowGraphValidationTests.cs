using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Tests.Infrastructure;

namespace IndependentApproval.Api.Tests;

public sealed class WorkflowGraphValidationTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Validator_rejects_missing_or_inactive_roles_start_reachability_dead_ends_and_permissions()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var created = await harness.CreateWorkflowAsync(client, seed);
        var emptyValidation = await harness.ValidateAsync(
            client,
            created.Detail.Id,
            created.Version);
        Assert.False(emptyValidation.IsValid);
        AssertIssue(emptyValidation, "workflow.starter_role_required");
        AssertIssue(emptyValidation, "workflow.single_start_step_required");

        using (var invalidPublish = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Post,
                   $"/api/admin/workflows/{created.Detail.Id}/versions/{created.Version.Id}/publish",
                   new WorkflowConcurrencyRequest(created.Version.RowVersion)))
        {
            Assert.Equal(HttpStatusCode.BadRequest, invalidPublish.StatusCode);
            var body = await invalidPublish.Content.ReadAsStringAsync();
            using var problem = JsonDocument.Parse(body);
            Assert.Equal(
                "administration.validation_failed",
                problem.RootElement.GetProperty("code").GetString());
            Assert.Contains("[workflow.starter_role_required]", body, StringComparison.Ordinal);
            Assert.DoesNotContain("SqlException", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        }

        var version = await harness.ReplaceStarterRolesAsync(
            client,
            created.Detail.Id,
            created.Version,
            seed.Roles[0].Id);
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "start",
            [seed.Roles[0].Id],
            1,
            isStartStep: true);
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "orphan",
            [seed.Roles[1].Id],
            2);
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "missing_role",
            [],
            3);

        using (var multipleStart = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Post,
                   $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}/steps",
                   new CreateWorkflowStepRequest(
                       "second_start", "Second start", "بداية ثانية", 4, 0, 0,
                       true, true, false, true, false, false, "optional",
                       [seed.Roles[0].Id], version.RowVersion)))
        {
            Assert.Equal(HttpStatusCode.BadRequest, multipleStart.StatusCode);
            var body = await multipleStart.Content.ReadAsStringAsync();
            using var problem = JsonDocument.Parse(body);
            Assert.Equal(
                "administration.validation_failed",
                problem.RootElement.GetProperty("code").GetString());
            var errors = problem.RootElement.GetProperty("errors");
            Assert.True(errors.TryGetProperty("isStartStep", out var startStepErrors));
            Assert.Contains(
                startStepErrors.EnumerateArray(),
                error => error.GetString()!.Contains(
                    "Only one step",
                    StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("SqlException", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        }

        var persistedStartStepCount = await fixture.QueryDatabaseAsync(dbContext =>
        {
            return Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
                dbContext.WorkflowSteps.Where(step =>
                    step.WorkflowVersionId == version.Id
                    && step.IsStartStep));
        });
        Assert.Equal(1, persistedStartStepCount);

        await fixture.QueryDatabaseAsync(async dbContext =>
        {
            var starterRole = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .SingleAsync(dbContext.ApplicationRoles.Where(role =>
                    role.Id == seed.Roles[0].Id));
            starterRole.IsActive = false;
            var orphanRole = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .SingleAsync(dbContext.ApplicationRoles.Where(role =>
                    role.Id == seed.Roles[1].Id));
            orphanRole.IsActive = false;
            orphanRole.IsArchived = true;
            await dbContext.SaveChangesAsync();
            return true;
        });

        var invalid = await harness.ValidateAsync(
            client,
            created.Detail.Id,
            version);
        Assert.False(invalid.IsValid);
        AssertIssue(invalid, "workflow.starter_role_inactive");
        AssertIssue(invalid, "workflow.starter_role_required");
        AssertIssue(invalid, "workflow.step_role_inactive");
        AssertIssue(invalid, "workflow.step_role_required");
        AssertIssue(invalid, "workflow.step_unreachable");
        AssertIssue(invalid, "workflow.terminal_transition_required");
        AssertIssue(invalid, "workflow.path_dead_end");
        AssertIssue(invalid, "workflow.field_permission_required");
    }

    [Fact]
    public async Task Transition_endpoints_reject_missing_cross_version_and_inactive_step_ownership()
    {
        var harness = new WorkflowTestHarness(fixture);
        var firstSeed = await harness.SeedDesignAsync(fields: []);
        var secondSeed = await harness.SeedDesignAsync(fields: []);
        using var client = harness.CreateAdministratorClient();
        var first = await harness.CreateWorkflowAsync(client, firstSeed);
        var second = await harness.CreateWorkflowAsync(client, secondSeed);
        var firstVersion = await harness.AddStepAsync(
            client,
            first.Detail.Id,
            first.Version,
            "first",
            [firstSeed.Roles[0].Id],
            1,
            isStartStep: true);
        firstVersion = await harness.AddStepAsync(
            client,
            first.Detail.Id,
            firstVersion,
            "inactive",
            [firstSeed.Roles[1].Id],
            2,
            isActive: false);
        var secondVersion = await harness.AddStepAsync(
            client,
            second.Detail.Id,
            second.Version,
            "foreign",
            [secondSeed.Roles[0].Id],
            1,
            isStartStep: true);
        var source = firstVersion.Steps.Single(step => step.Key == "first");
        var inactive = firstVersion.Steps.Single(step => step.Key == "inactive");
        var foreign = Assert.Single(secondVersion.Steps);

        var invalidEndpoints = new[]
        {
            new CreateWorkflowTransitionRequest(
                "missing_source", Guid.NewGuid(), source.Id, "Missing", "مفقود",
                "forward", "inProgress", false, null, 1, true,
                firstVersion.RowVersion),
            new CreateWorkflowTransitionRequest(
                "foreign_source", foreign.Id, source.Id, "Foreign", "خارجي",
                "forward", "inProgress", false, null, 2, true,
                firstVersion.RowVersion),
            new CreateWorkflowTransitionRequest(
                "foreign_target", source.Id, foreign.Id, "Foreign", "خارجي",
                "forward", "inProgress", false, null, 3, true,
                firstVersion.RowVersion)
        };

        foreach (var request in invalidEndpoints)
        {
            using var response = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                client,
                HttpMethod.Post,
                $"/api/admin/workflows/{first.Detail.Id}/versions/{firstVersion.Id}/transitions",
                request);
            await WorkflowTestHarness.AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "administration.validation_failed");
        }

        firstVersion = await harness.AddTransitionAsync(
            client,
            first.Detail.Id,
            firstVersion,
            "to_inactive",
            source.Id,
            inactive.Id,
            "forward",
            "inProgress",
            1);
        firstVersion = await harness.AddTransitionAsync(
            client,
            first.Detail.Id,
            firstVersion,
            "from_inactive",
            inactive.Id,
            source.Id,
            "return",
            "inProgress",
            1);
        var validation = await harness.ValidateAsync(
            client,
            first.Detail.Id,
            firstVersion);
        AssertIssue(validation, "workflow.transition_target_inactive");
        AssertIssue(validation, "workflow.transition_source_inactive");
    }

    [Fact]
    public async Task Validator_rejects_malformed_action_status_terminal_and_comment_combinations()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync(fields: []);
        using var client = harness.CreateAdministratorClient();
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
            "no_comments",
            [seed.Roles[0].Id],
            1,
            isStartStep: true,
            commentPolicy: "none");
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "comments_required",
            [seed.Roles[1].Id],
            2,
            commentPolicy: "required");
        var start = version.Steps.Single(step => step.Key == "no_comments");
        var required = version.Steps.Single(step => step.Key == "comments_required");
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "valid_submit",
            start.Id,
            required.Id,
            "submit",
            "submitted",
            1);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "valid_terminal",
            required.Id,
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
            "comment_forbidden",
            start.Id,
            required.Id,
            "forward",
            "inProgress",
            2,
            requiresComment: true);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "comment_missing",
            required.Id,
            start.Id,
            "return",
            "inProgress",
            2,
            requiresComment: false);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "terminal_submit",
            start.Id,
            null,
            "submit",
            "completed",
            3,
            terminalOutcome: "completed");
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "nonterminal_complete",
            start.Id,
            required.Id,
            "complete",
            "inProgress",
            4);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "wrong_terminal_action",
            required.Id,
            null,
            "approve",
            "rejected",
            3,
            requiresComment: true,
            terminalOutcome: "rejected");

        var validation = await harness.ValidateAsync(
            client,
            created.Detail.Id,
            version);
        Assert.False(validation.IsValid);
        Assert.Equal(
            2,
            validation.Issues.Count(issue =>
                issue.Code == "workflow.comment_policy_conflict"));
        Assert.True(
            validation.Issues.Count(issue =>
                issue.Code == "workflow.action_status_incompatible") >= 3);

        using var terminalMismatch = await WorkflowTestHarness.SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}/transitions",
            new CreateWorkflowTransitionRequest(
                "terminal_mismatch",
                required.Id,
                null,
                "Reject",
                "رفض",
                "reject",
                "approved",
                true,
                "rejected",
                4,
                true,
                version.RowVersion));
        await WorkflowTestHarness.AssertProblemAsync(
            terminalMismatch,
            HttpStatusCode.BadRequest,
            "administration.validation_failed");
    }

    [Fact]
    public async Task Validator_accepts_self_loops_cycles_and_return_paths_with_a_terminal_exit()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var created = await harness.CreateWorkflowAsync(client, seed);
        var version = await harness.BuildSimpleValidGraphAsync(
            client,
            created.Detail.Id,
            seed,
            created.Version);
        var start = version.Steps.Single(step => step.Key == "start");
        var approval = version.Steps.Single(step => step.Key == "approval");
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "hold_self",
            approval.Id,
            approval.Id,
            "putOnHold",
            "onHold",
            2,
            requiresComment: true);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "resume_self",
            approval.Id,
            approval.Id,
            "resume",
            "inProgress",
            3,
            requiresComment: true);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "return_to_start",
            approval.Id,
            start.Id,
            "return",
            "moreInformationRequired",
            4,
            requiresComment: true);

        var validation = await harness.ValidateAsync(
            client,
            created.Detail.Id,
            version);
        Assert.True(
            validation.IsValid,
            string.Join("; ", validation.Issues.Select(issue =>
                $"{issue.Code}:{issue.Path}")));
    }

    [Fact]
    public async Task Field_matrix_rejects_cross_form_missing_and_capability_escalating_permissions()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        var foreignSeed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
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
            "restricted",
            [seed.Roles[0].Id],
            1,
            isStartStep: true,
            canEditRequestData: false,
            canOpenDocuments: false,
            canEditDocuments: true,
            canForwardDocuments: true,
            commentPolicy: "optional");
        var step = Assert.Single(version.Steps);
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "terminal",
            step.Id,
            null,
            "approve",
            "approved",
            1,
            terminalOutcome: "approved");

        var invalidOwnershipInputs = new[]
        {
            new WorkflowStepFieldPermissionInput(
                foreignSeed.Fields[0].Id,
                "readOnly",
                null,
                false,
                null),
            new WorkflowStepFieldPermissionInput(
                Guid.NewGuid(),
                "readOnly",
                null,
                false,
                null)
        };

        foreach (var invalidInput in invalidOwnershipInputs)
        {
            var currentStep = version.Steps.Single(candidate => candidate.Key == "restricted");
            using var response = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                client,
                HttpMethod.Put,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}" +
                $"/steps/{currentStep.Id}/field-permissions",
                new ReplaceWorkflowStepFieldPermissionsRequest(
                    [invalidInput],
                    version.RowVersion,
                    currentStep.RowVersion));
            await WorkflowTestHarness.AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "administration.validation_failed");
        }

        var nonDocumentWithDocumentAccess = new WorkflowStepFieldPermissionInput(
            seed.Field("details").Id,
            "readOnly",
            "view",
            false,
            null);
        var documentWithoutAccess = new WorkflowStepFieldPermissionInput(
            seed.Field("attachment").Id,
            "readOnly",
            null,
            false,
            null);
        foreach (var invalidInput in new[]
                 {
                     nonDocumentWithDocumentAccess,
                     documentWithoutAccess
                 })
        {
            var currentStep = version.Steps.Single(candidate => candidate.Key == "restricted");
            using var response = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                client,
                HttpMethod.Put,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}" +
                $"/steps/{currentStep.Id}/field-permissions",
                new ReplaceWorkflowStepFieldPermissionsRequest(
                    [invalidInput],
                    version.RowVersion,
                    currentStep.RowVersion));
            await WorkflowTestHarness.AssertProblemAsync(
                response,
                HttpStatusCode.BadRequest,
                "administration.validation_failed");
        }

        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "restricted",
            [
                new WorkflowStepFieldPermissionInput(
                    seed.Field("details").Id,
                    "editable",
                    null,
                    false,
                    null),
                new WorkflowStepFieldPermissionInput(
                    seed.Field("attachment").Id,
                    "hidden",
                    "edit",
                    true,
                    null)
            ]);
        var invalidCapabilities = await harness.ValidateAsync(
            client,
            created.Detail.Id,
            version);
        AssertIssue(invalidCapabilities, "workflow.step_document_capability_invalid");
        AssertIssue(invalidCapabilities, "workflow.field_edit_not_allowed");
        AssertIssue(invalidCapabilities, "workflow.hidden_field_document_access");
        AssertIssue(invalidCapabilities, "workflow.document_open_not_allowed");
        AssertIssue(invalidCapabilities, "workflow.document_forward_not_allowed");

        var permissionStep = version.Steps.Single(candidate => candidate.Key == "restricted");
        var detailPermission = permissionStep.FieldPermissions.Single(permission =>
            permission.RequestFieldDefinitionId == seed.Field("details").Id);
        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "restricted",
            [
                new WorkflowStepFieldPermissionInput(
                    detailPermission.RequestFieldDefinitionId,
                    "readOnly",
                    null,
                    false,
                    detailPermission.RowVersion)
            ]);
        var incompleteMatrix = await harness.ValidateAsync(
            client,
            created.Detail.Id,
            version);
        AssertIssue(incompleteMatrix, "workflow.field_permission_required");
    }

    private static void AssertIssue(
        WorkflowValidationResponse response,
        string code) =>
        Assert.Contains(response.Issues, issue => issue.Code == code);
}
