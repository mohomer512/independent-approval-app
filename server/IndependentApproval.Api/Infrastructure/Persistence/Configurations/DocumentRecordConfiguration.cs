using IndependentApproval.Api.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class DocumentRecordConfiguration : IEntityTypeConfiguration<DocumentRecord>
{
    public void Configure(EntityTypeBuilder<DocumentRecord> builder)
    {
        builder.ToTable("Documents", "app");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(document => document.StorageKey)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(document => document.ContentType)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(document => document.FileSize)
            .IsRequired();

        builder.Property(document => document.Description)
            .HasMaxLength(2000);

        builder.Property(document => document.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Available");

        builder.Property(document => document.UploadedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(document => document.UploadedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(document => document.RowVersion)
            .IsRowVersion();

        builder.HasIndex(document => document.StorageKey)
            .IsUnique();

        builder.HasIndex(document => document.UploadedAtUtc);

        builder.HasIndex(document => document.UploadedBy);
    }
}
