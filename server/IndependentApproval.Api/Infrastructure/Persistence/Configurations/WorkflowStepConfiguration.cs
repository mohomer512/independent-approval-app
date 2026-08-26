using System.Linq.Expressions;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("WorkflowSteps", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_WorkflowSteps_SortOrder_NonNegative",
                "[SortOrder] >= 0");
            table.HasCheckConstraint(
                "CK_WorkflowSteps_CommentPolicy_Valid",
                "[CommentPolicy] IN ('None', 'Optional', 'Required')");
        });

        builder.HasKey(step => step.Id);

        builder.Property(step => step.Id)
            .ValueGeneratedNever();

        builder.Property(step => step.WorkflowVersionId)
            .ValueGeneratedNever()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        var keyProperty = builder.Property(step => step.Key)
            .IsRequired()
            .HasMaxLength(100);
        keyProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        var normalizedKeyProperty = builder.Property(step => step.NormalizedKey)
            .IsRequired()
            .HasMaxLength(100);
        normalizedKeyProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(step => step.NameEnglish)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(step => step.NameArabic)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(step => step.DiagramX)
            .HasPrecision(12, 2);

        builder.Property(step => step.DiagramY)
            .HasPrecision(12, 2);

        builder.Property(step => step.IsActive)
            .HasDefaultValue(true);

        builder.Property(step => step.CanOpenDocuments)
            .HasDefaultValue(true);

        builder.Property(step => step.CommentPolicy)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(step => step.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(step => step.CreatedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(step => step.ModifiedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(step => step.RowVersion)
            .IsRowVersion();

        builder.HasAlternateKey(step => new
        {
            step.Id,
            step.WorkflowVersionId
        })
            .HasName("AK_WorkflowSteps_Id_WorkflowVersionId");

        builder.HasIndex(step => new { step.WorkflowVersionId, step.NormalizedKey })
            .IsUnique();

        builder.HasIndex(step => new { step.WorkflowVersionId, step.SortOrder })
            .IsUnique();

        builder.HasIndex(step => step.WorkflowVersionId)
            .IsUnique()
            .HasDatabaseName("UX_WorkflowSteps_WorkflowVersionId_StartStep")
            .HasFilter("[IsStartStep] = 1");

        builder.HasIndex(step => new { step.WorkflowVersionId, step.IsActive });

        builder.HasOne(step => step.WorkflowVersion)
            .WithMany(version => version.Steps)
            .HasForeignKey(step => step.WorkflowVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAuditRelationship(builder, step => step.CreatedByUserId);
        ConfigureAuditRelationship(builder, step => step.ModifiedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<WorkflowStep> builder,
        Expression<Func<WorkflowStep, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
