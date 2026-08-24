using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class RequestNumberSequenceConfiguration : IEntityTypeConfiguration<RequestNumberSequence>
{
    public void Configure(EntityTypeBuilder<RequestNumberSequence> builder)
    {
        builder.ToTable("RequestNumberSequences", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_RequestNumberSequences_NormalizedPrefix_Valid",
                "LEN([NormalizedPrefix]) > 0 AND " +
                "[NormalizedPrefix] COLLATE Latin1_General_100_BIN2 " +
                "NOT LIKE '%[^A-Z0-9-]%' COLLATE Latin1_General_100_BIN2");
            table.HasCheckConstraint(
                "CK_RequestNumberSequences_Year_Valid",
                "[Year] BETWEEN 1 AND 9999");
            table.HasCheckConstraint(
                "CK_RequestNumberSequences_NextValue_Positive",
                "[NextValue] > 0");
        });

        builder.HasKey(sequence => new { sequence.NormalizedPrefix, sequence.Year });

        builder.Property(sequence => sequence.NormalizedPrefix)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(30)
            .ValueGeneratedNever();

        builder.Property(sequence => sequence.Year)
            .ValueGeneratedNever();

        builder.Property(sequence => sequence.NextValue)
            .HasDefaultValue(RequestNumberFormat.FirstSequenceValue);

        builder.Property(sequence => sequence.RowVersion)
            .IsRowVersion();
    }
}
