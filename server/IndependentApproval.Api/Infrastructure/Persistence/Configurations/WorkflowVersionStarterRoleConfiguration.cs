using IndependentApproval.Api.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkflowVersionStarterRoleConfiguration :
    IEntityTypeConfiguration<WorkflowVersionStarterRole>
{
    public void Configure(EntityTypeBuilder<WorkflowVersionStarterRole> builder)
    {
        builder.ToTable("WorkflowVersionStarterRoles", "app");

        builder.HasKey(assignment => new
        {
            assignment.WorkflowVersionId,
            assignment.ApplicationRoleId
        });

        builder.Property(assignment => assignment.WorkflowVersionId)
            .ValueGeneratedNever();

        builder.Property(assignment => assignment.ApplicationRoleId)
            .ValueGeneratedNever();

        builder.HasIndex(assignment => assignment.ApplicationRoleId);

        builder.HasOne(assignment => assignment.WorkflowVersion)
            .WithMany(version => version.StarterRoles)
            .HasForeignKey(assignment => assignment.WorkflowVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.ApplicationRole)
            .WithMany()
            .HasForeignKey(assignment => assignment.ApplicationRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
