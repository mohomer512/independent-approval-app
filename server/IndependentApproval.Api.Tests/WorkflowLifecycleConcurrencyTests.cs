using System.Net;
using System.Net.Http.Json;
using IndependentApproval.Api.Contracts.Administration;
using IndependentApproval.Api.Contracts.Administration.Workflows;
using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Tests.Infrastructure;

namespace IndependentApproval.Api.Tests;

public sealed class WorkflowLifecycleConcurrencyTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Parent_step_transition_and_permission_row_versions_reject_stale_changes()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var created = await harness.CreateWorkflowAsync(client, seed);
        var original = created.Version;

        var updated = await harness.MutateVersionAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{original.Id}",
            VersionUpdate(original, "Updated workflow"),
            HttpStatusCode.OK);
        Assert.NotEqual(original.RowVersion, updated.RowVersion);
        using (var staleParent = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Put,
                   $"/api/admin/workflows/{created.Detail.Id}/versions/{original.Id}",
                   VersionUpdate(original, "Stale workflow")))
        {
            await WorkflowTestHarness.AssertProblemAsync(
                staleParent,
                HttpStatusCode.Conflict,
                "administration.concurrency_conflict");
        }

        var withStep = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            updated,
            "review",
            [seed.Roles[0].Id],
            1,
            isStartStep: true,
            canEditRequestData: true,
            canEditDocuments: true);
        var originalStep = Assert.Single(withStep.Steps);
        var stepUpdated = await harness.MutateVersionAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{withStep.Id}" +
            $"/steps/{originalStep.Id}",
            StepUpdate(originalStep, withStep.RowVersion, "Updated step"),
            HttpStatusCode.OK);
        var currentStep = Assert.Single(stepUpdated.Steps);
        Assert.NotEqual(originalStep.RowVersion, currentStep.RowVersion);
        using (var staleStep = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Put,
                   $"/api/admin/workflows/{created.Detail.Id}/versions/{stepUpdated.Id}" +
                   $"/steps/{currentStep.Id}",
                   StepUpdate(originalStep, stepUpdated.RowVersion, "Stale step")))
        {
            await WorkflowTestHarness.AssertProblemAsync(
                staleStep,
                HttpStatusCode.Conflict,
                "administration.concurrency_conflict");
        }

        var withTarget = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            stepUpdated,
            "target",
            [seed.Roles[1].Id],
            2);
        var source = withTarget.Steps.Single(step => step.Key == "review");
        var target = withTarget.Steps.Single(step => step.Key == "target");
        var withTransition = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            withTarget,
            "send",
            source.Id,
            target.Id,
            "forward",
            "inProgress",
            1);
        var originalTransition = Assert.Single(withTransition.Transitions);
        var transitionUpdated = await harness.MutateVersionAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{withTransition.Id}" +
            $"/transitions/{originalTransition.Id}",
            TransitionUpdate(
                originalTransition,
                withTransition.RowVersion,
                "Updated action"),
            HttpStatusCode.OK);
        var currentTransition = Assert.Single(transitionUpdated.Transitions);
        Assert.NotEqual(originalTransition.RowVersion, currentTransition.RowVersion);
        using (var staleTransition = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Put,
                   $"/api/admin/workflows/{created.Detail.Id}/versions/{transitionUpdated.Id}" +
                   $"/transitions/{currentTransition.Id}",
                   TransitionUpdate(
                       originalTransition,
                       transitionUpdated.RowVersion,
                       "Stale action")))
        {
            await WorkflowTestHarness.AssertProblemAsync(
                staleTransition,
                HttpStatusCode.Conflict,
                "administration.concurrency_conflict");
        }

        var withPermissions = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            transitionUpdated,
            "review",
            WorkflowTestHarness.PermissionsForStep(
                seed.Fields,
                editable: true,
                canEditDocuments: true));
        var permissionStep = withPermissions.Steps.Single(step => step.Key == "review");
        var originalPermissions = permissionStep.FieldPermissions.ToArray();
        var permissionInputs = originalPermissions.Select(permission =>
            new WorkflowStepFieldPermissionInput(
                permission.RequestFieldDefinitionId,
                permission.Access,
                permission.DocumentAccess,
                permission.CanSelectForForwarding,
                permission.RowVersion))
            .ToArray();
        var permissionUpdated = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            withPermissions,
            "review",
            permissionInputs.Select(input => input with
            {
                Access = input.Access == "editableRequired" ? "editable" : input.Access
            }).ToArray());
        Assert.NotEqual(withPermissions.RowVersion, permissionUpdated.RowVersion);

        var latestStep = permissionUpdated.Steps.Single(step => step.Key == "review");
        using var stalePermission = await WorkflowTestHarness.SendWithAntiforgeryAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{permissionUpdated.Id}" +
            $"/steps/{latestStep.Id}/field-permissions",
            new ReplaceWorkflowStepFieldPermissionsRequest(
                permissionInputs,
                permissionUpdated.RowVersion,
                latestStep.RowVersion));
        await WorkflowTestHarness.AssertProblemAsync(
            stalePermission,
            HttpStatusCode.Conflict,
            "administration.concurrency_conflict");
    }

    [Fact]
    public async Task Publish_racing_a_graph_edit_allows_exactly_one_change()
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
        var validation = await harness.ValidateAsync(client, created.Detail.Id, version);
        Assert.True(validation.IsValid, string.Join("; ", validation.Issues.Select(issue => issue.Code)));
        var token = await WorkflowTestHarness.GetAntiforgeryTokenAsync(client);
        using var publishRequest = WorkflowTestHarness.CreateAntiforgeryRequest(
            HttpMethod.Post,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}/publish",
            new WorkflowConcurrencyRequest(version.RowVersion),
            token);
        using var editRequest = WorkflowTestHarness.CreateAntiforgeryRequest(
            HttpMethod.Post,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}/steps",
            new CreateWorkflowStepRequest(
                "racing_inactive",
                "Racing inactive step",
                "خطوة سباق غير نشطة",
                99,
                100,
                100,
                false,
                false,
                false,
                false,
                false,
                false,
                "none",
                [],
                version.RowVersion),
            token);
        var responses = await Task.WhenAll(
            client.SendAsync(publishRequest),
            client.SendAsync(editRequest));

        try
        {
            Assert.Single(
                responses,
                response => response.StatusCode is HttpStatusCode.OK
                    or HttpStatusCode.Created);
            var conflict = Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.Conflict);
            await WorkflowTestHarness.AssertProblemAsync(
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

        var persisted = await client.GetFromJsonAsync<WorkflowVersionResponse>(
            $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}");
        Assert.NotNull(persisted);
        Assert.True(
            persisted.Lifecycle == "published" && persisted.Steps.Count == 2
            || persisted.Lifecycle == "draft" && persisted.Steps.Count == 3);
    }

    [Fact]
    public async Task Published_graph_is_immutable_and_clone_remaps_the_complete_graph()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var created = await harness.CreateWorkflowAsync(client, seed);
        var draft = await harness.BuildSimpleValidGraphAsync(
            client,
            created.Detail.Id,
            seed,
            created.Version);
        var published = await harness.PublishAsync(client, created.Detail.Id, draft);
        Assert.Equal("published", published.Lifecycle);
        Assert.NotNull(published.PublishedAtUtc);
        var step = published.Steps.Single(candidate => candidate.Key == "start");
        var transition = published.Transitions.Single(candidate => candidate.Key == "submit");
        var permission = Assert.Single(
            step.FieldPermissions,
            candidate => candidate.RequestFieldDefinitionId == seed.Fields[0].Id);
        var immutableMutations = new (HttpMethod Method, string Path, object Body)[]
        {
            (
                HttpMethod.Put,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}",
                VersionUpdate(published, "Forbidden metadata")),
            (
                HttpMethod.Put,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}/starter-roles",
                new ReplaceWorkflowStarterRolesRequest([seed.Roles[0].Id], published.RowVersion)),
            (
                HttpMethod.Post,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}/steps",
                new CreateWorkflowStepRequest(
                    "forbidden", "Forbidden", "ممنوع", 50, 0, 0, false, true,
                    false, true, false, false, "none", [seed.Roles[0].Id],
                    published.RowVersion)),
            (
                HttpMethod.Put,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}/steps/{step.Id}",
                StepUpdate(step, published.RowVersion, "Forbidden step")),
            (
                HttpMethod.Delete,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}/steps/{step.Id}",
                new DeleteWorkflowStepRequest(published.RowVersion, step.RowVersion)),
            (
                HttpMethod.Put,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}/steps/{step.Id}/roles",
                new ReplaceWorkflowStepRolesRequest(
                    [seed.Roles[0].Id], published.RowVersion, step.RowVersion)),
            (
                HttpMethod.Put,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}" +
                $"/steps/{step.Id}/field-permissions",
                new ReplaceWorkflowStepFieldPermissionsRequest(
                    [new WorkflowStepFieldPermissionInput(
                        permission.RequestFieldDefinitionId,
                        permission.Access,
                        permission.DocumentAccess,
                        permission.CanSelectForForwarding,
                        permission.RowVersion)],
                    published.RowVersion,
                    step.RowVersion)),
            (
                HttpMethod.Post,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}/transitions",
                new CreateWorkflowTransitionRequest(
                    "forbidden_transition", step.Id, step.Id, "Forbidden", "ممنوع",
                    "return", "inProgress", false, null, 50, true,
                    published.RowVersion)),
            (
                HttpMethod.Put,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}" +
                $"/transitions/{transition.Id}",
                TransitionUpdate(transition, published.RowVersion, "Forbidden transition")),
            (
                HttpMethod.Delete,
                $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}" +
                $"/transitions/{transition.Id}",
                new DeleteWorkflowTransitionRequest(
                    published.RowVersion,
                    transition.RowVersion))
        };

        foreach (var mutation in immutableMutations)
        {
            using var response = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                client,
                mutation.Method,
                mutation.Path,
                mutation.Body);
            await WorkflowTestHarness.AssertProblemAsync(
                response,
                HttpStatusCode.Conflict,
                "administration.workflow_version_immutable");
        }

        var clone = await harness.MutateVersionAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}/clone",
            new WorkflowConcurrencyRequest(published.RowVersion),
            HttpStatusCode.Created);
        Assert.Equal("draft", clone.Lifecycle);
        Assert.Equal(2, clone.VersionNumber);
        Assert.Equal(published.StarterRoles.Select(role => role.Id),
            clone.StarterRoles.Select(role => role.Id));
        Assert.Equal(published.Steps.Count, clone.Steps.Count);
        Assert.Equal(published.Transitions.Count, clone.Transitions.Count);
        Assert.Empty(published.Steps.Select(step => step.Id)
            .Intersect(clone.Steps.Select(step => step.Id)));
        Assert.Empty(published.Transitions.Select(item => item.Id)
            .Intersect(clone.Transitions.Select(item => item.Id)));

        foreach (var originalStep in published.Steps)
        {
            var clonedStep = clone.Steps.Single(candidate =>
                candidate.Key == originalStep.Key);
            Assert.Equal(
                originalStep.Roles.Select(role => role.Id),
                clonedStep.Roles.Select(role => role.Id));
            Assert.Equal(
                originalStep.FieldPermissions
                    .Select(item => (item.RequestFieldDefinitionId, item.Access,
                        item.DocumentAccess, item.CanSelectForForwarding)),
                clonedStep.FieldPermissions
                    .Select(item => (item.RequestFieldDefinitionId, item.Access,
                        item.DocumentAccess, item.CanSelectForForwarding)));
            Assert.Empty(originalStep.FieldPermissions.Select(item => item.Id)
                .Intersect(clonedStep.FieldPermissions.Select(item => item.Id)));
        }

        foreach (var originalTransition in published.Transitions)
        {
            var clonedTransition = clone.Transitions.Single(candidate =>
                candidate.Key == originalTransition.Key);
            Assert.Equal(
                StepKey(published, originalTransition.SourceStepId),
                StepKey(clone, clonedTransition.SourceStepId));
            Assert.Equal(
                StepKey(published, originalTransition.TargetStepId),
                StepKey(clone, clonedTransition.TargetStepId));
        }

        using var duplicateClone = await WorkflowTestHarness.SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}/clone",
            new WorkflowConcurrencyRequest(published.RowVersion));
        await WorkflowTestHarness.AssertProblemAsync(
            duplicateClone,
            HttpStatusCode.Conflict,
            "administration.workflow_draft_exists");
    }

    [Fact]
    public async Task In_use_version_is_reported_locked_and_archiving_preserves_history()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var created = await harness.CreateWorkflowAsync(client, seed);
        var draft = await harness.BuildSimpleValidGraphAsync(
            client,
            created.Detail.Id,
            seed,
            created.Version);
        var published = await harness.PublishAsync(client, created.Detail.Id, draft);
        var requestId = Guid.NewGuid();
        await fixture.QueryDatabaseAsync(async dbContext =>
        {
            dbContext.ApprovalRequests.Add(new ApprovalRequest
            {
                Id = requestId,
                RequestNumber = $"{seed.RequestPrefix}-2026-888888",
                RequestTypeId = seed.RequestTypeId,
                RequestTypeVersionId = seed.RequestTypeVersionId,
                WorkflowDefinitionId = created.Detail.Id,
                WorkflowVersionId = published.Id,
                CurrentWorkflowStepId = published.Steps.Single(step => step.Key == "start").Id,
                Title = "Workflow history preservation",
                Status = ApprovalRequestStatus.InProgress,
                RequestedByUserId = fixture.BootstrapAdministratorId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await dbContext.SaveChangesAsync();
            return true;
        });
        var inUse = await client.GetFromJsonAsync<WorkflowVersionResponse>(
            $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}");
        Assert.NotNull(inUse);
        Assert.True(inUse.IsInUse);
        Assert.Equal(1, inUse.RequestCount);
        using (var inUseMutation = await WorkflowTestHarness.SendWithAntiforgeryAsync(
                   client,
                   HttpMethod.Put,
                   $"/api/admin/workflows/{created.Detail.Id}/versions/{inUse.Id}",
                   VersionUpdate(inUse, "Blocked in-use update")))
        {
            await WorkflowTestHarness.AssertProblemAsync(
                inUseMutation,
                HttpStatusCode.Conflict,
                "administration.workflow_in_use");
        }

        using var archiveVersionResponse = await WorkflowTestHarness.SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}/archive",
            new WorkflowConcurrencyRequest(inUse.RowVersion));
        Assert.Equal(HttpStatusCode.OK, archiveVersionResponse.StatusCode);
        var archivedVersion = await archiveVersionResponse.Content
            .ReadFromJsonAsync<WorkflowVersionResponse>();
        Assert.NotNull(archivedVersion);
        Assert.Equal("archived", archivedVersion.Lifecycle);
        Assert.True(archivedVersion.IsInUse);
        Assert.Equal(1, archivedVersion.RequestCount);
        Assert.Equal(published.Steps.Count, archivedVersion.Steps.Count);

        var detail = await client.GetFromJsonAsync<WorkflowDetailResponse>(
            $"/api/admin/workflows/{created.Detail.Id}");
        Assert.NotNull(detail);
        using var archiveRootResponse = await WorkflowTestHarness.SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{created.Detail.Id}/archive",
            new WorkflowConcurrencyRequest(detail.RowVersion));
        Assert.Equal(HttpStatusCode.OK, archiveRootResponse.StatusCode);
        var archivedRoot = await archiveRootResponse.Content
            .ReadFromJsonAsync<WorkflowDetailResponse>();
        Assert.NotNull(archivedRoot);
        Assert.True(archivedRoot.IsArchived);

        var historyStillExists = await fixture.QueryDatabaseAsync(async dbContext =>
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .AnyAsync(dbContext.ApprovalRequests.Where(request => request.Id == requestId)));
        Assert.True(historyStillExists);
        var defaultList = await client.GetFromJsonAsync<
            IndependentApproval.Api.Contracts.Common.PagedResponse<WorkflowListItemResponse>>(
            "/api/admin/workflows?page=1&pageSize=100");
        var archiveList = await client.GetFromJsonAsync<
            IndependentApproval.Api.Contracts.Common.PagedResponse<WorkflowListItemResponse>>(
            "/api/admin/workflows?includeArchived=true&page=1&pageSize=100");
        Assert.NotNull(defaultList);
        Assert.NotNull(archiveList);
        Assert.DoesNotContain(defaultList.Items, item => item.Id == created.Detail.Id);
        Assert.Contains(archiveList.Items, item => item.Id == created.Detail.Id);
    }

    [Fact]
    public async Task Draft_rebind_to_another_published_form_version_clears_stale_permissions()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        var replacement = await harness.SeedAdditionalPublishedFormVersionAsync(
            seed,
            [new WorkflowSeedField("replacement_field", RequestFieldType.ShortText, true)]);
        using var client = harness.CreateAdministratorClient();
        var created = await harness.CreateWorkflowAsync(client, seed);
        var version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            created.Version,
            "start",
            [seed.Roles[0].Id],
            1,
            isStartStep: true,
            canEditRequestData: true,
            canEditDocuments: true);
        version = await harness.AddStepAsync(
            client,
            created.Detail.Id,
            version,
            "target",
            [seed.Roles[1].Id],
            2);
        version = await harness.ReplacePermissionsAsync(
            client,
            created.Detail.Id,
            version,
            "start",
            WorkflowTestHarness.PermissionsForStep(
                seed.Fields,
                editable: true,
                canEditDocuments: true));
        var start = version.Steps.Single(step => step.Key == "start");
        var target = version.Steps.Single(step => step.Key == "target");
        version = await harness.AddTransitionAsync(
            client,
            created.Detail.Id,
            version,
            "send",
            start.Id,
            target.Id,
            "forward",
            "inProgress",
            1);
        var oldStepIds = version.Steps.Select(step => step.Id).ToArray();
        var oldTransitionIds = version.Transitions.Select(item => item.Id).ToArray();

        var rebound = await harness.MutateVersionAsync(
            client,
            HttpMethod.Put,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{version.Id}",
            VersionUpdate(version, version.NameEnglish, replacement.Id),
            HttpStatusCode.OK);
        Assert.Equal(replacement.Id, rebound.RequestTypeVersionId);
        Assert.Equal(oldStepIds, rebound.Steps.Select(step => step.Id));
        Assert.Equal(oldTransitionIds, rebound.Transitions.Select(item => item.Id));
        Assert.All(rebound.Steps, step => Assert.Empty(step.FieldPermissions));
        var validation = await harness.ValidateAsync(client, created.Detail.Id, rebound);
        Assert.False(validation.IsValid);
        Assert.Contains(
            validation.Issues,
            issue => issue.Code.Contains("permission", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Published_workflow_summary_count_tracks_version_archive()
    {
        var harness = new WorkflowTestHarness(fixture);
        var seed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var before = await client.GetFromJsonAsync<AdministrationSummaryResponse>(
            "/api/admin/summary");
        Assert.NotNull(before);
        var created = await harness.CreateWorkflowAsync(client, seed);
        var draft = await harness.BuildSimpleValidGraphAsync(
            client,
            created.Detail.Id,
            seed,
            created.Version);
        var published = await harness.PublishAsync(client, created.Detail.Id, draft);
        var afterPublish = await client.GetFromJsonAsync<AdministrationSummaryResponse>(
            "/api/admin/summary");
        Assert.NotNull(afterPublish);
        Assert.Equal(before.PublishedWorkflowCount + 1, afterPublish.PublishedWorkflowCount);

        using var archiveResponse = await WorkflowTestHarness.SendWithAntiforgeryAsync(
            client,
            HttpMethod.Post,
            $"/api/admin/workflows/{created.Detail.Id}/versions/{published.Id}/archive",
            new WorkflowConcurrencyRequest(published.RowVersion));
        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);
        var afterArchive = await client.GetFromJsonAsync<AdministrationSummaryResponse>(
            "/api/admin/summary");
        Assert.NotNull(afterArchive);
        Assert.Equal(before.PublishedWorkflowCount, afterArchive.PublishedWorkflowCount);
    }

    private static UpdateWorkflowVersionRequest VersionUpdate(
        WorkflowVersionResponse version,
        string nameEnglish,
        Guid? requestTypeVersionId = null) =>
        new(
            requestTypeVersionId ?? version.RequestTypeVersionId,
            nameEnglish,
            version.NameArabic,
            version.DescriptionEnglish,
            version.DescriptionArabic,
            version.NavigationLabelEnglish,
            version.NavigationLabelArabic,
            version.NavigationSlug,
            version.NavigationOrder,
            version.RowVersion);

    private static UpdateWorkflowStepRequest StepUpdate(
        WorkflowStepResponse step,
        string versionRowVersion,
        string nameEnglish) =>
        new(
            nameEnglish,
            step.NameArabic,
            step.SortOrder,
            step.DiagramX,
            step.DiagramY,
            step.IsStartStep,
            step.IsActive,
            step.CanEditRequestData,
            step.CanOpenDocuments,
            step.CanEditDocuments,
            step.CanForwardDocuments,
            step.CommentPolicy,
            versionRowVersion,
            step.RowVersion);

    private static UpdateWorkflowTransitionRequest TransitionUpdate(
        WorkflowTransitionResponse transition,
        string versionRowVersion,
        string labelEnglish) =>
        new(
            transition.SourceStepId,
            transition.TargetStepId,
            labelEnglish,
            transition.ActionLabelArabic,
            transition.ActionType,
            transition.ResultingStatus,
            transition.RequiresComment,
            transition.TerminalOutcome,
            transition.SortOrder,
            transition.IsActive,
            versionRowVersion,
            transition.RowVersion);

    private static string? StepKey(
        WorkflowVersionResponse version,
        Guid? stepId) =>
        stepId is null
            ? null
            : version.Steps.Single(step => step.Id == stepId).Key;
}
