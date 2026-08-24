using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class RequestTypePrefixReservationConfiguration :
    IEntityTypeConfiguration<RequestTypePrefixReservation>
{
    public void Configure(EntityTypeBuilder<RequestTypePrefixReservation> builder)
    {
        builder.ToTable("RequestTypePrefixReservations", "app", table =>
            table.HasCheckConstraint(
                "CK_RequestTypePrefixReservations_NormalizedPrefix_Valid",
                "LEN([NormalizedPrefix]) > 0 AND " +
                "[NormalizedPrefix] COLLATE Latin1_General_100_BIN2 " +
                "NOT LIKE '%[^A-Z0-9]%' COLLATE Latin1_General_100_BIN2"));

        builder.HasKey(reservation => reservation.NormalizedPrefix);

        var prefixProperty = builder.Property(reservation => reservation.NormalizedPrefix)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(30)
            .ValueGeneratedNever();
        prefixProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

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
            reservation.NormalizedPrefix
        })
            .HasName("AK_RequestTypePrefixReservations_RequestTypeId_NormalizedPrefix");

        builder.HasOne(reservation => reservation.RequestType)
            .WithMany(requestType => requestType.PrefixReservations)
            .HasForeignKey(reservation => reservation.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(reservation => reservation.ReservedByUser)
            .WithMany()
            .HasForeignKey(reservation => reservation.ReservedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
