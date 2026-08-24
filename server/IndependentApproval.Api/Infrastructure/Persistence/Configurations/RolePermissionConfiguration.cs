using IndependentApproval.Api.Domain.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", "app");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .ValueGeneratedNever();

        builder.Property(assignment => assignment.GrantedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(assignment => assignment.GrantedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(assignment => assignment.RemovedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(assignment => assignment.RowVersion)
            .IsRowVersion();

        builder.HasIndex(assignment => new
        {
            assignment.ApplicationRoleId,
            assignment.PermissionId
        })
            .IsUnique();

        builder.HasIndex(assignment => new { assignment.PermissionId, assignment.RemovedAtUtc });

        builder.HasOne(assignment => assignment.ApplicationRole)
            .WithMany(role => role.PermissionAssignments)
            .HasForeignKey(assignment => assignment.ApplicationRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Permission)
            .WithMany(permission => permission.RoleAssignments)
            .HasForeignKey(assignment => assignment.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAuditRelationship(builder, assignment => assignment.GrantedByUserId);
        ConfigureAuditRelationship(builder, assignment => assignment.RemovedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<RolePermission> builder,
        System.Linq.Expressions.Expression<Func<RolePermission, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
