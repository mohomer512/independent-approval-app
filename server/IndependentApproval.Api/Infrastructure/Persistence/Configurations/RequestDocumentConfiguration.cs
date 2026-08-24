using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class RequestDocumentConfiguration : IEntityTypeConfiguration<RequestDocument>
{
    public void Configure(EntityTypeBuilder<RequestDocument> builder)
    {
        builder.ToTable("RequestDocuments", "app", table =>
            table.HasCheckConstraint(
                "CK_RequestDocuments_SortOrder_NonNegative",
                "[SortOrder] >= 0"));

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id)
            .ValueGeneratedNever();

        builder.Property(document => document.RequestTypeVersionId)
            .ValueGeneratedNever();

        builder.Property(document => document.AttachedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(document => document.RowVersion)
            .IsRowVersion();

        builder.HasIndex(document => new
        {
            document.ApprovalRequestId,
            document.RequestFieldDefinitionId,
            document.DocumentId
        })
            .IsUnique();

        builder.HasIndex(document => new
        {
            document.ApprovalRequestId,
            document.RequestFieldDefinitionId,
            document.SortOrder
        });

        builder.HasIndex(document => document.DocumentId);

        builder.HasOne(document => document.ApprovalRequest)
            .WithMany(request => request.Documents)
            .HasForeignKey(document => new
            {
                document.ApprovalRequestId,
                document.RequestTypeVersionId
            })
            .HasPrincipalKey(request => new
            {
                request.Id,
                request.RequestTypeVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(document => document.RequestFieldDefinition)
            .WithMany(field => field.Documents)
            .HasForeignKey(document => new
            {
                document.RequestFieldDefinitionId,
                document.RequestTypeVersionId
            })
            .HasPrincipalKey(field => new
            {
                field.Id,
                field.RequestTypeVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(document => document.Document)
            .WithMany()
            .HasForeignKey(document => document.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(document => document.AttachedByApplicationUser)
            .WithMany()
            .HasForeignKey(document => document.AttachedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
