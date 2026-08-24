using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class RequestContentValueConfiguration : IEntityTypeConfiguration<RequestContentValue>
{
    public void Configure(EntityTypeBuilder<RequestContentValue> builder)
    {
        builder.ToTable("RequestContentValues", "app", table =>
            table.HasCheckConstraint(
                "CK_RequestContentValues_ValueJson_Valid",
                "ISJSON(CONCAT('[', [ValueJson], ']')) = 1"));

        builder.HasKey(value => value.Id);

        builder.Property(value => value.Id)
            .ValueGeneratedNever();

        builder.Property(value => value.RequestTypeVersionId)
            .ValueGeneratedNever();

        builder.Property(value => value.ValueJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(value => value.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(value => value.RowVersion)
            .IsRowVersion();

        builder.HasIndex(value => new
        {
            value.ApprovalRequestId,
            value.RequestFieldDefinitionId
        })
            .IsUnique();

        builder.HasIndex(value => value.RequestFieldDefinitionId);

        builder.HasOne(value => value.ApprovalRequest)
            .WithMany(request => request.ContentValues)
            .HasForeignKey(value => new
            {
                value.ApprovalRequestId,
                value.RequestTypeVersionId
            })
            .HasPrincipalKey(request => new
            {
                request.Id,
                request.RequestTypeVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(value => value.RequestFieldDefinition)
            .WithMany(field => field.ContentValues)
            .HasForeignKey(value => new
            {
                value.RequestFieldDefinitionId,
                value.RequestTypeVersionId
            })
            .HasPrincipalKey(field => new
            {
                field.Id,
                field.RequestTypeVersionId
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(value => value.CreatedByApplicationUser)
            .WithMany()
            .HasForeignKey(value => value.CreatedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(value => value.ModifiedByApplicationUser)
            .WithMany()
            .HasForeignKey(value => value.ModifiedByApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
