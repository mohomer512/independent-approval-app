using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class RequestTypeSlugReservationConfiguration :
    IEntityTypeConfiguration<RequestTypeSlugReservation>
{
    public void Configure(EntityTypeBuilder<RequestTypeSlugReservation> builder)
    {
        builder.ToTable("RequestTypeSlugReservations", "app", table =>
            table.HasCheckConstraint(
                "CK_RequestTypeSlugReservations_NormalizedSlug_Valid",
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

        builder.Property(reservation => reservation.RequestTypeId)
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
            reservation.RequestTypeId,
            reservation.NormalizedSlug
        })
            .HasName("AK_RequestTypeSlugReservations_RequestTypeId_NormalizedSlug");

        builder.HasOne(reservation => reservation.RequestType)
            .WithMany(requestType => requestType.SlugReservations)
            .HasForeignKey(reservation => reservation.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(reservation => reservation.ReservedByUser)
            .WithMany()
            .HasForeignKey(reservation => reservation.ReservedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
