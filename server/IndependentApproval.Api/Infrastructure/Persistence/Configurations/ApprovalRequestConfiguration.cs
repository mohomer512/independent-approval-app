using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("ApprovalRequests", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_ApprovalRequests_Status_Valid",
                "[Status] IN ('Draft', 'Submitted', 'InProgress', 'OnHold', " +
                "'MoreInformationRequired', 'Approved', 'Rejected', 'Completed', 'Cancelled')");
            table.HasCheckConstraint(
                "CK_ApprovalRequests_WorkflowDefinitionId_NotEmpty",
                "[WorkflowDefinitionId] <> '00000000-0000-0000-0000-000000000000'");
            table.HasCheckConstraint(
                "CK_ApprovalRequests_WorkflowVersionId_NotEmpty",
                "[WorkflowVersionId] <> '00000000-0000-0000-0000-000000000000'");
            table.HasCheckConstraint(
                "CK_ApprovalRequests_CurrentWorkflowStepId_NotEmpty",
                "[CurrentWorkflowStepId] IS NULL OR " +
                "[CurrentWorkflowStepId] <> '00000000-0000-0000-0000-000000000000'");
            table.HasCheckConstraint(
                "CK_ApprovalRequests_ModifiedAtUtc_Chronology",
                "[ModifiedAtUtc] IS NULL OR [ModifiedAtUtc] >= [CreatedAtUtc]");
            table.HasCheckConstraint(
                "CK_ApprovalRequests_SubmittedAtUtc_Chronology",
                "[SubmittedAtUtc] IS NULL OR [SubmittedAtUtc] >= [CreatedAtUtc]");
            table.HasCheckConstraint(
                "CK_ApprovalRequests_CompletedAtUtc_Chronology",
                "[CompletedAtUtc] IS NULL OR [CompletedAtUtc] >= [CreatedAtUtc]");
        });

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .ValueGeneratedNever();

        var requestNumberProperty = builder.Property(request => request.RequestNumber)
            .IsRequired()
            .HasMaxLength(80);
        requestNumberProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(request => request.RequestTypeId)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(request => request.RequestTypeVersionId)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(request => request.WorkflowDefinitionId)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(request => request.WorkflowVersionId)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(request => request.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(request => request.RequestedByUserId)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(request => request.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(request => request.RowVersion)
            .IsRowVersion();

        builder.HasIndex(request => request.RequestNumber)
            .IsUnique();

        builder.HasAlternateKey(request => new
        {
            request.Id,
            request.RequestTypeVersionId
        })
            .HasName("AK_ApprovalRequests_Id_RequestTypeVersionId");

        builder.HasIndex(request => new
        {
            request.RequestedByUserId,
            request.Status,
            request.ModifiedAtUtc
        });

        builder.HasIndex(request => new
        {
            request.RequestTypeVersionId,
            request.Status
        });

        builder.HasIndex(request => new
        {
            request.WorkflowVersionId,
            request.Status
        });

        builder.HasIndex(request => request.CurrentWorkflowStepId);

        builder.HasOne(request => request.RequestType)
            .WithMany(requestType => requestType.Requests)
            .HasForeignKey(request => request.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.RequestTypeVersion)
            .WithMany(version => version.Requests)
            .HasForeignKey(request => new
            {
                request.RequestTypeVersionId,
                request.RequestTypeId
            })
            .HasPrincipalKey(version => new
            {
                version.Id,
                version.RequestTypeId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.WorkflowDefinition)
            .WithMany(workflow => workflow.Requests)
            .HasForeignKey(request => request.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.WorkflowVersion)
            .WithMany(version => version.Requests)
            .HasForeignKey(request => new
            {
                request.WorkflowVersionId,
                request.WorkflowDefinitionId,
                request.RequestTypeVersionId
            })
            .HasPrincipalKey(version => new
            {
                version.Id,
                version.WorkflowDefinitionId,
                version.RequestTypeVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.CurrentWorkflowStep)
            .WithMany(step => step.CurrentRequests)
            .HasForeignKey(request => new
            {
                request.CurrentWorkflowStepId,
                request.WorkflowVersionId
            })
            .HasPrincipalKey(step => new
            {
                step.Id,
                step.WorkflowVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.RequestedByUser)
            .WithMany()
            .HasForeignKey(request => request.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
