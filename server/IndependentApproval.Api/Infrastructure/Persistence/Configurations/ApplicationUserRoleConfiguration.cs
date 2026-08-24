using IndependentApproval.Api.Domain.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserRoleConfiguration : IEntityTypeConfiguration<ApplicationUserRole>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<ApplicationUserRole> builder)
    {
        builder.ToTable("ApplicationUserRoles", "app");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .ValueGeneratedNever();

        builder.Property(assignment => assignment.AssignedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(assignment => assignment.AssignedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(assignment => assignment.RemovedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(assignment => assignment.RowVersion)
            .IsRowVersion();

        builder.HasIndex(assignment => new
        {
            assignment.ApplicationUserId,
            assignment.ApplicationRoleId
        })
            .IsUnique();

        builder.HasIndex(assignment => new { assignment.ApplicationRoleId, assignment.RemovedAtUtc });

        builder.HasOne(assignment => assignment.ApplicationUser)
            .WithMany(user => user.RoleAssignments)
            .HasForeignKey(assignment => assignment.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.ApplicationRole)
            .WithMany(role => role.UserAssignments)
            .HasForeignKey(assignment => assignment.ApplicationRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAuditRelationship(builder, assignment => assignment.AssignedByUserId);
        ConfigureAuditRelationship(builder, assignment => assignment.RemovedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<ApplicationUserRole> builder,
        System.Linq.Expressions.Expression<Func<ApplicationUserRole, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
