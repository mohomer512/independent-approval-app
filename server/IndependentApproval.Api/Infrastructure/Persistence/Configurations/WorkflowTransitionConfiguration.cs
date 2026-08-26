using System.Linq.Expressions;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable("WorkflowTransitions", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_WorkflowTransitions_SortOrder_NonNegative",
                "[SortOrder] >= 0");
            table.HasCheckConstraint(
                "CK_WorkflowTransitions_ActionType_Valid",
                "[ActionType] IN ('Submit', 'Approve', 'Reject', 'PutOnHold', 'Resume', " +
                "'RequestMoreInformation', 'Return', 'Forward', 'Complete')");
            table.HasCheckConstraint(
                "CK_WorkflowTransitions_ResultingStatus_Valid",
                "[ResultingStatus] IN ('Draft', 'Submitted', 'InProgress', 'OnHold', " +
                "'MoreInformationRequired', 'Approved', 'Rejected', 'Completed', 'Cancelled')");
            table.HasCheckConstraint(
                "CK_WorkflowTransitions_TerminalOutcome_Valid",
                "[TerminalOutcome] IS NULL OR " +
                "[TerminalOutcome] IN ('Approved', 'Rejected', 'Completed')");
            table.HasCheckConstraint(
                "CK_WorkflowTransitions_TargetOrTerminal",
                "(([TerminalOutcome] IS NULL AND [TargetStepId] IS NOT NULL) OR " +
                "([TerminalOutcome] IS NOT NULL AND [TargetStepId] IS NULL))");
            table.HasCheckConstraint(
                "CK_WorkflowTransitions_TerminalStatus_MatchesOutcome",
                "[TerminalOutcome] IS NULL OR " +
                "([TerminalOutcome] = 'Approved' AND [ResultingStatus] = 'Approved') OR " +
                "([TerminalOutcome] = 'Rejected' AND [ResultingStatus] = 'Rejected') OR " +
                "([TerminalOutcome] = 'Completed' AND [ResultingStatus] = 'Completed')");
        });

        builder.HasKey(transition => transition.Id);

        builder.Property(transition => transition.Id)
            .ValueGeneratedNever();

        builder.Property(transition => transition.WorkflowVersionId)
            .ValueGeneratedNever()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        var keyProperty = builder.Property(transition => transition.Key)
            .IsRequired()
            .HasMaxLength(100);
        keyProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        var normalizedKeyProperty = builder.Property(transition => transition.NormalizedKey)
            .IsRequired()
            .HasMaxLength(100);
        normalizedKeyProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(transition => transition.ActionLabelEnglish)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(transition => transition.ActionLabelArabic)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(transition => transition.ActionType)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(transition => transition.ResultingStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(transition => transition.TerminalOutcome)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(transition => transition.IsActive)
            .HasDefaultValue(true);

        builder.Property(transition => transition.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(transition => transition.CreatedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(transition => transition.ModifiedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(transition => transition.RowVersion)
            .IsRowVersion();

        builder.HasAlternateKey(transition => new
        {
            transition.Id,
            transition.WorkflowVersionId
        })
            .HasName("AK_WorkflowTransitions_Id_WorkflowVersionId");

        builder.HasIndex(transition => new
        {
            transition.WorkflowVersionId,
            transition.NormalizedKey
        })
            .IsUnique();

        builder.HasIndex(transition => new
        {
            transition.WorkflowVersionId,
            transition.SourceStepId,
            transition.SortOrder
        })
            .IsUnique();

        builder.HasIndex(transition => new
        {
            transition.WorkflowVersionId,
            transition.TargetStepId
        });

        builder.HasOne(transition => transition.WorkflowVersion)
            .WithMany(version => version.Transitions)
            .HasForeignKey(transition => transition.WorkflowVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transition => transition.SourceStep)
            .WithMany(step => step.OutgoingTransitions)
            .HasForeignKey(transition => new
            {
                transition.SourceStepId,
                transition.WorkflowVersionId
            })
            .HasPrincipalKey(step => new
            {
                step.Id,
                step.WorkflowVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(transition => transition.TargetStep)
            .WithMany(step => step.IncomingTransitions)
            .HasForeignKey(transition => new
            {
                transition.TargetStepId,
                transition.WorkflowVersionId
            })
            .HasPrincipalKey(step => new
            {
                step.Id,
                step.WorkflowVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAuditRelationship(builder, transition => transition.CreatedByUserId);
        ConfigureAuditRelationship(builder, transition => transition.ModifiedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<WorkflowTransition> builder,
        Expression<Func<WorkflowTransition, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
