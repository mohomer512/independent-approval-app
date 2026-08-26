using System.Linq.Expressions;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkflowStepFieldPermissionConfiguration :
    IEntityTypeConfiguration<WorkflowStepFieldPermission>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<WorkflowStepFieldPermission> builder)
    {
        builder.ToTable("WorkflowStepFieldPermissions", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_WorkflowStepFieldPermissions_Access_Valid",
                "[Access] IN ('Hidden', 'ReadOnly', 'Editable', 'EditableRequired')");
            table.HasCheckConstraint(
                "CK_WorkflowStepFieldPermissions_DocumentAccess_Valid",
                "[DocumentAccess] IS NULL OR [DocumentAccess] IN ('Hidden', 'View', 'Edit')");
            table.HasCheckConstraint(
                "CK_WorkflowStepFieldPermissions_ForwardingRequiresDocumentAccess",
                "[CanSelectForForwarding] = 0 OR [DocumentAccess] IN ('View', 'Edit')");
        });

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Id)
            .ValueGeneratedNever();

        ConfigureImmutableId(builder, permission => permission.WorkflowVersionId);
        ConfigureImmutableId(builder, permission => permission.RequestTypeVersionId);
        ConfigureImmutableId(builder, permission => permission.WorkflowStepId);
        ConfigureImmutableId(builder, permission => permission.RequestFieldDefinitionId);

        builder.Property(permission => permission.Access)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(permission => permission.DocumentAccess)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(permission => permission.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(permission => permission.CreatedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(permission => permission.ModifiedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(permission => permission.RowVersion)
            .IsRowVersion();

        builder.HasIndex(permission => new
        {
            permission.WorkflowStepId,
            permission.RequestFieldDefinitionId
        })
            .IsUnique();

        builder.HasIndex(permission => new
        {
            permission.WorkflowVersionId,
            permission.RequestTypeVersionId
        });

        builder.HasIndex(permission => new
        {
            permission.RequestFieldDefinitionId,
            permission.RequestTypeVersionId
        });

        builder.HasOne(permission => permission.WorkflowVersion)
            .WithMany(version => version.FieldPermissions)
            .HasForeignKey(permission => new
            {
                permission.WorkflowVersionId,
                permission.RequestTypeVersionId
            })
            .HasPrincipalKey(version => new
            {
                version.Id,
                version.RequestTypeVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(permission => permission.WorkflowStep)
            .WithMany(step => step.FieldPermissions)
            .HasForeignKey(permission => new
            {
                permission.WorkflowStepId,
                permission.WorkflowVersionId
            })
            .HasPrincipalKey(step => new
            {
                step.Id,
                step.WorkflowVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(permission => permission.RequestFieldDefinition)
            .WithMany(field => field.WorkflowStepFieldPermissions)
            .HasForeignKey(permission => new
            {
                permission.RequestFieldDefinitionId,
                permission.RequestTypeVersionId
            })
            .HasPrincipalKey(field => new
            {
                field.Id,
                field.RequestTypeVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAuditRelationship(builder, permission => permission.CreatedByUserId);
        ConfigureAuditRelationship(builder, permission => permission.ModifiedByUserId);
    }

    private static void ConfigureImmutableId(
        EntityTypeBuilder<WorkflowStepFieldPermission> builder,
        Expression<Func<WorkflowStepFieldPermission, Guid>> propertyExpression)
    {
        builder.Property(propertyExpression)
            .ValueGeneratedNever()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<WorkflowStepFieldPermission> builder,
        Expression<Func<WorkflowStepFieldPermission, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
