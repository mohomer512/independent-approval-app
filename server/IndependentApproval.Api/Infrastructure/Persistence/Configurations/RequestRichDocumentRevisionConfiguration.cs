using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class RequestRichDocumentRevisionConfiguration : IEntityTypeConfiguration<RequestRichDocumentRevision>
{
    public void Configure(EntityTypeBuilder<RequestRichDocumentRevision> builder)
    {
        builder.ToTable("RequestRichDocumentRevisions", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_RequestRichDocumentRevisions_RevisionNumber_Positive",
                "[RevisionNumber] > 0");
            table.HasCheckConstraint(
                "CK_RequestRichDocumentRevisions_ContentLength_NonNegative",
                "[ContentLength] >= 0");
            table.HasCheckConstraint(
                "CK_RequestRichDocumentRevisions_ContentSha256_Valid",
                "LEN([ContentSha256]) = 64 AND [ContentSha256] NOT LIKE '%[^0-9A-Fa-f]%'");
        });

        builder.HasKey(revision => revision.Id);

        builder.Property(revision => revision.Id)
            .ValueGeneratedNever();

        ConfigureImmutableProperty(builder.Property(revision => revision.ApprovalRequestId));
        ConfigureImmutableProperty(builder.Property(revision => revision.RequestTypeVersionId));
        ConfigureImmutableProperty(builder.Property(revision => revision.RequestFieldDefinitionId));
        ConfigureImmutableProperty(builder.Property(revision => revision.RevisionNumber));

        var contentProperty = builder.Property(revision => revision.ContentHtml)
            .IsRequired()
            .HasColumnType("nvarchar(max)");
        contentProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        var hashProperty = builder.Property(revision => revision.ContentSha256)
            .IsRequired()
            .HasColumnType("char(64)");
        hashProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        ConfigureImmutableProperty(builder.Property(revision => revision.ContentLength));

        var sanitizerVersionProperty = builder.Property(revision => revision.SanitizerVersion)
            .IsRequired()
            .HasMaxLength(100);
        sanitizerVersionProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        var createdAtProperty = builder.Property(revision => revision.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");
        createdAtProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        ConfigureImmutableProperty(builder.Property(revision => revision.CreatedByApplicationUserId));

        builder.Property(revision => revision.RowVersion)
            .IsRowVersion();

        builder.HasIndex(revision => new
        {
            revision.ApprovalRequestId,
            revision.RequestFieldDefinitionId,
            revision.RevisionNumber
        })
            .IsUnique();

        builder.HasIndex(revision => revision.RequestFieldDefinitionId);

        builder.HasOne(revision => revision.ApprovalRequest)
            .WithMany(request => request.RichDocumentRevisions)
            .HasForeignKey(revision => new
            {
                revision.ApprovalRequestId,
                revision.RequestTypeVersionId
            })
            .HasPrincipalKey(request => new
            {
                request.Id,
                request.RequestTypeVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(revision => revision.RequestFieldDefinition)
            .WithMany(field => field.RichDocumentRevisions)
            .HasForeignKey(revision => new
            {
                revision.RequestFieldDefinitionId,
                revision.RequestTypeVersionId
            })
            .HasPrincipalKey(field => new
            {
                field.Id,
                field.RequestTypeVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(revision => revision.CreatedByApplicationUser)
            .WithMany()
            .HasForeignKey(revision => revision.CreatedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureImmutableProperty<TProperty>(PropertyBuilder<TProperty> propertyBuilder)
    {
        propertyBuilder.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
    }
}
