using System.Linq.Expressions;
using IndependentApproval.Api.Domain.Administration;
using IndependentApproval.Api.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndependentApproval.Api.Infrastructure.Persistence.Configurations;

public sealed class RequestTypeConfiguration : IEntityTypeConfiguration<RequestType>
{
    private const int AccountMaxLength = 256;

    public void Configure(EntityTypeBuilder<RequestType> builder)
    {
        builder.ToTable("RequestTypes", "app", table =>
        {
            table.HasCheckConstraint(
                "CK_RequestTypes_Code_NotBlank",
                "LEN(LTRIM(RTRIM([Code]))) > 0");
            table.HasCheckConstraint(
                "CK_RequestTypes_NormalizedCode_NotBlank",
                "LEN(LTRIM(RTRIM([NormalizedCode]))) > 0");
        });

        builder.HasKey(requestType => requestType.Id);

        builder.Property(requestType => requestType.Id)
            .ValueGeneratedNever();

        var codeProperty = builder.Property(requestType => requestType.Code)
            .IsRequired()
            .HasMaxLength(100);
        codeProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        var normalizedCodeProperty = builder.Property(requestType => requestType.NormalizedCode)
            .IsRequired()
            .HasMaxLength(100);
        normalizedCodeProperty.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        builder.Property(requestType => requestType.IsArchived)
            .HasDefaultValue(false);

        builder.Property(requestType => requestType.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(requestType => requestType.CreatedByAccount)
            .IsRequired()
            .HasMaxLength(AccountMaxLength);

        builder.Property(requestType => requestType.ModifiedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(requestType => requestType.ArchivedByAccount)
            .HasMaxLength(AccountMaxLength);

        builder.Property(requestType => requestType.RowVersion)
            .IsRowVersion();

        builder.HasIndex(requestType => requestType.NormalizedCode)
            .IsUnique();

        builder.HasIndex(requestType => requestType.IsArchived);

        ConfigureAuditRelationship(builder, requestType => requestType.CreatedByUserId);
        ConfigureAuditRelationship(builder, requestType => requestType.ModifiedByUserId);
        ConfigureAuditRelationship(builder, requestType => requestType.ArchivedByUserId);
    }

    private static void ConfigureAuditRelationship(
        EntityTypeBuilder<RequestType> builder,
        Expression<Func<RequestType, object?>> foreignKeyExpression)
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
