using IndependentApproval.Api.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkflowStepRoleConfiguration : IEntityTypeConfiguration<WorkflowStepRole>
{
    public void Configure(EntityTypeBuilder<WorkflowStepRole> builder)
    {
        builder.ToTable("WorkflowStepRoles", "app");

        builder.HasKey(assignment => new
        {
            assignment.WorkflowStepId,
            assignment.ApplicationRoleId
        });

        builder.Property(assignment => assignment.WorkflowStepId)
            .ValueGeneratedNever()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(assignment => assignment.WorkflowVersionId)
            .ValueGeneratedNever()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(assignment => assignment.ApplicationRoleId)
            .ValueGeneratedNever()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.HasIndex(assignment => new
        {
            assignment.WorkflowVersionId,
            assignment.ApplicationRoleId
        });

        builder.HasOne(assignment => assignment.WorkflowStep)
            .WithMany(step => step.AssignedRoles)
            .HasForeignKey(assignment => new
            {
                assignment.WorkflowStepId,
                assignment.WorkflowVersionId
            })
            .HasPrincipalKey(step => new
            {
                step.Id,
                step.WorkflowVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.WorkflowVersion)
            .WithMany()
            .HasForeignKey(assignment => assignment.WorkflowVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.ApplicationRole)
            .WithMany()
            .HasForeignKey(assignment => assignment.ApplicationRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
