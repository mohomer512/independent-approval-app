using IndependentApproval.Api.Domain.Requests;
using IndependentApproval.Api.Domain.Workflows;
using IndependentApproval.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IndependentApproval.Api.Tests;

public sealed class WorkflowPersistenceTests(
    AdministrationApiFixture fixture) : IClassFixture<AdministrationApiFixture>
{
    [Fact]
    public async Task Workflow_model_uses_row_versions_immutable_keys_and_composite_ownership_foreign_keys()
    {
        await fixture.QueryDatabaseAsync(dbContext =>
        {
            var model = dbContext.Model;
            var root = Assert.IsAssignableFrom<IEntityType>(
                model.FindEntityType(typeof(WorkflowDefinition)));
            Assert.True(root.FindProperty(nameof(WorkflowDefinition.RowVersion))!
                .IsConcurrencyToken);
            Assert.Equal(
                PropertySaveBehavior.Throw,
                root.FindProperty(nameof(WorkflowDefinition.Code))!
                    .GetAfterSaveBehavior());
            Assert.Contains(
                root.GetIndexes(),
                index => index.IsUnique
                         && index.Properties.Select(property => property.Name)
                             .SequenceEqual([nameof(WorkflowDefinition.NormalizedCode)]));

            var reservation = Assert.IsAssignableFrom<IEntityType>(
                model.FindEntityType(typeof(WorkflowSlugReservation)));
            Assert.Equal(
                [nameof(WorkflowSlugReservation.NormalizedSlug)],
                reservation.FindPrimaryKey()!.Properties.Select(property => property.Name));
            Assert.Equal(
                PropertySaveBehavior.Throw,
                reservation.FindProperty(nameof(WorkflowSlugReservation.WorkflowDefinitionId))!
                    .GetAfterSaveBehavior());

            var version = Assert.IsAssignableFrom<IEntityType>(
                model.FindEntityType(typeof(WorkflowVersion)));
            Assert.True(version.FindProperty(nameof(WorkflowVersion.RowVersion))!
                .IsConcurrencyToken);
            Assert.Contains(
                version.GetKeys(),
                key => key.Properties.Select(property => property.Name).SequenceEqual(
                    [
                        nameof(WorkflowVersion.Id),
                        nameof(WorkflowVersion.WorkflowDefinitionId),
                        nameof(WorkflowVersion.RequestTypeVersionId)
                    ]));
            Assert.Contains(
                version.GetIndexes(),
                index => index.IsUnique
                         && index.GetFilter()?.Contains(
                             "Lifecycle",
                             StringComparison.Ordinal) == true);

            var step = Assert.IsAssignableFrom<IEntityType>(
                model.FindEntityType(typeof(WorkflowStep)));
            Assert.True(step.FindProperty(nameof(WorkflowStep.RowVersion))!
                .IsConcurrencyToken);
            Assert.Contains(
                step.GetKeys(),
                key => key.Properties.Select(property => property.Name).SequenceEqual(
                    [nameof(WorkflowStep.Id), nameof(WorkflowStep.WorkflowVersionId)]));

            AssertCompositeForeignKey<WorkflowStepRole>(
                model,
                [
                    nameof(WorkflowStepRole.WorkflowStepId),
                    nameof(WorkflowStepRole.WorkflowVersionId)
                ]);
            AssertCompositeForeignKey<WorkflowTransition>(
                model,
                [
                    nameof(WorkflowTransition.SourceStepId),
                    nameof(WorkflowTransition.WorkflowVersionId)
                ]);
            AssertCompositeForeignKey<WorkflowTransition>(
                model,
                [
                    nameof(WorkflowTransition.TargetStepId),
                    nameof(WorkflowTransition.WorkflowVersionId)
                ]);
            AssertCompositeForeignKey<WorkflowStepFieldPermission>(
                model,
                [
                    nameof(WorkflowStepFieldPermission.WorkflowVersionId),
                    nameof(WorkflowStepFieldPermission.RequestTypeVersionId)
                ]);
            AssertCompositeForeignKey<WorkflowStepFieldPermission>(
                model,
                [
                    nameof(WorkflowStepFieldPermission.WorkflowStepId),
                    nameof(WorkflowStepFieldPermission.WorkflowVersionId)
                ]);
            AssertCompositeForeignKey<WorkflowStepFieldPermission>(
                model,
                [
                    nameof(WorkflowStepFieldPermission.RequestFieldDefinitionId),
                    nameof(WorkflowStepFieldPermission.RequestTypeVersionId)
                ]);
            AssertCompositeForeignKey<ApprovalRequest>(
                model,
                [
                    nameof(ApprovalRequest.WorkflowVersionId),
                    nameof(ApprovalRequest.WorkflowDefinitionId),
                    nameof(ApprovalRequest.RequestTypeVersionId)
                ]);
            AssertCompositeForeignKey<ApprovalRequest>(
                model,
                [
                    nameof(ApprovalRequest.CurrentWorkflowStepId),
                    nameof(ApprovalRequest.WorkflowVersionId)
                ]);
            return Task.FromResult(true);
        });
    }

    [Fact]
    public async Task Database_rejects_cross_workflow_steps_roles_transitions_permissions_and_requests()
    {
        var harness = new WorkflowTestHarness(fixture);
        var firstSeed = await harness.SeedDesignAsync();
        var secondSeed = await harness.SeedDesignAsync();
        using var client = harness.CreateAdministratorClient();
        var first = await harness.CreateWorkflowAsync(client, firstSeed);
        var second = await harness.CreateWorkflowAsync(client, secondSeed);
        var firstVersion = await harness.AddStepAsync(
            client,
            first.Detail.Id,
            first.Version,
            "first_step",
            [firstSeed.Roles[0].Id],
            1,
            isStartStep: true);
        var secondVersion = await harness.AddStepAsync(
            client,
            second.Detail.Id,
            second.Version,
            "second_step",
            [secondSeed.Roles[0].Id],
            1,
            isStartStep: true);
        var firstStep = Assert.Single(firstVersion.Steps);
        var secondStep = Assert.Single(secondVersion.Steps);

        await AssertDatabaseRejectsAsync(new WorkflowStepRole
        {
            WorkflowStepId = secondStep.Id,
            WorkflowVersionId = firstVersion.Id,
            ApplicationRoleId = firstSeed.Roles[1].Id
        });
        await AssertDatabaseRejectsAsync(new WorkflowTransition
        {
            Id = Guid.NewGuid(),
            WorkflowVersionId = firstVersion.Id,
            Key = "cross_transition",
            NormalizedKey = "CROSS_TRANSITION",
            SourceStepId = secondStep.Id,
            TargetStepId = firstStep.Id,
            ActionLabelEnglish = "Cross transition",
            ActionLabelArabic = "انتقال متقاطع",
            ActionType = WorkflowActionType.Forward,
            ResultingStatus = ApprovalRequestStatus.InProgress,
            SortOrder = 99,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
            CreatedByUserId = fixture.BootstrapAdministratorId
        });
        await AssertDatabaseRejectsAsync(new WorkflowStepFieldPermission
        {
            Id = Guid.NewGuid(),
            WorkflowVersionId = firstVersion.Id,
            RequestTypeVersionId = firstSeed.RequestTypeVersionId,
            WorkflowStepId = firstStep.Id,
            RequestFieldDefinitionId = secondSeed.Fields[0].Id,
            Access = WorkflowFieldAccess.ReadOnly,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByAccount = AdministrationApiFixture.BootstrapAdministratorAccount,
            CreatedByUserId = fixture.BootstrapAdministratorId
        });
        await AssertDatabaseRejectsAsync(new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = $"{secondSeed.RequestPrefix}-2026-777777",
            RequestTypeId = secondSeed.RequestTypeId,
            RequestTypeVersionId = secondSeed.RequestTypeVersionId,
            WorkflowDefinitionId = first.Detail.Id,
            WorkflowVersionId = secondVersion.Id,
            CurrentWorkflowStepId = secondStep.Id,
            Title = "Cross-workflow request",
            Status = ApprovalRequestStatus.Draft,
            RequestedByUserId = fixture.BootstrapAdministratorId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
    }

    private static void AssertCompositeForeignKey<TEntity>(
        IModel model,
        IReadOnlyCollection<string> expectedProperties)
    {
        var entity = Assert.IsAssignableFrom<IEntityType>(
            model.FindEntityType(typeof(TEntity)));
        Assert.Contains(
            entity.GetForeignKeys(),
            foreignKey => foreignKey.Properties
                .Select(property => property.Name)
                .ToHashSet(StringComparer.Ordinal)
                .SetEquals(expectedProperties));
    }

    private async Task AssertDatabaseRejectsAsync<TEntity>(TEntity entity)
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
}
