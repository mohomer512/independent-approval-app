using IndependentApproval.Api.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class WorkflowSlugReservationConfiguration :
    IEntityTypeConfiguration<WorkflowSlugReservation>
{
    public void Configure(EntityTypeBuilder<WorkflowSlugReservation> builder)
    {
        builder.ToTable("WorkflowSlugReservations", "app", table =>
            table.HasCheckConstraint(
                "CK_WorkflowSlugReservations_NormalizedSlug_Valid",
                "LEN([NormalizedSlug]) > 0 AND " +
                "[NormalizedSlug] COLLATE Latin1_General_100_BIN2 " +
                "NOT LIKE '%[^a-z0-9-]%' COLLATE Latin1_General_100_BIN2"));

        builder.HasKey(reservation => reservation.NormalizedSlug);

        var slugProperty = builder.Property(reservation => reservation.NormalizedSlug)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(200)
            .ValueGeneratedNever();
        slugProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(reservation => reservation.WorkflowDefinitionId)
            .ValueGeneratedNever()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(reservation => reservation.ReservedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(reservation => reservation.ReservedByAccount)
            .IsRequired()
            .HasMaxLength(256)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(reservation => reservation.ReservedByUserId)
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.HasAlternateKey(reservation => new
        {
            reservation.WorkflowDefinitionId,
            reservation.NormalizedSlug
        })
            .HasName("AK_WorkflowSlugReservations_WorkflowDefinitionId_NormalizedSlug");

        builder.HasOne(reservation => reservation.WorkflowDefinition)
            .WithMany(workflow => workflow.SlugReservations)
            .HasForeignKey(reservation => reservation.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(reservation => reservation.ReservedByUser)
            .WithMany()
            .HasForeignKey(reservation => reservation.ReservedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
