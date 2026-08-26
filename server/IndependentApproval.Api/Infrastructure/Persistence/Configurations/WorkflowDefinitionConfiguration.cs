using System.Linq.Expressions;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_WorkflowDefinitions_Code_NotBlank",
                "LEN(LTRIM(RTRIM([Code]))) > 0");
            table.HasCheckConstraint(
                "CK_WorkflowDefinitions_NormalizedCode_NotBlank",
                "LEN(LTRIM(RTRIM([NormalizedCode]))) > 0");
            table.HasCheckConstraint(
                "CK_WorkflowDefinitions_ArchivedAudit",
                "[IsArchived] = 0 OR [ArchivedAtUtc] IS NOT NULL");
        });

        builder.HasKey(workflow => workflow.Id);

        builder.Property(workflow => workflow.Id)
            .ValueGeneratedNever();

        var codeProperty = builder.Property(workflow => workflow.Code)
            .IsRequired()
            .HasMaxLength(100);
        codeProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        var normalizedCodeProperty = builder.Property(workflow => workflow.NormalizedCode)
            .IsRequired()
            .HasMaxLength(100);
        normalizedCodeProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(workflow => workflow.IsArchived)
            .HasDefaultValue(false);

        builder.Property(workflow => workflow.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(workflow => workflow.CreatedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(workflow => workflow.ModifiedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(workflow => workflow.ArchivedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(workflow => workflow.RowVersion)
            .IsRowVersion();

        builder.HasIndex(workflow => workflow.NormalizedCode)
            .IsUnique();

        builder.HasIndex(workflow => workflow.IsArchived);

        ConfigureAuditRelationship(builder, workflow => workflow.CreatedByUserId);
        ConfigureAuditRelationship(builder, workflow => workflow.ModifiedByUserId);
        ConfigureAuditRelationship(builder, workflow => workflow.ArchivedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<WorkflowDefinition> builder,
        Expression<Func<WorkflowDefinition, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
