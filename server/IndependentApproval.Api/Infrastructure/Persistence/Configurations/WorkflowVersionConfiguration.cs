using System.Linq.Expressions;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkflowVersionConfiguration : IEntityTypeConfiguration<WorkflowVersion>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<WorkflowVersion> builder)
    {
        builder.ToTable("WorkflowVersions", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_WorkflowVersions_VersionNumber_Positive",
                "[VersionNumber] > 0");
            table.HasCheckConstraint(
                "CK_WorkflowVersions_NavigationOrder_NonNegative",
                "[NavigationOrder] >= 0");
            table.HasCheckConstraint(
                "CK_WorkflowVersions_Lifecycle_Valid",
                "[Lifecycle] IN ('Draft', 'Published', 'Archived')");
            table.HasCheckConstraint(
                "CK_WorkflowVersions_PublishedAudit",
                "[Lifecycle] <> 'Published' OR [PublishedAtUtc] IS NOT NULL");
            table.HasCheckConstraint(
                "CK_WorkflowVersions_ArchivedAudit",
                "[Lifecycle] <> 'Archived' OR [ArchivedAtUtc] IS NOT NULL");
        });

        builder.HasKey(version => version.Id);

        builder.Property(version => version.Id)
            .ValueGeneratedNever();

        builder.Property(version => version.WorkflowDefinitionId)
            .ValueGeneratedNever()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(version => version.RequestTypeVersionId)
            .ValueGeneratedNever();

        builder.Property(version => version.VersionNumber)
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(version => version.NameEnglish)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(version => version.NameArabic)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(version => version.DescriptionEnglish)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(version => version.DescriptionArabic)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(version => version.NavigationLabelEnglish)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(version => version.NavigationLabelArabic)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(version => version.NavigationSlug)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(200);

        builder.Property(version => version.Lifecycle)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(version => version.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(version => version.CreatedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(version => version.ModifiedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(version => version.PublishedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(version => version.ArchivedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(version => version.RowVersion)
            .IsRowVersion();

        builder.HasIndex(version => new
        {
            version.WorkflowDefinitionId,
            version.VersionNumber
        })
            .IsUnique();

        builder.HasAlternateKey(version => new
        {
            version.Id,
            version.WorkflowDefinitionId,
            version.RequestTypeVersionId
        })
            .HasName(
                "AK_WorkflowVersions_Id_WorkflowDefinitionId_RequestTypeVersionId");

        builder.HasAlternateKey(version => new
        {
            version.Id,
            version.RequestTypeVersionId
        })
            .HasName("AK_WorkflowVersions_Id_RequestTypeVersionId");

        builder.HasIndex(version => version.WorkflowDefinitionId)
            .IsUnique()
            .HasDatabaseName("UX_WorkflowVersions_WorkflowDefinitionId_Draft")
            .HasFilter("[Lifecycle] = 'Draft'");

        builder.HasIndex(version => new { version.Lifecycle, version.NavigationOrder });

        builder.HasIndex(version => new { version.Lifecycle, version.NavigationSlug });

        builder.HasIndex(version => version.RequestTypeVersionId);

        builder.HasOne(version => version.WorkflowDefinition)
            .WithMany(workflow => workflow.Versions)
            .HasForeignKey(version => version.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(version => version.RequestTypeVersion)
            .WithMany(requestTypeVersion => requestTypeVersion.WorkflowVersions)
            .HasForeignKey(version => version.RequestTypeVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(version => version.SlugReservation)
            .WithMany(reservation => reservation.Versions)
            .HasForeignKey(version => new
            {
                version.WorkflowDefinitionId,
                version.NavigationSlug
            })
            .HasPrincipalKey(reservation => new
            {
                reservation.WorkflowDefinitionId,
                reservation.NormalizedSlug
            })
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAuditRelationship(builder, version => version.CreatedByUserId);
        ConfigureAuditRelationship(builder, version => version.ModifiedByUserId);
        ConfigureAuditRelationship(builder, version => version.PublishedByUserId);
        ConfigureAuditRelationship(builder, version => version.ArchivedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<WorkflowVersion> builder,
        Expression<Func<WorkflowVersion, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
